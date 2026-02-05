from json import loads
from shutil import which
from pathlib import Path
from time import monotonic_ns
from tempfile import gettempdir
from argparse import ArgumentParser
from sys import exit, stdout, stderr
from squash.lib.http import HttpClient
from squash.lib.tui import Tui, MessageSeverity
from subprocess import run, PIPE, Popen, DEVNULL
from squash.lib.host import is_in_windows_terminal
from squash.lib.errors import MissingBinaryException
from typing import cast, Literal, Optional, TypedDict
from squash import APP_NAME, APP_VERSION_STRING, BINARY_PATH, SEVENZIP_PATH, APP_DESCRIPTION

class EncodeResults(TypedDict):
    success: bool
    file_path: Path
    file_size: int
    target_size: int
    iteration: int
    bitrate: float

tui = Tui(stdout, stderr)

def _get_video_info(path: Path) -> tuple[float, int]:
    ffprobe_path = _get_binary_path('ffprobe')
    proc = Popen(
            [ffprobe_path, '-v', 'error', '-show_entries', 'format=duration,bit_rate', '-of', 'json', path],
            stdout=PIPE,
            text=True,
    )

    out, err = proc.communicate()
    data = loads(out)

    return float(data['format']['duration']), int(data['format']['bit_rate'])

def _get_file_size(path: Path) -> int:
    return path.stat().st_size

def _format_bytes(size: int, precision = 2) -> str:
    if size < 0:
        raise ValueError('size must be non-negative')

    units = ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB']
    i = 0
    while size >= 1024 and i < len(units) - 1:
        size /= 1024
        i += 1
    if i == 0:
        return f'{int(size)}{units[i]}'
    else:
        return f'{size:.{precision}f}{units[i]}'

def _get_encode_settings(quality: int) -> list[str]:
    base_args = [
        '-c:a', 'aac',
        '-profile:v', 'main',
        '-movflags', '+faststart',
        '-pix_fmt', 'yuv420p',
    ]

    quality_map = {
        1: ('libx264', 'medium'),
        2: ('libx265', 'medium'),
        3: ('libx265', 'slow'),
        4: ('libx265', 'veryslow'),
    }

    if quality in quality_map:
        codec, preset = quality_map[quality]
        base_args.extend(['-c:v', codec, '-preset', preset])

    return base_args

def _encode_video(path: Path, output: Path, video_bitrate: float, audio_bitrate: int, quality: int) -> None:
    ffmpeg_path = _get_binary_path('ffmpeg')

    with Popen([
        ffmpeg_path,
        '-y',
        '-i', str(path),
        '-b:v', f'{video_bitrate}k',
        '-b:a', f'{audio_bitrate}k',
        *_get_encode_settings(quality),
        '-progress', 'pipe:1',
        '-nostats',
        str(output)
    ], stdout=PIPE, stderr=DEVNULL, text=True, bufsize=1) as proc:
        progress: dict[str, str] = {}
        for line in proc.stdout:
            line = line.strip()

            if not line or 'N/A' in line:
                continue

            if '=' not in line:
                continue

            key, value = line.split('=', 1)
            if not key or not value:
                continue

            progress[key] = value

            if key == 'progress' and value == 'continue':
                tui.writeln('Encoding... (speed: %s, FPS: %s, bitrate: %s)' % (
                        tui.dim(progress.get('speed')),
                        tui.dim(progress.get('fps')),
                        tui.dim(progress.get('bitrate')),
                    )
                )

        proc.wait()

def _resize_video_to_target(input_file: Path, target_size_mb: int, output_file: Path, tolerance: float, iterations: int, quality: int) -> EncodeResults:
    tui.writeln(f'Analyzing {tui.bold(str(input_file))}...')

    target_size_bytes = target_size_mb * 1024 * 1024
    tolerance_bytes = target_size_bytes * (tolerance / 100)

    current_video_size = _get_file_size(input_file)

    tui.writeln(f'Current size: {tui.bold(_format_bytes(current_video_size))}')

    if current_video_size <= target_size_bytes:
        tui.writeln('Video is already at or under target size. No encoding needed.', severity=MessageSeverity.SUCCESS)
        return {
            'success': True,
            'file_path': input_file,
            'file_size': current_video_size,
            'target_size': target_size_bytes,
            'iteration': 0,
            'bitrate': 0,
        }

    video_duration, video_bitrate = _get_video_info(input_file)
    audio_bitrate = 128  # k

    # calculate initial target bitrate
    # formula: (target_bytes * 8) / duration = total_bitrate
    # subtract audio bitrate and add ~3% overhead for container
    target_bitrate = (target_size_bytes * 8) / video_duration / 1_000 - audio_bitrate
    target_bitrate *= 0.97  # account for container overhead

    min_bitrate = 100
    max_bitrate = target_bitrate * 2
    current_bitrate = target_bitrate

    iteration = 0

    temp_output = Path(gettempdir(), f'squash-temp-{monotonic_ns()}.mp4')

    try:
        while iteration < iterations:
            iteration += 1

            tui.writeln(f'Iteration {iteration} of {iterations}: encoding at {current_bitrate:.0f}kbps with a {tolerance}% tolerance...')

            _encode_video(input_file, temp_output, current_bitrate, audio_bitrate, quality)

            new_file_size = _get_file_size(temp_output)
            size_difference = new_file_size - target_size_bytes
            percent_difference = size_difference / target_size_bytes * 100
            is_positive_difference = percent_difference < 0
            styled_percent_difference = tui.color_success(f'{percent_difference:.2f}%') if is_positive_difference else tui.color_error(f'+{percent_difference:.2f}%')

            tui.writeln(f'Result: {tui.bold(_format_bytes(new_file_size))} ({styled_percent_difference})')

            if new_file_size < target_size_bytes:
                gap_to_target = target_size_bytes - new_file_size
                if gap_to_target < tolerance_bytes:
                    tui.writeln(f'Target achieved! Moving file to {output_file.name}')
                    temp_output.replace(output_file)
                    temp_output.unlink(missing_ok=True)
                    return {
                        'success': True,
                        'file_path': output_file,
                        'file_size': new_file_size,
                        'target_size': target_size_bytes,
                        'iteration': iteration,
                        'bitrate': current_bitrate,
                    }
                else:
                    tui.writeln(f'Too far below target ({tui.bold(_format_bytes(gap_to_target))} gap), increasing bitrate for next iteration')
                    min_bitrate = current_bitrate
            else:
                tui.writeln('Over target, reducing bitrate for next iteration')
                max_bitrate = current_bitrate

            current_bitrate = (min_bitrate + max_bitrate) / 2
            if current_bitrate < 100:
                tui.writeln(f'Bitrate too low. Cannot achieve target size without severe quality loss.')
                break

        final_size = _get_file_size(temp_output)
        if final_size <= target_size_bytes:
            tui.writeln(f'Max iterations reached, using best result under target.')
            temp_output.replace(output_file)
            temp_output.unlink(missing_ok=True)

            return {
                'success': False,
                'file_path': output_file,
                'file_size': final_size,
                'target_size': target_size_bytes,
                'iteration': iteration,
                'bitrate': current_bitrate,
            }
        else:
            temp_output.unlink(missing_ok=True)
            raise Exception(f'Could not achieve target size after {iterations} iterations. Final result was {_format_bytes(final_size)} (over target).')
    except Exception as e:
        temp_output.unlink(missing_ok=True)
        raise e

def _get_binary_path(name: Literal['ffmpeg', 'ffprobe']) -> Optional[Path]:
    binary_path = (Path(BINARY_PATH).parent / name).with_suffix('.exe')
    if binary_path.exists():
        return binary_path

    which_path = which(name)

    return Path(which_path) if which_path is not None else None

def _has_required_binaries() -> bool:
    has_ffmpeg = _get_binary_path('ffmpeg')
    has_ffprobe = _get_binary_path('ffprobe')

    return has_ffmpeg is not None and has_ffprobe is not None

def main() -> int:
    #region Windows Terminal check
    if not is_in_windows_terminal():
        print(tui.boxed('It is highly recommended you use a command line shell like Windows Terminal for the best experience: https://apps.microsoft.com/detail/9n0dx20hk701'))
    #endregion

    #region FFmpeg/FFprobe check
    if not _has_required_binaries():
        tui.writeln('It appears that you are missing FFmpeg and/or FFprobe on your system.', severity=MessageSeverity.WARNING)
        tui.write('Would you like to automatically download them?', severity=MessageSeverity.WARNING)
        if input(' [y/N]\n').lower() != 'y':
            return 1

        def on_progress(current: int, total: int):
            tui.erase_line()
            if current == total:
                tui.write(f'Finished downloading archive\n', severity=MessageSeverity.SUCCESS)
            else:
                tui.write(f'Downloading... ({_format_bytes(current)}/{_format_bytes(total)})')

        download_path = Path(gettempdir()) / 'ffmpeg.7z'

        http = HttpClient()
        http.download('https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z', download_path, on_progress=on_progress)

        tui.writeln('Extracting...')

        run([SEVENZIP_PATH, 'e', download_path, '-r', f'-o{BINARY_PATH.parent}', '-aoa', 'ffmpeg.exe', 'ffprobe.exe'], stdout=DEVNULL, check=True)

        tui.writeln('Done!')
    #endregion

    #region Argument parsing
    parser = ArgumentParser(prog=APP_NAME, description=APP_DESCRIPTION)
    parser.add_argument('input', help='input video file', type=Path)
    parser.add_argument('size', help='target file size in megabytes (MB)', type=int)
    parser.add_argument('-o', '--output', help='output video file path', type=Path)
    parser.add_argument('-t', '--tolerance', help='tolerance', default=2.0, type=float)
    parser.add_argument('-i', '--iterations', help='iterations', default=15, type=int)
    parser.add_argument('-q', '--quality', help='quality', action='count', default=0)
    parser.add_argument('-v', '--version', help='displays the application version', action='version', version=f'%(prog)s {APP_VERSION_STRING}')

    args = parser.parse_args()
    #endregion

    #region Argument validation
    input_file = cast(Path, args.input)
    if not input_file.exists():
        tui.writeln('Input video file does not exist', severity=MessageSeverity.ERROR)
        return 1

    target_size_mb = cast(int, args.size)
    if target_size_mb < 1:
        tui.writeln('Size must be greater than 0', severity=MessageSeverity.ERROR)
        return 1

    output_file = cast(Path, args.output)
    if output_file is None:
        output_file = input_file.with_name(f'{input_file.stem}-squashed-{monotonic_ns()}{input_file.suffix}')

    tolerance = cast(float, args.tolerance)
    if tolerance <= 0.0 or tolerance > 50.0:
        tui.writeln('Tolerance must be between 0 and 50', severity=MessageSeverity.ERROR)
        return 1

    iterations = cast(int, args.iterations)
    if iterations <= 0:
        tui.writeln('Max iterations must be greater than 0', severity=MessageSeverity.ERROR)
        return 1

    quality = max(1, min(args.quality, 4))
    #endregion

    try:
        results = _resize_video_to_target(input_file, target_size_mb, output_file, tolerance, iterations, quality)
        tui.writeln('-' * 64, severity=MessageSeverity.SUCCESS)
        tui.writeln(f'Status: {'Success' if results['success'] else 'Partial'}', severity=MessageSeverity.SUCCESS)
        tui.writeln(f'Output: {results['file_path']}', severity=MessageSeverity.SUCCESS)
        tui.writeln(f'Target Size: {_format_bytes(results['target_size'])}', severity=MessageSeverity.SUCCESS)
        tui.writeln(f'Final Size: {_format_bytes(results['file_size'])}', severity=MessageSeverity.SUCCESS)
        tui.writeln(f'Iterations: {results['iteration']}/{iterations}', severity=MessageSeverity.SUCCESS)
        tui.writeln(f'Final Bitrate: {results['bitrate']:.0f}', severity=MessageSeverity.SUCCESS)
        return 0
    except MissingBinaryException as e:
        tui.writeln(e, severity=MessageSeverity.ERROR)
        return 1
    except Exception as e:
        tui.writeln(e, severity=MessageSeverity.ERROR)
        return 1

if __name__ == '__main__':
    exit(main())
