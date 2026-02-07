# squash

A CLI tool to compress a video to a target size while maintaining as much quality as possible, primarily made with Discord in mind.

---

## Basic usage

```shell
squash ./my-video.mp4 10
```

> [!IMPORTANT]
> Squash isn't magic. It can't encode a 150MB video down to 10MB without a severe loss in quality; it is just not possible. You should aim for reasonable targets based on the original video file size.

## Requirements

Squash requires [FFmpeg and FFprobe](https://ffmpeg.org/) either in your system PATH or alongside the application's executable. You will be prompted to allow Squash to download them for you.

## Installation

Currently only Windows is supported and can be installed via a setup downloaded from the [releases.](https://github.com/depthbomb/squash/releases/latest)

The setup can automatically add Squash to your system's PATH on installation and remove it on uninstallation.

## Options

### `-o` / `--output`

```shell
squash ./my-video.mp4 10 --output ./my-smaller-video.mp4
```

The output option lets you specify the name and location of the resulting encoded video file. If this option is omitted, then the video file will be given a unique name and saved to the same directory as the input file.

### `-t` / `--tolerance`

```shell
squash ./my-video.mp4 10 --tolerance 1.5
```

The tolerance option lets you set how close, as a percentage, an encoding has to be under the target size to be considered a successful result. By default it is 2% which means if you are targeting 10MB then 9.98MB will be considered a success.

Setting this to 0 can technically work but may take many more iterations than the default maximum. (see section below)

### `-i` / `--iterations`

```shell
squash ./my-video.mp4 10 --iterations 20
```

The iterations option lets you set the maximum number of encoding iterations that will be performed on a video file. If this number is reached and that iteration's resulting file size is below the target, then that is the file that will be saved. Defaults to 15.

### `-q` / `--quality`

```shell
squash ./my-video.mp4 10 --quality 2
```

|       Level | Codec | FFmpeg Preset | Notes                    |
|------------:|-------|---------------|--------------------------|
| 1 (default) | h264  | medium        | Fast, reasonable quality |
|           2 | h265  | medium        | Slower, better quality   |
|           3 | h265  | slow          | Very slow, small gain    |
|           4 | h265  | veryslow      | Extremely slow           |

The quality option determines the codec and preset for FFmpeg that greatly affects both the quality of the resulting video and how long it takes each iteration of encoding.

By default, the quality setting is set to 1, the lowest, which will use the h264 codec and the "medium" preset in FFmpeg. This is good enough for quickly encoding videos with a reasonable size-to-target gap (for example 30MB to 10MB) with minimal but noticeable quality loss.

The second level (2) uses the h265 codec and the "medium" preset. This results in about a 2x longer encoding time each iteration but a noticeable improvement in quality. Do note that h265 may not be supported everywhere. Firefox, for example, cannot play h265 files directly in the browser. Discord, what this tool was made for, does support the codec.

The third level (3) uses the h265 codec and the "slow" preset. This results in an even longer encoding time each iteration for a marginal improvement in quality.

The final level (4) uses the h265 codec and the "veryslow" preset. This results in an absurdly long encoding time each iteration.

The quality option can be defined using `--quality 2` or using a shorter syntax: `-qq`

> [!WARNING]
> You should stick to the first two levels as the latter two can take a very long time, even for reasonable targets, for a potentially-marginal improvement in quality.
