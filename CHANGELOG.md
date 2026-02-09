# 02/09/2026

## 1.3.0

- Implemented a smarter method of selecting bitrates for encoding iterations that can reduce the overall time it takes to compress a video
- Other optimizations and improvements

# 02/07/2026

## 1.2.0

- Updated the UX of encoding progress
  - Now displays the encoding percentage
  - Now displays an approximate ETA until encoding is finished
  - Now updates a single line instead of printing one line per progress step
- Iterations are now timed and the time an iteration took is displayed after encoding is done
- Total time taking for the command is now displayed in the final results

# 02/05/2026

## 1.1.0

- If FFmpeg and/or FFprobe are missing from your system then you will be asked if you want Squash to download them for you
- Added a recommendation for Windows Terminal if it isn't being used
- Added `-v`/`--version` flag

## 1.0.1

- Updated logging prefix

---

# 02/04/2026

## 1.0.0

- Initial release
