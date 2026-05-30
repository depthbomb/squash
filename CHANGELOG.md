# 05/30/2026

## 3.2.4

- Side navigation now uses your system's accent color

# 05/29/2026

## 3.2.3

- The current input file's size will be displayed under the thumbnail preview

# 05/28/2026

## 3.2.2

- The application's window will now open at the center of the active screen

# 05/22/2026

## 3.2.1

- Fixed notifications displaying when _Enable notifications_ is disabled
- Fixed a blank progress notification being displayed if the input file is already smaller than the target size
- Improved the formatting of progress notifications
- Generated thumbnails are now cropped to a 16:9 ratio
- Completion notification now displays a thumbnail of the output file
- Removed _Allow multiple instances_ setting

# 05/21/2026

## 3.2.0

- Overhauled UI
- The selected video file's thumbnail is now displayed, double-clicking will open the video in your default application
- Added settings
- Added notifications, which can be disabled in settings
- Quality presets 3 and 4 are now not selectable by default and must be enabled in settings
- Improved file picker and save file dialogs

# 05/16/2026

## 3.1.0

- Improved messages when an operation has completed, including failed operations
- Added an about window

# 05/15/2026

## 3.0.1

- Fixed controls staying enabled during encoding
- Minor optimizations

# 05/14/2026

## 3.0.0

- Complete rewritten in C# running on .NET 10, resulting in a significantly smaller file size and improved performance
- Now includes an auto-updater that handles downloading and installing the required .NET runtime for you

# 02/19/2026

## 2.2.0

- Added automatic light/dark theme
- The app will now offer to automatically download FFmpeg/FFprobe for you if it isn't found

# 02/16/2026

## 2.1.0

- Fixed dragging a file onto the app not working if the file was dropped on a component within the frame
- Adjusted app theme
- Added app info

# 02/15/2026

## 2.0.0

- Rewritten to be a desktop application
  - Input file, output file, and other options are now easily configured within the GUI
  - You can also drag a video file into the window to select it as the input file
  - Automatic downloading of FFmpeg/FFprobe is not implemented yet, instead they are included in the setup
- The last release of the CLI version, 1.3.0, will remain available to download but will not receive any further releases

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
