# Video quality benchmark

The benchmark workflow compares completed files at their actual byte sizes. It does not treat a codec's requested bitrate as the final size and does not assign a universal “noticeable” threshold to VMAF.

## Encode a candidate with Squash itself

```powershell
dotnet run --project Squash.Tools --configuration Release -- \
  encode reference.mp4 candidate.mp4 20000000 2 0.25 8 true
```

The final argument controls audio. Use `false` to omit all audio; size budgeting and output validation then allocate zero bytes to audio.

Preset 5 prefers `libsvtav1` when the installed FFmpeg provides it and otherwise uses `libaom-av1`, which is present in the automatically downloaded Essentials build.

## Compare baseline and candidate

```powershell
./benchmarks/Compare-VideoQuality.ps1 \
  -Reference reference.mp4 \
  -Baseline baseline.mp4 \
  -Candidate candidate.mp4 \
  -OutputDirectory artifacts/video-quality/run-001
```

The script records SHA-256 hashes, actual bytes, FFmpeg version, stream/color/timing metadata, per-frame metric logs, and pooled VMAF, PSNR, SSIM, MS-SSIM, CIEDE2000, and CAMBI results.

Before accepting a result:

- require both files to be at or below the applicable target;
- compare closely matched actual video-byte budgets or use multiple rate-distortion points;
- reject frame-count, timing, geometry, pixel-format, color, HDR, or promised-stream regressions before interpreting metrics;
- inspect per-clip and worst-frame results, not only pooled means;
- use randomized, blinded A/B viewing under controlled conditions before calling a difference “noticeable”;
- report encoder-only and end-to-end runtime separately.
