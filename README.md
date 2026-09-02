# Squash

A tool for compressing videos to a target size while preserving as much quality as possible, primarily designed for use with Discord.

## Requirements

Squash requires [FFmpeg and FFprobe](https://ffmpeg.org/) to be installed on your system. If they are not found, Squash can download the latest FFmpeg Essentials build and verify its SHA-256 checksum before extracting it.

## Installation

Squash can be installed using the setup available from the [latest release.](https://github.com/depthbomb/squash/releases/latest)

## Options

### Target size

The target size is a strict upper limit expressed in decimal megabytes, where 1 MB is 1,000,000 bytes. Squash measures the encoded video payload, preserved audio, and container overhead while refining the video bitrate, then saves the largest successful result that does not exceed the target.

If the input can be remuxed below the target without re-encoding its video, Squash uses that lossless result instead.

### Audio

The **Include original audio** option copies every compatible source audio stream without re-encoding it. This preserves the original audio quality and accounts for the measured audio size when calculating the remaining video budget.

Disable this option to completely omit audio and make the full available budget usable by the video stream. If an included audio format cannot be copied into the MP4 output, or the preserved audio leaves insufficient space for viable video, Squash reports the problem instead of silently reducing audio quality.

### Tolerance

The tolerance option determines how close, as a percentage, an encoded file must be to the target size to be considered successful. By default, this value is set to 2%, meaning that if the target size is 10 MB, a resulting file size of 9.8 MB or larger will be considered successful.

Setting this value to 0 is technically possible, but it may require significantly more iterations than the default maximum. (See the section below.)

### Max iterations

The max iterations option determines the maximum number of encoding iterations that can be performed for a video file. If this limit is reached and the resulting file size from the final iteration is below the target size, that file will be saved. The default value is 15.

### Quality preset

|         Level | Codec | Encoder settings |
|--------------:|-------|------------------|
|             1 | H.264 | x264 `medium` |
|   2 (default) | H.265 | x265 `medium` |
|             3 | H.265 | x265 `slow` |
|             4 | H.265 | x265 `veryslow` |
|             5 | AV1 | SVT-AV1 preset 6 or libaom `cpu-used 2` |

The preset option determines the FFmpeg codec and preset used during encoding, which greatly affects both the resulting video quality and the amount of time each encoding iteration takes.

The first level (1) uses H.264 with the x264 `medium` preset. It offers the broadest playback compatibility and the fastest default encoding option, but it is generally less efficient at restrictive file sizes.

The second level (2), which is the default, uses H.265 with the x265 `medium` preset. It typically retains more quality than H.264 at the same size, but takes longer to encode and is not supported by every player.

The third level (3) uses H.265 with the x265 `slow` preset. This further increases encoding time per iteration for only a marginal improvement in quality.

The fourth level (4) uses H.265 with the x265 `veryslow` preset. This can result in extremely long encoding times for each iteration.

The fifth level (5) is an experimental AV1 mode. Squash prefers SVT-AV1 when the installed FFmpeg build provides it and otherwise falls back to libaom. AV1 can offer better compression efficiency, but it is substantially slower—especially through libaom—and has more limited playback compatibility. Its advantage over H.265 depends on the source and target and is not guaranteed.

All presets use two-pass encoding and measured target-size refinement. H.264 and H.265 preserve supported source bit depth and chroma formats instead of always converting to 4:2:0; AV1 capabilities depend on the encoder available in the installed FFmpeg build.

> [!WARNING]
> Presets 3 through 5 are not selectable by default because they can take a very long time even for reasonable targets. Enable additional quality presets in Settings to use them.

## Planned features

- [x] Automatic FFmpeg/FFprobe detection, checksum-verified download, and extraction
- [x] Notifications
- [ ] Queue support for multiple video files with parallel encodings
