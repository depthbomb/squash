package com.caprinelogic.service;

import org.springframework.stereotype.Component;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.*;
import java.util.function.BooleanSupplier;

@Component
public class EncodeService {
    private static final int BYTES_PER_MEGABYTE = 1024 * 1024;
    private static final int MIN_VIDEO_BITRATE = 100;
    private static final int MIN_AUDIO_BITRATE = 32;
    private static final int DEFAULT_AUDIO_BITRATE = 128;
    private static final double CONTAINER_OVERHEAD = 0.97;

    private final BinaryService binaryService;

    public EncodeService(BinaryService binaryService) {
        this.binaryService = binaryService;
    }

    public record EncodeRequest(
        Path inputFile,
        Path outputFile,
        int targetSizeMb,
        double tolerancePercent,
        int maxIterations,
        int qualityPreset
    ) {}

    public record EncodeResult(
        boolean success,
        Path filePath,
        long fileSizeBytes,
        long targetSizeBytes,
        int iteration,
        double videoBitrateKbps,
        double elapsedSeconds
    ) {}

    public record ProgressUpdate(int progressPercent, String titleText) {}

    public interface ProgressListener {
        void onUpdate(ProgressUpdate update);
    }

    public EncodeResult resizeVideoToTarget(
        EncodeRequest request,
        ProgressListener progressListener,
        BooleanSupplier cancelled
    ) throws Exception {
        validateRequest(request);

        var startedAtNanos = System.nanoTime();
        var ffprobePath = requireBinaryPath("ffprobe", "FFprobe was not found.");
        var ffmpegPath = requireBinaryPath("ffmpeg", "FFmpeg was not found.");
        var targetSizeBytes = (long) request.targetSizeMb() * BYTES_PER_MEGABYTE;
        var toleranceBytes = targetSizeBytes * (request.tolerancePercent() / 100.0);
        var currentVideoSize = Files.size(request.inputFile());
        if (currentVideoSize <= targetSizeBytes) {
            return new EncodeResult(
                true,
                request.inputFile(),
                currentVideoSize,
                targetSizeBytes,
                0,
                0,
                elapsedSecondsSince(startedAtNanos)
            );
        }

        var videoInfo = getVideoInfo(ffprobePath, request.inputFile());
        var duration = videoInfo.durationSeconds();
        if (duration <= 0) {
            throw new IllegalStateException("Input video duration is invalid or unavailable.");
        }

        var sourceBitrate = (double) videoInfo.videoBitrateKbps().orElse((currentVideoSize * 8.0) / duration / 1_000.0);
        var audioBitrate = selectAudioBitrate(duration, targetSizeBytes, DEFAULT_AUDIO_BITRATE);
        var targetBitrate = calculateTargetBitrate(duration, targetSizeBytes, audioBitrate);
        var minBitrate = MIN_VIDEO_BITRATE;
        var maxBitrate = targetBitrate * 2;

        if (sourceBitrate > 0) {
            var sourceVideoCap = Math.max(MIN_VIDEO_BITRATE, sourceBitrate - audioBitrate);
            maxBitrate = Math.min(maxBitrate, sourceVideoCap * 1.1);
        }

        maxBitrate = Math.max(maxBitrate, targetBitrate);

        var currentBitrate = targetBitrate;
        var bestUnder = (Sample) null;
        var bestOver = (Sample) null;
        var lastEncodedSize = (Long) null;
        var lastEncodedBitrate = (Double) null;
        var tempOutput = Files.createTempFile("squash-temp-", ".mp4");

        try {
            int iteration = 0;

            while (iteration < request.maxIterations()) {
                throwIfCancelled(cancelled);
                iteration++;

                progressListener.onUpdate(new ProgressUpdate(
                    0,
                    String.format(
                        Locale.US,
                        "Iteration %d/%d at %.0f kbps",
                        iteration,
                        request.maxIterations(),
                        currentBitrate
                    )
                ));

                encodeVideo(
                    ffmpegPath,
                    request.inputFile(),
                    tempOutput,
                    currentBitrate,
                    audioBitrate,
                    request.qualityPreset(),
                    duration,
                    iteration,
                    request.maxIterations(),
                    progressListener,
                    cancelled
                );

                var newFileSize = Files.size(tempOutput);
                lastEncodedSize = newFileSize;
                lastEncodedBitrate = currentBitrate;

                if (newFileSize < targetSizeBytes) {
                    if (bestUnder == null || newFileSize > bestUnder.fileSize()) {
                        bestUnder = new Sample(currentBitrate, newFileSize);
                    }

                    var gapToTarget = targetSizeBytes - newFileSize;
                    if (gapToTarget < toleranceBytes) {
                        Files.move(tempOutput, request.outputFile(), StandardCopyOption.REPLACE_EXISTING);
                        return new EncodeResult(
                            true,
                            request.outputFile(),
                            newFileSize,
                            targetSizeBytes,
                            iteration,
                            currentBitrate,
                            elapsedSecondsSince(startedAtNanos)
                        );
                    }

                    minBitrate = (int) currentBitrate;
                } else {
                    if (bestOver == null || newFileSize < bestOver.fileSize()) {
                        bestOver = new Sample(currentBitrate, newFileSize);
                    }

                    maxBitrate = currentBitrate;
                }

                currentBitrate = estimateNextBitrate(
                    currentBitrate,
                    newFileSize,
                    targetSizeBytes,
                    minBitrate,
                    maxBitrate,
                    bestUnder,
                    bestOver
                );

                if (currentBitrate < MIN_VIDEO_BITRATE) {
                    break;
                }
            }

            var finalSize = lastEncodedSize != null ? lastEncodedSize : Files.size(tempOutput);
            var finalBitrate = lastEncodedBitrate != null ? lastEncodedBitrate : currentBitrate;
            if (finalSize <= targetSizeBytes) {
                Files.move(tempOutput, request.outputFile(), StandardCopyOption.REPLACE_EXISTING);
                return new EncodeResult(
                    false,
                    request.outputFile(),
                    finalSize,
                    targetSizeBytes,
                    request.maxIterations(),
                    finalBitrate,
                    elapsedSecondsSince(startedAtNanos)
                );
            }

            if (bestOver != null) {
                throw new IllegalStateException(
                    String.format(
                        Locale.US,
                        "Could not reach target after %d iterations. Closest over-target result was %s at %.0f kbps.",
                        request.maxIterations(),
                        formatBytes(bestOver.fileSize()),
                        bestOver.bitrateKbps()
                    )
                );
            }

            throw new IllegalStateException(
                String.format(
                    Locale.US,
                    "Could not reach target after %d iterations. Final result was %s.",
                    request.maxIterations(),
                    formatBytes(finalSize)
                )
            );
        } finally {
            Files.deleteIfExists(tempOutput);
        }
    }

    private void validateRequest(EncodeRequest request) {
        if (!Files.isRegularFile(request.inputFile())) {
            throw new IllegalArgumentException("Input file does not exist.");
        }

        if (request.targetSizeMb() < 1) {
            throw new IllegalArgumentException("Target size must be greater than 0.");
        }

        if (request.tolerancePercent() <= 0.0 || request.tolerancePercent() > 50.0) {
            throw new IllegalArgumentException("Tolerance must be between 0 and 50.");
        }

        if (request.maxIterations() <= 0) {
            throw new IllegalArgumentException("Max iterations must be greater than 0.");
        }

        var quality = request.qualityPreset();
        if (quality < 1 || quality > 4) {
            throw new IllegalArgumentException("Quality preset must be between 1 and 4.");
        }

        if (request.inputFile().normalize().equals(request.outputFile().normalize())) {
            throw new IllegalArgumentException("Output file cannot be the same as input file.");
        }
    }

    private VideoInfo getVideoInfo(Path ffprobePath, Path inputFile) throws Exception {
        var command = List.of(
            ffprobePath.toString(),
            "-v", "error",
            "-show_entries", "format=duration,bit_rate",
            "-of", "default=noprint_wrappers=1:nokey=1",
            inputFile.toString()
        );

        var process = new ProcessBuilder(command).redirectErrorStream(true).start();
        var lines = new ArrayList<String>();
        try (var reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
            var line = "";
            while ((line = reader.readLine()) != null) {
                if (!line.isBlank()) {
                    lines.add(line.trim());
                }
            }
        }

        var exit = process.waitFor();
        if (exit != 0 || lines.isEmpty()) {
            throw new IllegalStateException("FFprobe failed to read input video metadata.");
        }

        double duration;
        try {
            duration = Double.parseDouble(lines.getFirst());
        } catch (NumberFormatException exception) {
            throw new IllegalStateException("FFprobe returned an invalid duration value.", exception);
        }

        Optional<Double> bitrateKbps = Optional.empty();
        if (lines.size() > 1) {
            var raw = lines.get(1);
            if (!"N/A".equalsIgnoreCase(raw)) {
                try {
                    bitrateKbps = Optional.of(Double.parseDouble(raw) / 1_000.0);
                } catch (NumberFormatException ignored) {
                    bitrateKbps = Optional.empty();
                }
            }
        }

        return new VideoInfo(duration, bitrateKbps);
    }

    private void encodeVideo(
        Path ffmpegPath,
        Path inputFile,
        Path outputFile,
        double videoBitrate,
        int audioBitrate,
        int qualityPreset,
        double duration,
        int iteration,
        int maxIterations,
        ProgressListener progressListener,
        BooleanSupplier cancelled
    ) throws Exception {
        var command = new ArrayList<String>();
        command.add(ffmpegPath.toString());
        command.add("-hide_banner");
        command.add("-loglevel");
        command.add("error");
        command.add("-y");
        command.add("-i");
        command.add(inputFile.toString());
        command.add("-b:v");
        command.add(String.format(Locale.US, "%.0fk", videoBitrate));
        command.add("-b:a");
        command.add(audioBitrate + "k");
        command.addAll(getEncodeSettings(qualityPreset));
        command.add("-progress");
        command.add("pipe:1");
        command.add("-nostats");
        command.add(outputFile.toString());

        var process = new ProcessBuilder(command).redirectErrorStream(true).start();
        var progressData = new HashMap<String, String>();
        var lastMessageLine = (String) null;

        try (var reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
            var line = "";
            while ((line = reader.readLine()) != null) {
                throwIfCancelled(cancelled, process);

                line = line.trim();
                if (line.isEmpty()) {
                    continue;
                }

                var separatorIndex = line.indexOf('=');
                if (separatorIndex <= 0) {
                    lastMessageLine = line;
                    continue;
                }

                var key = line.substring(0, separatorIndex);
                var value = line.substring(separatorIndex + 1);
                progressData.put(key, value);

                if ("progress".equals(key) && "continue".equals(value)) {
                    var percent = computePercent(progressData, duration);
                    var status = buildProgressStatus(progressData, duration);
                    var title = String.format(
                        Locale.US,
                        "Iteration %d/%d%s",
                        iteration,
                        maxIterations,
                        status.isBlank() ? "" : " - " + status
                    );
                    progressListener.onUpdate(new ProgressUpdate(percent, title));
                }
            }
        }

        var exitCode = process.waitFor();
        if (exitCode != 0) {
            var message = "FFmpeg failed with exit code " + exitCode + ".";
            if (lastMessageLine != null && !lastMessageLine.isBlank()) {
                message = message + " " + lastMessageLine;
            }
            throw new IllegalStateException(message);
        }

        progressListener.onUpdate(
            new ProgressUpdate(
                100,
                String.format(Locale.US, "Iteration %d/%d complete", iteration, maxIterations)
            )
        );
    }

    private List<String> getEncodeSettings(int qualityPreset) {
        var args = new ArrayList<>(List.of(
            "-c:a", "aac",
            "-profile:v", "main",
            "-movflags", "+faststart",
            "-pix_fmt", "yuv420p"
        ));

        switch (qualityPreset) {
            case 1 -> args.addAll(List.of("-c:v", "libx264", "-preset", "medium"));
            case 2 -> args.addAll(List.of("-c:v", "libx265", "-preset", "medium"));
            case 3 -> args.addAll(List.of("-c:v", "libx265", "-preset", "slow"));
            case 4 -> args.addAll(List.of("-c:v", "libx265", "-preset", "veryslow"));
            default -> throw new IllegalArgumentException("Unexpected quality preset: " + qualityPreset);
        }

        return args;
    }

    private int computePercent(Map<String, String> progress, double duration) {
        if (duration <= 0) {
            return 0;
        }

        var outTimeMicros = progress.get("out_time_ms");
        if (outTimeMicros == null) {
            return 0;
        }

        try {
            var outSeconds = Long.parseLong(outTimeMicros) / 1_000_000.0;
            return (int) Math.max(0, Math.min(100, (outSeconds / duration) * 100.0));
        } catch (NumberFormatException ignored) {
            return 0;
        }
    }

    private String buildProgressStatus(Map<String, String> progress, double duration) {
        var outTimeMicros = progress.get("out_time_ms");
        var speed = progress.get("speed");
        var fps = progress.get("fps");
        var bitrate = progress.get("bitrate");
        var etaSeconds = (Double) null;
        var speedMultiplier = parseSpeedMultiplier(speed);
        if (speedMultiplier != null && outTimeMicros != null && speedMultiplier > 0) {
            try {
                var outSeconds = Long.parseLong(outTimeMicros) / 1_000_000.0;
                var remaining = Math.max(0.0, duration - outSeconds);

                etaSeconds = remaining / speedMultiplier;
            } catch (NumberFormatException ignored) {}
        }

        var parts = new ArrayList<String>();
        if (speed != null && !speed.isBlank()) {
            parts.add("Speed " + speed.trim());
        }
        if (fps != null && !fps.isBlank()) {
            parts.add("FPS " + fps.trim());
        }
        if (bitrate != null && !bitrate.isBlank()) {
            parts.add("BR " + bitrate.trim());
        }
        if (etaSeconds != null) {
            parts.add("ETA " + formatDuration(etaSeconds));
        }

        return String.join(", ", parts);
    }

    private Double parseSpeedMultiplier(String speed) {
        if (speed == null || speed.isBlank()) {
            return null;
        }

        var trimmed = speed.trim();
        if (trimmed.endsWith("x")) {
            trimmed = trimmed.substring(0, trimmed.length() - 1);
        }

        try {
            return Double.parseDouble(trimmed);
        } catch (NumberFormatException ignored) {
            return null;
        }
    }

    private double calculateTargetBitrate(double duration, long targetSizeBytes, int audioBitrate) {
        if (duration <= 0) {
            return MIN_VIDEO_BITRATE;
        }

        var totalBitrate = (targetSizeBytes * 8.0) / duration / 1_000.0;
        var targetVideoBitrate = (totalBitrate - audioBitrate) * CONTAINER_OVERHEAD;

        return Math.max(MIN_VIDEO_BITRATE, targetVideoBitrate);
    }

    private int selectAudioBitrate(double duration, long targetSizeBytes, int defaultAudioBitrate) {
        if (duration <= 0) {
            return defaultAudioBitrate;
        }

        var totalBitrate = (targetSizeBytes * 8.0) / duration / 1_000.0;
        var minVideoTotal = MIN_VIDEO_BITRATE / CONTAINER_OVERHEAD;
        var maxAudioBitrate = totalBitrate - minVideoTotal;
        if (maxAudioBitrate >= defaultAudioBitrate) {
            return defaultAudioBitrate;
        }

        if (maxAudioBitrate <= 0) {
            return MIN_AUDIO_BITRATE;
        }

        var adjusted = (int) maxAudioBitrate;

        return Math.max(MIN_AUDIO_BITRATE, adjusted);
    }

    private double estimateNextBitrate(
        double currentBitrate,
        long currentSize,
        long targetSize,
        double minBitrate,
        double maxBitrate,
        Sample under,
        Sample over
    ) {
        double nextBitrate;

        if (under != null && over != null) {
            var sizeSpan = over.fileSize() - under.fileSize();
            if (sizeSpan > 0) {
                nextBitrate = under.bitrateKbps()
                    + ((targetSize - under.fileSize())
                    * (over.bitrateKbps() - under.bitrateKbps())
                    / (double) sizeSpan);
            } else {
                nextBitrate = (minBitrate + maxBitrate) / 2.0;
            }
        } else {
            if (currentSize > 0) {
                nextBitrate = currentBitrate * (targetSize / (double) currentSize);
            } else {
                nextBitrate = (minBitrate + maxBitrate) / 2.0;
            }
        }

        if (nextBitrate < minBitrate) {
            nextBitrate = minBitrate;
        } else if (nextBitrate > maxBitrate) {
            nextBitrate = maxBitrate;
        }

        if (Math.abs(nextBitrate - currentBitrate) < 1.0) {
            var midpoint = (minBitrate + maxBitrate) / 2.0;
            if (midpoint != nextBitrate) {
                nextBitrate = midpoint;
            }
        }

        return nextBitrate;
    }

    private void throwIfCancelled(BooleanSupplier cancelled) throws InterruptedException {
        if (cancelled.getAsBoolean()) {
            throw new InterruptedException("Encoding canceled");
        }
    }

    private void throwIfCancelled(BooleanSupplier cancelled, Process process) throws InterruptedException {
        if (cancelled.getAsBoolean()) {
            process.destroyForcibly();
            throw new InterruptedException("Encoding canceled");
        }
    }

    private Path requireBinaryPath(String name, String missingMessage) {
        var path = binaryService.getBinaryPath(name);
        if (path == null) {
            throw new IllegalStateException(missingMessage);
        }
        return path;
    }

    private double elapsedSecondsSince(long startedAtNanos) {
        return (System.nanoTime() - startedAtNanos) / 1_000_000_000.0;
    }

    private String formatDuration(double totalSeconds) {
        var seconds = (int) Math.max(0, totalSeconds);
        var hours = seconds / 3600;
        var minutes = (seconds % 3600) / 60;
        var remainder = seconds % 60;

        if (hours > 0) {
            return String.format(Locale.US, "%dh %02dm %02ds", hours, minutes, remainder);
        }
        if (minutes > 0) {
            return String.format(Locale.US, "%dm %02ds", minutes, remainder);
        }

        return remainder + "s";
    }

    private String formatBytes(long bytes) {
        if (bytes < 1024) {
            return bytes + "B";
        }

        var value = (double) bytes;
        var units = new String[]{"B", "KB", "MB", "GB", "TB"};
        var unitIndex = 0;

        while (value >= 1024.0 && unitIndex < units.length - 1) {
            value /= 1024.0;
            unitIndex++;
        }

        return String.format(Locale.US, "%.2f%s", value, units[unitIndex]);
    }

    private record VideoInfo(double durationSeconds, Optional<Double> videoBitrateKbps) {}

    private record Sample(double bitrateKbps, long fileSize) {}
}
