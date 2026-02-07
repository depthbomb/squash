from json import loads
from pathlib import Path
from os import environ, pathsep
from tempfile import gettempdir
from argparse import ArgumentParser
from sys import exit, stdout, stderr
from time import perf_counter, monotonic_ns
from subprocess import run, PIPE, Popen, DEVNULL
from typing import cast, Literal, Optional, TypedDict

from squash.lib.http import HTTPClient
from squash.lib.host import is_in_windows_terminal
from squash.lib.tui import EOL, Tui, MessageSeverity
from squash import APP_NAME, BINARY_PATH, SEVENZIP_PATH, APP_DESCRIPTION, APP_VERSION_STRING

BYTES_PER_MEGABYTE = 1024 * 1024
MIN_VIDEO_BITRATE = 100
AUDIO_BITRATE = 128
CONTAINER_OVERHEAD = 0.97

class EncodeResults(TypedDict):
    success: bool
    file_path: Path
    file_size: int
    target_size: int
    iteration: int
    bitrate: float
    elapsed_seconds: float

tui = Tui(stdout, stderr)

def _safe_unlink(path: Path) -> None:
    path.unlink(missing_ok=True)

def _get_video_info(path: Path) -> float:
    ffprobe_path = _get_binary_path('ffprobe')
    proc = Popen(
        [ffprobe_path, '-v', 'error', '-show_entries', 'format=duration', '-of', 'json', path],
        stdout=PIPE,
        text=True,
    )

    out, _ = proc.communicate()
    data = loads(out)

    return float(data['format']['duration'])

def _get_file_size(path: Path) -> int:
    return path.stat().st_size

def _format_bytes(size: int, precision: int = 2) -> str:
    if size < 0:
        raise ValueError('size must be non-negative')

    units = ('B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB')
    unit_index = 0
    while size >= 1024 and unit_index < len(units) - 1:
        size /= 1024
        unit_index += 1

    if unit_index == 0:
        return f'{int(size)}{units[unit_index]}'

    return f'{size:.{precision}f}{units[unit_index]}'

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

def _format_duration(total_seconds: float) -> str:
    if total_seconds < 0:
        total_seconds = 0

    total_seconds = int(total_seconds)
    hours, rem = divmod(total_seconds, 3600)
    minutes, seconds = divmod(rem, 60)

    if hours:
        return f'{hours}h {minutes:02d}m {seconds:02d}s'
    if minutes:
        return f'{minutes}m {seconds:02d}s'
    return f'{seconds}s'

def _parse_speed_multiplier(speed: Optional[str]) -> Optional[float]:
    if not speed:
        return None

    if speed.endswith('x'):
        speed = speed[:-1]

    try:
        return float(speed)
    except ValueError:
        return None

def _calculate_target_bitrate(duration: float, target_size_bytes: int, audio_bitrate: int) -> float:
    total_bitrate = (target_size_bytes * 8) / duration / 1_000
    return (total_bitrate - audio_bitrate) * CONTAINER_OVERHEAD

def _build_progress_status(progress: dict[str, str], duration: float) -> str:
    out_time_ms = progress.get('out_time_ms')
    fps = progress.get('fps')
    bitrate = progress.get('bitrate')
    speed = progress.get('speed')
    percent = None

    if out_time_ms and duration > 0:
        try:
            out_seconds = int(out_time_ms) / 1_000_000
            percent = min(100.0, (out_seconds / duration) * 100.0)
        except ValueError:
            percent = None

    eta = None
    speed_multiplier = _parse_speed_multiplier(speed)
    if speed_multiplier and out_time_ms:
        try:
            out_seconds = int(out_time_ms) / 1_000_000
            remaining = max(0.0, duration - out_seconds)
            eta = remaining / speed_multiplier if speed_multiplier > 0 else None
        except ValueError:
            eta = None

    status_parts = []
    if percent is not None:
        status_parts.append(f'{percent:5.1f}%')
    if speed:
        status_parts.append(f'speed {speed}')
    if fps:
        status_parts.append(f'fps {fps}')
    if bitrate:
        status_parts.append(f'br {bitrate}')
    if eta is not None:
        status_parts.append(f'eta ~{_format_duration(eta)}')

    return ' | '.join(status_parts)

def _encode_video(path: Path, output: Path, video_bitrate: float, audio_bitrate: int, quality: int, duration: float) -> None:
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
        wrote_progress = False
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
                status = _build_progress_status(progress, duration)
                status_text = f'Encoding... {tui.dim(status)}' if status else 'Encoding...'
                tui.write(status_text)
                wrote_progress = True

        proc.wait()

        if wrote_progress:
            tui.stdout.write(EOL)

def _build_results(
    *,
    success: bool,
    file_path: Path,
    file_size: int,
    target_size: int,
    iteration: int,
    bitrate: float,
    total_start: float,
) -> EncodeResults:
    return {
        'success': success,
        'file_path': file_path,
        'file_size': file_size,
        'target_size': target_size,
        'iteration': iteration,
        'bitrate': bitrate,
        'elapsed_seconds': perf_counter() - total_start,
    }

def _resize_video_to_target(
    input_file: Path,
    target_size_mb: int,
    output_file: Path,
    tolerance: float,
    iterations: int,
    quality: int,
) -> EncodeResults:
    total_start = perf_counter()
    tui.writeln(f'Analyzing {tui.bold(str(input_file))}...')

    target_size_bytes = target_size_mb * BYTES_PER_MEGABYTE
    tolerance_bytes = target_size_bytes * (tolerance / 100)

    current_video_size = _get_file_size(input_file)

    tui.writeln(f'Current size: {tui.bold(_format_bytes(current_video_size))}')

    if current_video_size <= target_size_bytes:
        tui.writeln('Video is already at or under target size. No encoding needed.', severity=MessageSeverity.SUCCESS)
        return _build_results(
            success=True,
            file_path=input_file,
            file_size=current_video_size,
            target_size=target_size_bytes,
            iteration=0,
            bitrate=0,
            total_start=total_start,
        )

    video_duration = _get_video_info(input_file)
    target_bitrate = _calculate_target_bitrate(video_duration, target_size_bytes, AUDIO_BITRATE)
    min_bitrate = MIN_VIDEO_BITRATE
    max_bitrate = target_bitrate * 2
    current_bitrate = target_bitrate

    iteration = 0

    temp_output = Path(gettempdir(), f'squash-temp-{monotonic_ns()}.mp4')
    try:
        while iteration < iterations:
            iteration += 1

            iteration_start = perf_counter()
            tui.writeln(f'Iteration {iteration} of {iterations}: encoding at {current_bitrate:.0f}kbps with a {tolerance}% tolerance...')

            _encode_video(input_file, temp_output, current_bitrate, AUDIO_BITRATE, quality, video_duration)

            new_file_size = _get_file_size(temp_output)
            iteration_seconds = perf_counter() - iteration_start
            size_difference = new_file_size - target_size_bytes
            percent_difference = size_difference / target_size_bytes * 100
            is_positive_difference = percent_difference < 0
            styled_percent_difference = tui.color_success(f'{percent_difference:.2f}%') if is_positive_difference else tui.color_error(f'+{percent_difference:.2f}%')

            tui.writeln(f'Result: {tui.bold(_format_bytes(new_file_size))} ({styled_percent_difference}) in {_format_duration(iteration_seconds)}')

            if new_file_size < target_size_bytes:
                gap_to_target = target_size_bytes - new_file_size
                if gap_to_target < tolerance_bytes:
                    tui.writeln(f'Target achieved! Moving file to {output_file.name}')
                    temp_output.replace(output_file)
                    return _build_results(
                        success=True,
                        file_path=output_file,
                        file_size=new_file_size,
                        target_size=target_size_bytes,
                        iteration=iteration,
                        bitrate=current_bitrate,
                        total_start=total_start,
                    )
                else:
                    tui.writeln(f'Too far below target ({tui.bold(_format_bytes(gap_to_target))} gap), increasing bitrate for next iteration')
                    min_bitrate = current_bitrate
            else:
                tui.writeln('Over target, reducing bitrate for next iteration')
                max_bitrate = current_bitrate

            current_bitrate = (min_bitrate + max_bitrate) / 2
            if current_bitrate < MIN_VIDEO_BITRATE:
                tui.writeln('Bitrate too low. Cannot achieve target size without severe quality loss.')
                break

        final_size = _get_file_size(temp_output)
        if final_size <= target_size_bytes:
            tui.writeln(f'Max iterations reached, using best result under target.')
            temp_output.replace(output_file)
            return _build_results(
                success=False,
                file_path=output_file,
                file_size=final_size,
                target_size=target_size_bytes,
                iteration=iteration,
                bitrate=current_bitrate,
                total_start=total_start,
            )
        else:
            raise Exception(f'Could not achieve target size after {iterations} iterations. Final result was {_format_bytes(final_size)} (over target).')
    except KeyboardInterrupt:
        raise
    finally:
        _safe_unlink(temp_output)

def _find_in_path(bin_name: str) -> Optional[Path]:
    for directory in environ.get('PATH', '').split(pathsep):
        candidate = Path(directory) / f'{bin_name}.exe'
        if candidate.is_file():
            return candidate

    return None

def _get_binary_path(name: Literal['ffmpeg', 'ffprobe']) -> Optional[Path]:
    binary_path = (Path(BINARY_PATH).parent / name).with_suffix('.exe')
    if binary_path.exists():
        return binary_path

    return _find_in_path(name)

def _has_required_binaries() -> bool:
    has_ffmpeg = _get_binary_path('ffmpeg')
    has_ffprobe = _get_binary_path('ffprobe')

    return has_ffmpeg is not None and has_ffprobe is not None

def main() -> int:
    #region Windows Terminal check
    if not is_in_windows_terminal():
        tui.stdout.write(tui.boxed('It is highly recommended you use a command line shell like Windows Terminal for the best experience: https://apps.microsoft.com/detail/9n0dx20hk701') + EOL)
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

        http = HTTPClient()
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
        status = 'Success' if results['success'] else 'Partial'
        actual_size = _format_bytes(results['file_size'])
        target_size = _format_bytes(results['target_size'])
        delta = results['file_size'] - results['target_size']
        delta_sign = '+' if delta > 0 else ''
        delta_text = f'{delta_sign}{_format_bytes(abs(delta))}'
        tui.writeln(
            (
                f'{status}: wrote {results['file_path']}; '
                f'final size {actual_size} (target {target_size}, delta {delta_text}); '
                f'iterations {results['iteration']}/{iterations}; '
                f'final bitrate {results['bitrate']:.0f} kbps; '
                f'total time {_format_duration(results['elapsed_seconds'])}.'
            ),
            severity=MessageSeverity.SUCCESS,
        )
        return 0
    except KeyboardInterrupt:
        tui.writeln('Encoding cancelled.', severity=MessageSeverity.WARNING)
        return 130
    except Exception as e:
        tui.writeln(e, severity=MessageSeverity.ERROR)
        return 1

if __name__ == '__main__':
    exit(main())
