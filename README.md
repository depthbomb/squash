# Squash

A tool for compressing videos to a target size while preserving as much quality as possible, primarily designed for use with Discord.

## Requirements

Squash requires [FFmpeg and FFprobe](https://ffmpeg.org/) to be installed on your system. If they are not found, Squash can optionally download them for you.

## Installation

Squash can be installed using the setup available from the [latest release.](https://github.com/depthbomb/squash/releases/latest)

## Options

### Tolerance

The tolerance option determines how close, as a percentage, an encoded file must be to the target size to be considered successful. By default, this value is set to 2%, meaning that if the target size is 10 MB, a resulting file size of 9.98 MB will be considered successful.

Setting this value to 0 is technically possible, but it may require significantly more iterations than the default maximum. (See the section below.)

### Max iterations

The max iterations option determines the maximum number of encoding iterations that can be performed for a video file. If this limit is reached and the resulting file size from the final iteration is below the target size, that file will be saved. The default value is 15.

### Preset

|         Level | Codec | FFmpeg Preset |
|--------------:|-------|---------------|
|             1 | h264  | medium        |
|   2 (default) | h265  | medium        |
|             3 | h265  | slow          |
|             4 | h265  | veryslow      |

The preset option determines the FFmpeg codec and preset used during encoding, which greatly affects both the resulting video quality and the amount of time each encoding iteration takes.

The first level (1) uses the h264 codec with the "medium" FFmpeg preset. This is suitable for quickly encoding videos with a reasonable size reduction target, such as compressing a 30 MB file to 10 MB, while keeping quality loss minimal but still noticeable.

The second level (2), which is the default, uses the h265 codec with the "medium" preset. This typically results in encoding times that are roughly twice as long per iteration, but with a noticeable improvement in quality. Keep in mind that h265 is not supported everywhere. For example, Firefox cannot play h265 videos directly in the browser. Discord, which this tool was primarily designed for, does support the codec.

The third level (3) uses the h265 codec with the "slow" preset. This further increases encoding time per iteration for only a marginal improvement in quality.

The final level (4) uses the h265 codec with the "veryslow" preset. This can result in extremely long encoding times for each iteration.

> [!WARNING]
> The latter two presets are not selectable by default because they can take a very long time even for reasonable targets while providing only minor improvements in quality.

## Planned features

- [ ] Smoother FFmpeg/FFprobe detection, download, and extraction flow
- [x] Notifications
- [ ] Queue support for multiple video files with parallel encodings
