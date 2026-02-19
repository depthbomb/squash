package com.caprinelogic.ui

import com.caprinelogic.service.ExtractService
import org.springframework.stereotype.Component
import java.awt.BorderLayout
import java.awt.Dimension
import java.awt.FlowLayout
import java.awt.event.KeyEvent
import java.awt.event.WindowAdapter
import java.awt.event.WindowEvent
import java.io.IOException
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.nio.file.Files
import java.nio.file.InvalidPathException
import java.nio.file.Path
import java.nio.file.StandardOpenOption
import java.util.Locale
import java.util.Optional
import java.util.concurrent.CancellationException
import javax.swing.BorderFactory
import javax.swing.JButton
import javax.swing.JComponent
import javax.swing.JDialog
import javax.swing.JLabel
import javax.swing.JOptionPane
import javax.swing.JPanel
import javax.swing.JProgressBar
import javax.swing.KeyStroke
import javax.swing.SwingWorker

@Component
class BinaryDownloaderDialog(private val extractService: ExtractService) : JDialog() {
    private val buttonCancel: JButton
    private val progressBar: JProgressBar
    private val statusLabel: JLabel
    private var worker: DownloadAndExtractWorker? = null

    init {
        val contentPane = JPanel(BorderLayout(0, 10))
        contentPane.border = BorderFactory.createEmptyBorder(10, 10, 10, 10)

        statusLabel = JLabel("Starting download...")
        progressBar = JProgressBar().apply {
            isIndeterminate = true
            isStringPainted = true
        }

        val progressPanel = JPanel(BorderLayout(0, 6))
        progressPanel.add(statusLabel, BorderLayout.NORTH)
        progressPanel.add(progressBar, BorderLayout.CENTER)

        buttonCancel = JButton("Cancel")
        val controlsPanel = JPanel(FlowLayout(FlowLayout.RIGHT, 0, 0))
        controlsPanel.add(buttonCancel)

        contentPane.add(progressPanel, BorderLayout.CENTER)
        contentPane.add(controlsPanel, BorderLayout.SOUTH)

        title = "Download FFmpeg"
        setContentPane(contentPane)
        isModal = true
        isResizable = false

        buttonCancel.addActionListener { onCancel() }

        defaultCloseOperation = DO_NOTHING_ON_CLOSE
        addWindowListener(object : WindowAdapter() {
            override fun windowClosing(e: WindowEvent) {
                onCancel()
            }
        })

        contentPane.registerKeyboardAction(
            { onCancel() },
            KeyStroke.getKeyStroke(KeyEvent.VK_ESCAPE, 0),
            JComponent.WHEN_ANCESTOR_OF_FOCUSED_COMPONENT
        )

        progressBar.minimum = 0
        progressBar.maximum = 100
        progressBar.value = 0
        progressBar.string = "0%"

        pack()
        val size = size
        minimumSize = Dimension(360, size.height)
        setLocationRelativeTo(owner)
    }

    override fun setVisible(visible: Boolean) {
        if (visible) {
            startDownload()
        }

        super.setVisible(visible)
    }

    private fun startDownload() {
        val activeWorker = worker
        if (activeWorker != null && !activeWorker.isDone) {
            return
        }

        statusLabel.text = "Starting download..."
        buttonCancel.isEnabled = true
        buttonCancel.text = "Cancel"

        worker = DownloadAndExtractWorker().also { it.execute() }
    }

    private fun onCancel() {
        val activeWorker = worker
        if (activeWorker != null && !activeWorker.isDone) {
            setCancellingState()
            activeWorker.cancel(true)
            return
        }

        dispose()
    }

    private fun resolveApplicationExecutableDirectory(): Path {
        val jpackageAppPath = System.getProperty("jpackage.app-path")
        if (!jpackageAppPath.isNullOrBlank()) {
            try {
                val appPath = Path.of(jpackageAppPath).toAbsolutePath().normalize()
                val parent = appPath.parent
                if (parent != null) {
                    return parent
                }
            } catch (_: InvalidPathException) {
                // Ignore and try fallback.
            }
        }

        return ProcessHandle.current()
            .info()
            .command()
            .flatMap { resolveExecutableParentFromCommand(it) }
            .orElseGet { Path.of(System.getProperty("user.dir", ".")).toAbsolutePath().normalize() }
    }

    private fun resolveExecutableParentFromCommand(commandPath: String): Optional<Path> {
        return try {
            val command = Path.of(commandPath).toAbsolutePath().normalize()
            val fileName = command.fileName
            if (fileName == null || isJavaLauncher(fileName.toString())) {
                Optional.empty()
            } else {
                Optional.ofNullable(command.parent)
            }
        } catch (_: InvalidPathException) {
            Optional.empty()
        }
    }

    private fun isJavaLauncher(fileName: String): Boolean {
        return when (fileName.lowercase(Locale.ROOT)) {
            "java", "java.exe", "javaw", "javaw.exe" -> true
            else -> false
        }
    }

    private fun setCancellingState() {
        statusLabel.text = "Cancelling..."
        buttonCancel.isEnabled = false
    }

    private fun setCancelledState() {
        progressBar.isIndeterminate = false
        progressBar.value = 0
        progressBar.string = "Canceled"
        statusLabel.text = "Download cancelled."
    }

    private inner class DownloadAndExtractWorker : SwingWorker<Boolean, UiUpdate>() {
        override fun doInBackground(): Boolean {
            var archivePath: Path? = null
            try {
                val outputDirectory = resolveApplicationExecutableDirectory()
                Files.createDirectories(outputDirectory)
                archivePath = Files.createTempFile("ffmpeg-git-essentials-", ".7z")

                downloadArchive(archivePath)
                if (isCancelled) {
                    return false
                }

                publish(UiUpdate("Extracting ffmpeg.exe and ffprobe.exe...", null, true))
                extractService.extractFilesFromArchive(archivePath, FILES_TO_EXTRACT, outputDirectory)
                verifyExtractedBinaries(outputDirectory)

                publish(UiUpdate("FFmpeg download and extraction complete.", 100, false))
                return true
            } finally {
                if (archivePath != null) {
                    Files.deleteIfExists(archivePath)
                }
            }
        }

        override fun process(updates: MutableList<UiUpdate>) {
            if (updates.isEmpty()) {
                return
            }

            val latest = updates.last()
            if (latest.status != null) {
                statusLabel.text = latest.status
            }

            if (latest.indeterminate != null) {
                progressBar.isIndeterminate = latest.indeterminate
                if (latest.indeterminate && latest.progressPercent == null) {
                    progressBar.string = ""
                }
            }

            if (latest.progressPercent != null) {
                val bounded = latest.progressPercent.coerceIn(0, 100)
                progressBar.value = bounded
                progressBar.string = "$bounded%"
            }
        }

        override fun done() {
            buttonCancel.isEnabled = true
            buttonCancel.text = "Close"

            try {
                if (isCancelled) {
                    setCancelledState()
                    return
                }

                if (get() == true) {
                    dispose()
                }
            } catch (_: CancellationException) {
                setCancelledState()
            } catch (exception: Exception) {
                progressBar.isIndeterminate = false
                progressBar.value = 0
                progressBar.string = "Failed"

                val cause = exception.cause ?: exception
                val message = cause.message ?: "Failed to download FFmpeg binaries."
                statusLabel.text = "Download failed."
                JOptionPane.showMessageDialog(
                    this@BinaryDownloaderDialog,
                    message,
                    "Download failed",
                    JOptionPane.ERROR_MESSAGE
                )
            }
        }

        private fun downloadArchive(outputPath: Path) {
            publish(UiUpdate("Connecting to FFmpeg download...", null, true))

            val request = HttpRequest.newBuilder(FFMPEG_ARCHIVE_URI)
                .GET()
                .build()
            val response = HTTP_CLIENT.send(request, HttpResponse.BodyHandlers.ofInputStream())
            if (response.statusCode() / 100 != 2) {
                throw IOException("FFmpeg download failed with HTTP status ${response.statusCode()}.")
            }

            val expectedBytes = response.headers().firstValueAsLong("content-length").orElse(-1L)
            if (expectedBytes > 0) {
                publish(UiUpdate("Downloading FFmpeg binaries...", 0, false))
            } else {
                publish(UiUpdate("Downloading FFmpeg binaries...", null, true))
            }

            var downloadedBytes = 0L
            var lastReportedBytes = 0L
            var lastProgress = -1

            response.body().use { inputStream ->
                Files.newOutputStream(
                    outputPath,
                    StandardOpenOption.CREATE,
                    StandardOpenOption.TRUNCATE_EXISTING,
                    StandardOpenOption.WRITE
                ).use { outputStream ->
                    val buffer = ByteArray(DOWNLOAD_BUFFER_SIZE)
                    while (true) {
                        val read = inputStream.read(buffer)
                        if (read < 0) {
                            break
                        }
                        if (isCancelled) {
                            throw InterruptedException("Download cancelled.")
                        }

                        outputStream.write(buffer, 0, read)
                        downloadedBytes += read

                        if (expectedBytes > 0) {
                            val progress = ((downloadedBytes * 100L) / expectedBytes).coerceAtMost(100).toInt()
                            if (progress != lastProgress) {
                                lastProgress = progress
                                publish(UiUpdate("Downloading FFmpeg binaries...", progress, false))
                            }
                        } else if (downloadedBytes - lastReportedBytes >= 5L * 1024 * 1024) {
                            lastReportedBytes = downloadedBytes
                            publish(UiUpdate("Downloading FFmpeg binaries...", null, true))
                        }
                    }
                }
            }

            if (expectedBytes > 0 && downloadedBytes < expectedBytes) {
                throw IOException("FFmpeg download was interrupted before completion.")
            }
            if (expectedBytes <= 0) {
                publish(UiUpdate("Download complete.", 100, false))
            }
        }

        private fun verifyExtractedBinaries(outputDirectory: Path) {
            for (fileName in FILES_TO_EXTRACT) {
                val filePath = outputDirectory.resolve(fileName)
                if (!Files.isRegularFile(filePath)) {
                    throw IOException("Expected extracted file not found: $filePath")
                }
            }
        }
    }

    private data class UiUpdate(
        val status: String?,
        val progressPercent: Int?,
        val indeterminate: Boolean?
    )

    private companion object {
        val FFMPEG_ARCHIVE_URI: URI = URI.create("https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z")
        val FILES_TO_EXTRACT = arrayOf("ffmpeg.exe", "ffprobe.exe")
        const val DOWNLOAD_BUFFER_SIZE = 64 * 1024
        val HTTP_CLIENT: HttpClient = HttpClient.newBuilder()
            .followRedirects(HttpClient.Redirect.NORMAL)
            .build()
    }
}
