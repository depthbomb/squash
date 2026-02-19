package com.caprinelogic.service

import org.springframework.stereotype.Component
import java.io.BufferedReader
import java.io.InputStreamReader
import java.nio.charset.StandardCharsets
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.StandardCopyOption
import java.util.Locale
import java.util.function.BooleanSupplier
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.min

@Component
class EncodeService(private val binaryService: BinaryService) {
    data class EncodeRequest(
        val inputFile: Path,
        val outputFile: Path,
        val targetSizeMb: Int,
        val tolerancePercent: Double,
        val maxIterations: Int,
        val qualityPreset: Int
    )

    data class EncodeResult(
        val success: Boolean,
        val filePath: Path,
        val fileSizeBytes: Long,
        val targetSizeBytes: Long,
        val iteration: Int,
        val videoBitrateKbps: Double,
        val elapsedSeconds: Double
    )

    data class ProgressUpdate(val progressPercent: Int, val titleText: String)

    fun interface ProgressListener {
        fun onUpdate(update: ProgressUpdate)
    }

    fun resizeVideoToTarget(
        request: EncodeRequest,
        progressListener: ProgressListener,
        cancelled: BooleanSupplier
    ): EncodeResult {
        validateRequest(request)

        val startedAtNanos = System.nanoTime()
        val ffprobePath = requireBinaryPath("ffprobe", "FFprobe was not found.")
        val ffmpegPath = requireBinaryPath("ffmpeg", "FFmpeg was not found.")
        val targetSizeBytes = request.targetSizeMb.toLong() * BYTES_PER_MEGABYTE
        val toleranceBytes = targetSizeBytes * (request.tolerancePercent / 100.0)
        val currentVideoSize = Files.size(request.inputFile)
        if (currentVideoSize <= targetSizeBytes) {
            return EncodeResult(
                success = true,
                filePath = request.inputFile,
                fileSizeBytes = currentVideoSize,
                targetSizeBytes = targetSizeBytes,
                iteration = 0,
                videoBitrateKbps = 0.0,
                elapsedSeconds = elapsedSecondsSince(startedAtNanos)
            )
        }

        val videoInfo = getVideoInfo(ffprobePath, request.inputFile)
        val duration = videoInfo.durationSeconds
        if (duration <= 0.0) {
            throw IllegalStateException("Input video duration is invalid or unavailable.")
        }

        val sourceBitrate = videoInfo.videoBitrateKbps ?: (currentVideoSize * 8.0) / duration / 1_000.0
        val audioBitrate = selectAudioBitrate(duration, targetSizeBytes, DEFAULT_AUDIO_BITRATE)
        val targetBitrate = calculateTargetBitrate(duration, targetSizeBytes, audioBitrate)
        var minBitrate = MIN_VIDEO_BITRATE.toDouble()
        var maxBitrate = targetBitrate * 2

        if (sourceBitrate > 0.0) {
            val sourceVideoCap = max(MIN_VIDEO_BITRATE.toDouble(), sourceBitrate - audioBitrate)
            maxBitrate = min(maxBitrate, sourceVideoCap * 1.1)
        }

        maxBitrate = max(maxBitrate, targetBitrate)

        var currentBitrate = targetBitrate
        var bestUnder: Sample? = null
        var bestOver: Sample? = null
        var lastEncodedSize: Long? = null
        var lastEncodedBitrate: Double? = null
        val tempOutput = Files.createTempFile("squash-temp-", ".mp4")

        try {
            var iteration = 0

            while (iteration < request.maxIterations) {
                throwIfCancelled(cancelled)
                iteration++

                progressListener.onUpdate(
                    ProgressUpdate(
                        0,
                        String.format(
                            Locale.US,
                            "Iteration %d/%d at %.0f kbps",
                            iteration,
                            request.maxIterations,
                            currentBitrate
                        )
                    )
                )

                encodeVideo(
                    ffmpegPath = ffmpegPath,
                    inputFile = request.inputFile,
                    outputFile = tempOutput,
                    videoBitrate = currentBitrate,
                    audioBitrate = audioBitrate,
                    qualityPreset = request.qualityPreset,
                    duration = duration,
                    iteration = iteration,
                    maxIterations = request.maxIterations,
                    progressListener = progressListener,
                    cancelled = cancelled
                )

                val newFileSize = Files.size(tempOutput)
                lastEncodedSize = newFileSize
                lastEncodedBitrate = currentBitrate

                if (newFileSize < targetSizeBytes) {
                    if (bestUnder == null || newFileSize > bestUnder.fileSize) {
                        bestUnder = Sample(currentBitrate, newFileSize)
                    }

                    val gapToTarget = targetSizeBytes - newFileSize
                    if (gapToTarget < toleranceBytes) {
                        Files.move(tempOutput, request.outputFile, StandardCopyOption.REPLACE_EXISTING)
                        return EncodeResult(
                            success = true,
                            filePath = request.outputFile,
                            fileSizeBytes = newFileSize,
                            targetSizeBytes = targetSizeBytes,
                            iteration = iteration,
                            videoBitrateKbps = currentBitrate,
                            elapsedSeconds = elapsedSecondsSince(startedAtNanos)
                        )
                    }

                    minBitrate = currentBitrate
                } else {
                    if (bestOver == null || newFileSize < bestOver.fileSize) {
                        bestOver = Sample(currentBitrate, newFileSize)
                    }

                    maxBitrate = currentBitrate
                }

                currentBitrate = estimateNextBitrate(
                    currentBitrate = currentBitrate,
                    currentSize = newFileSize,
                    targetSize = targetSizeBytes,
                    minBitrate = minBitrate,
                    maxBitrate = maxBitrate,
                    under = bestUnder,
                    over = bestOver
                )

                if (currentBitrate < MIN_VIDEO_BITRATE) {
                    break
                }
            }

            val finalSize = lastEncodedSize ?: Files.size(tempOutput)
            val finalBitrate = lastEncodedBitrate ?: currentBitrate
            if (finalSize <= targetSizeBytes) {
                Files.move(tempOutput, request.outputFile, StandardCopyOption.REPLACE_EXISTING)
                return EncodeResult(
                    success = false,
                    filePath = request.outputFile,
                    fileSizeBytes = finalSize,
                    targetSizeBytes = targetSizeBytes,
                    iteration = request.maxIterations,
                    videoBitrateKbps = finalBitrate,
                    elapsedSeconds = elapsedSecondsSince(startedAtNanos)
                )
            }

            if (bestOver != null) {
                throw IllegalStateException(
                    String.format(
                        Locale.US,
                        "Could not reach target after %d iterations. Closest over-target result was %s at %.0f kbps.",
                        request.maxIterations,
                        formatBytes(bestOver.fileSize),
                        bestOver.bitrateKbps
                    )
                )
            }

            throw IllegalStateException(
                String.format(
                    Locale.US,
                    "Could not reach target after %d iterations. Final result was %s.",
                    request.maxIterations,
                    formatBytes(finalSize)
                )
            )
        } finally {
            Files.deleteIfExists(tempOutput)
        }
    }

    private fun validateRequest(request: EncodeRequest) {
        if (!Files.isRegularFile(request.inputFile)) {
            throw IllegalArgumentException("Input file does not exist.")
        }

        if (request.targetSizeMb < 1) {
            throw IllegalArgumentException("Target size must be greater than 0.")
        }

        if (request.tolerancePercent <= 0.0 || request.tolerancePercent > 50.0) {
            throw IllegalArgumentException("Tolerance must be between 0 and 50.")
        }

        if (request.maxIterations <= 0) {
            throw IllegalArgumentException("Max iterations must be greater than 0.")
        }

        val quality = request.qualityPreset
        if (quality < 1 || quality > 4) {
            throw IllegalArgumentException("Quality preset must be between 1 and 4.")
        }

        if (request.inputFile.normalize() == request.outputFile.normalize()) {
            throw IllegalArgumentException("Output file cannot be the same as input file.")
        }
    }

    private fun getVideoInfo(ffprobePath: Path, inputFile: Path): VideoInfo {
        val command = listOf(
            ffprobePath.toString(),
            "-v", "error",
            "-show_entries", "format=duration,bit_rate",
            "-of", "default=noprint_wrappers=1:nokey=1",
            inputFile.toString()
        )

        val process = ProcessBuilder(command).redirectErrorStream(true).start()
        val lines = mutableListOf<String>()
        BufferedReader(InputStreamReader(process.inputStream, StandardCharsets.UTF_8)).use { reader ->
            while (true) {
                val line = reader.readLine() ?: break
                if (line.isNotBlank()) {
                    lines.add(line.trim())
                }
            }
        }

        val exit = process.waitFor()
        if (exit != 0 || lines.isEmpty()) {
            throw IllegalStateException("FFprobe failed to read input video metadata.")
        }

        val duration = lines.first().toDoubleOrNull()
            ?: throw IllegalStateException("FFprobe returned an invalid duration value.")

        var bitrateKbps: Double? = null
        if (lines.size > 1) {
            val raw = lines[1]
            if (!raw.equals("N/A", ignoreCase = true)) {
                bitrateKbps = raw.toDoubleOrNull()?.div(1_000.0)
            }
        }

        return VideoInfo(duration, bitrateKbps)
    }

    private fun encodeVideo(
        ffmpegPath: Path,
        inputFile: Path,
        outputFile: Path,
        videoBitrate: Double,
        audioBitrate: Int,
        qualityPreset: Int,
        duration: Double,
        iteration: Int,
        maxIterations: Int,
        progressListener: ProgressListener,
        cancelled: BooleanSupplier
    ) {
        val command = ArrayList<String>()
        command.add(ffmpegPath.toString())
        command.add("-hide_banner")
        command.add("-loglevel")
        command.add("error")
        command.add("-y")
        command.add("-i")
        command.add(inputFile.toString())
        command.add("-b:v")
        command.add(String.format(Locale.US, "%.0fk", videoBitrate))
        command.add("-b:a")
        command.add("${audioBitrate}k")
        command.addAll(getEncodeSettings(qualityPreset))
        command.add("-progress")
        command.add("pipe:1")
        command.add("-nostats")
        command.add(outputFile.toString())

        val process = ProcessBuilder(command).redirectErrorStream(true).start()
        val progressData = HashMap<String, String>()
        var lastMessageLine: String? = null

        BufferedReader(InputStreamReader(process.inputStream, StandardCharsets.UTF_8)).use { reader ->
            while (true) {
                var line = reader.readLine() ?: break
                throwIfCancelled(cancelled, process)

                line = line.trim()
                if (line.isEmpty()) {
                    continue
                }

                val separatorIndex = line.indexOf('=')
                if (separatorIndex <= 0) {
                    lastMessageLine = line
                    continue
                }

                val key = line.substring(0, separatorIndex)
                val value = line.substring(separatorIndex + 1)
                progressData[key] = value

                if (key == "progress" && value == "continue") {
                    val percent = computePercent(progressData, duration)
                    val status = buildProgressStatus(progressData, duration)
                    val title = String.format(
                        Locale.US,
                        "Iteration %d/%d%s",
                        iteration,
                        maxIterations,
                        if (status.isBlank()) "" else " - $status"
                    )
                    progressListener.onUpdate(ProgressUpdate(percent, title))
                }
            }
        }

        val exitCode = process.waitFor()
        if (exitCode != 0) {
            var message = "FFmpeg failed with exit code $exitCode."
            if (!lastMessageLine.isNullOrBlank()) {
                message += " $lastMessageLine"
            }
            throw IllegalStateException(message)
        }

        progressListener.onUpdate(
            ProgressUpdate(
                100,
                String.format(Locale.US, "Iteration %d/%d complete", iteration, maxIterations)
            )
        )
    }

    private fun getEncodeSettings(qualityPreset: Int): List<String> {
        val args = mutableListOf(
            "-c:a", "aac",
            "-profile:v", "main",
            "-movflags", "+faststart",
            "-pix_fmt", "yuv420p"
        )

        when (qualityPreset) {
            1 -> args.addAll(listOf("-c:v", "libx264", "-preset", "medium"))
            2 -> args.addAll(listOf("-c:v", "libx265", "-preset", "medium"))
            3 -> args.addAll(listOf("-c:v", "libx265", "-preset", "slow"))
            4 -> args.addAll(listOf("-c:v", "libx265", "-preset", "veryslow"))
            else -> throw IllegalArgumentException("Unexpected quality preset: $qualityPreset")
        }

        return args
    }

    private fun computePercent(progress: Map<String, String>, duration: Double): Int {
        if (duration <= 0.0) {
            return 0
        }

        val outTimeMicros = progress["out_time_ms"] ?: return 0
        return try {
            val outSeconds = outTimeMicros.toLong() / 1_000_000.0
            ((outSeconds / duration) * 100.0).coerceIn(0.0, 100.0).toInt()
        } catch (_: NumberFormatException) {
            0
        }
    }

    private fun buildProgressStatus(progress: Map<String, String>, duration: Double): String {
        val outTimeMicros = progress["out_time_ms"]
        val speed = progress["speed"]
        val fps = progress["fps"]
        val bitrate = progress["bitrate"]

        var etaSeconds: Double? = null
        val speedMultiplier = parseSpeedMultiplier(speed)
        if (speedMultiplier != null && outTimeMicros != null && speedMultiplier > 0) {
            try {
                val outSeconds = outTimeMicros.toLong() / 1_000_000.0
                val remaining = max(0.0, duration - outSeconds)
                etaSeconds = remaining / speedMultiplier
            } catch (_: NumberFormatException) {
                // Ignore malformed ffmpeg progress values.
            }
        }

        val parts = mutableListOf<String>()
        if (!speed.isNullOrBlank()) {
            parts.add("Speed ${speed.trim()}")
        }
        if (!fps.isNullOrBlank()) {
            parts.add("FPS ${fps.trim()}")
        }
        if (!bitrate.isNullOrBlank()) {
            parts.add("BR ${bitrate.trim()}")
        }
        if (etaSeconds != null) {
            parts.add("ETA ${formatDuration(etaSeconds)}")
        }

        return parts.joinToString(", ")
    }

    private fun parseSpeedMultiplier(speed: String?): Double? {
        if (speed.isNullOrBlank()) {
            return null
        }

        var trimmed = speed.trim()
        if (trimmed.endsWith("x")) {
            trimmed = trimmed.substring(0, trimmed.length - 1)
        }

        return trimmed.toDoubleOrNull()
    }

    private fun calculateTargetBitrate(duration: Double, targetSizeBytes: Long, audioBitrate: Int): Double {
        if (duration <= 0.0) {
            return MIN_VIDEO_BITRATE.toDouble()
        }

        val totalBitrate = (targetSizeBytes * 8.0) / duration / 1_000.0
        val targetVideoBitrate = (totalBitrate - audioBitrate) * CONTAINER_OVERHEAD
        return max(MIN_VIDEO_BITRATE.toDouble(), targetVideoBitrate)
    }

    private fun selectAudioBitrate(duration: Double, targetSizeBytes: Long, defaultAudioBitrate: Int): Int {
        if (duration <= 0.0) {
            return defaultAudioBitrate
        }

        val totalBitrate = (targetSizeBytes * 8.0) / duration / 1_000.0
        val minVideoTotal = MIN_VIDEO_BITRATE / CONTAINER_OVERHEAD
        val maxAudioBitrate = totalBitrate - minVideoTotal
        if (maxAudioBitrate >= defaultAudioBitrate) {
            return defaultAudioBitrate
        }
        if (maxAudioBitrate <= 0.0) {
            return MIN_AUDIO_BITRATE
        }

        val adjusted = maxAudioBitrate.toInt()
        return max(MIN_AUDIO_BITRATE, adjusted)
    }

    private fun estimateNextBitrate(
        currentBitrate: Double,
        currentSize: Long,
        targetSize: Long,
        minBitrate: Double,
        maxBitrate: Double,
        under: Sample?,
        over: Sample?
    ): Double {
        var nextBitrate = if (under != null && over != null) {
            val sizeSpan = over.fileSize - under.fileSize
            if (sizeSpan > 0) {
                under.bitrateKbps +
                    ((targetSize - under.fileSize) * (over.bitrateKbps - under.bitrateKbps) / sizeSpan.toDouble())
            } else {
                (minBitrate + maxBitrate) / 2.0
            }
        } else {
            if (currentSize > 0) {
                currentBitrate * (targetSize.toDouble() / currentSize.toDouble())
            } else {
                (minBitrate + maxBitrate) / 2.0
            }
        }

        if (nextBitrate < minBitrate) {
            nextBitrate = minBitrate
        } else if (nextBitrate > maxBitrate) {
            nextBitrate = maxBitrate
        }

        if (abs(nextBitrate - currentBitrate) < 1.0) {
            val midpoint = (minBitrate + maxBitrate) / 2.0
            if (midpoint != nextBitrate) {
                nextBitrate = midpoint
            }
        }

        return nextBitrate
    }

    private fun throwIfCancelled(cancelled: BooleanSupplier) {
        if (cancelled.asBoolean) {
            throw InterruptedException("Encoding canceled")
        }
    }

    private fun throwIfCancelled(cancelled: BooleanSupplier, process: Process) {
        if (cancelled.asBoolean) {
            process.destroyForcibly()
            throw InterruptedException("Encoding canceled")
        }
    }

    private fun requireBinaryPath(name: String, missingMessage: String): Path {
        return binaryService.getBinaryPath(name) ?: throw IllegalStateException(missingMessage)
    }

    private fun elapsedSecondsSince(startedAtNanos: Long): Double {
        return (System.nanoTime() - startedAtNanos) / 1_000_000_000.0
    }

    private fun formatDuration(totalSeconds: Double): String {
        val seconds = max(0.0, totalSeconds).toInt()
        val hours = seconds / 3600
        val minutes = (seconds % 3600) / 60
        val remainder = seconds % 60

        if (hours > 0) {
            return String.format(Locale.US, "%dh %02dm %02ds", hours, minutes, remainder)
        }
        if (minutes > 0) {
            return String.format(Locale.US, "%dm %02ds", minutes, remainder)
        }

        return "${remainder}s"
    }

    private fun formatBytes(bytes: Long): String {
        if (bytes < 1024) {
            return "${bytes}B"
        }

        var value = bytes.toDouble()
        val units = arrayOf("B", "KB", "MB", "GB", "TB")
        var unitIndex = 0

        while (value >= 1024.0 && unitIndex < units.size - 1) {
            value /= 1024.0
            unitIndex++
        }

        return String.format(Locale.US, "%.2f%s", value, units[unitIndex])
    }

    private data class VideoInfo(val durationSeconds: Double, val videoBitrateKbps: Double?)
    private data class Sample(val bitrateKbps: Double, val fileSize: Long)

    private companion object {
        const val BYTES_PER_MEGABYTE = 1024 * 1024
        const val MIN_VIDEO_BITRATE = 100
        const val MIN_AUDIO_BITRATE = 32
        const val DEFAULT_AUDIO_BITRATE = 128
        const val CONTAINER_OVERHEAD = 0.97
    }
}
