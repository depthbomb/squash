package com.caprinelogic.ui;

import com.caprinelogic.service.ExtractService;
import org.springframework.stereotype.Component;

import javax.swing.*;
import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.FlowLayout;
import java.awt.event.KeyEvent;
import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;
import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.file.Files;
import java.nio.file.InvalidPathException;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.util.List;
import java.util.Locale;
import java.util.Optional;
import java.util.concurrent.CancellationException;

@Component
public class BinaryDownloaderDialog extends JDialog {
    private static final URI FFMPEG_ARCHIVE_URI = URI.create("https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z");
    private static final String[] FILES_TO_EXTRACT = {"ffmpeg.exe", "ffprobe.exe"};
    private static final int DOWNLOAD_BUFFER_SIZE = 64 * 1024;
    private static final HttpClient HTTP_CLIENT = HttpClient.newBuilder()
            .followRedirects(HttpClient.Redirect.NORMAL)
            .build();

    private final JButton buttonCancel;
    private final JProgressBar progressBar;
    private final JLabel statusLabel;

    private final ExtractService extractService;
    private DownloadAndExtractWorker worker;

    public BinaryDownloaderDialog(ExtractService extractService) {
        this.extractService = extractService;

        var contentPane = new JPanel(new BorderLayout(0, 10));
        contentPane.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 10));

        statusLabel = new JLabel("Starting download...");
        progressBar = new JProgressBar();
        progressBar.setIndeterminate(true);
        progressBar.setStringPainted(true);

        var progressPanel = new JPanel(new BorderLayout(0, 6));
        progressPanel.add(statusLabel, BorderLayout.NORTH);
        progressPanel.add(progressBar, BorderLayout.CENTER);

        buttonCancel = new JButton("Cancel");
        var controlsPanel = new JPanel(new FlowLayout(FlowLayout.RIGHT, 0, 0));
        controlsPanel.add(buttonCancel);

        contentPane.add(progressPanel, BorderLayout.CENTER);
        contentPane.add(controlsPanel, BorderLayout.SOUTH);

        setTitle("Download FFmpeg");
        setContentPane(contentPane);
        setModal(true);
        setResizable(false);

        buttonCancel.addActionListener(_ -> onCancel());

        setDefaultCloseOperation(DO_NOTHING_ON_CLOSE);
        addWindowListener(new WindowAdapter() {
            @Override
            public void windowClosing(WindowEvent e) {
                onCancel();
            }
        });

        contentPane.registerKeyboardAction(
                _ -> onCancel(),
                KeyStroke.getKeyStroke(KeyEvent.VK_ESCAPE, 0),
                JComponent.WHEN_ANCESTOR_OF_FOCUSED_COMPONENT
        );

        progressBar.setMinimum(0);
        progressBar.setMaximum(100);
        progressBar.setValue(0);
        progressBar.setString("0%");

        pack();
        var size = getSize();
        setMinimumSize(new Dimension(360, size.height));
        setLocationRelativeTo(getOwner());
    }

    @Override
    public void setVisible(boolean visible) {
        if (visible) {
            startDownload();
        }

        super.setVisible(visible);
    }

    private void startDownload() {
        if (worker != null && !worker.isDone()) {
            return;
        }

        statusLabel.setText("Starting download...");
        buttonCancel.setEnabled(true);
        buttonCancel.setText("Cancel");

        worker = new DownloadAndExtractWorker();
        worker.execute();
    }

    private void onCancel() {
        if (worker != null && !worker.isDone()) {
            setCancellingState();
            worker.cancel(true);
            return;
        }

        dispose();
    }

    private Path resolveApplicationExecutableDirectory() {
        var jpackageAppPath = System.getProperty("jpackage.app-path");
        if (jpackageAppPath != null && !jpackageAppPath.isBlank()) {
            try {
                var appPath = Path.of(jpackageAppPath).toAbsolutePath().normalize();
                var parent = appPath.getParent();
                if (parent != null) {
                    return parent;
                }
            } catch (InvalidPathException ignored) {}
        }

        return ProcessHandle.current()
                .info()
                .command()
                .flatMap(this::resolveExecutableParentFromCommand)
                .orElseGet(() -> Path.of(System.getProperty("user.dir", ".")).toAbsolutePath().normalize());
    }

    private Optional<Path> resolveExecutableParentFromCommand(String commandPath) {
        try {
            var command = Path.of(commandPath).toAbsolutePath().normalize();
            var fileName = command.getFileName();
            if (fileName == null || isJavaLauncher(fileName.toString())) {
                return Optional.empty();
            }

            return Optional.ofNullable(command.getParent());
        } catch (InvalidPathException ignored) {
            return Optional.empty();
        }
    }

    private boolean isJavaLauncher(String fileName) {
        var normalized = fileName.toLowerCase(Locale.ROOT);
        return normalized.equals("java")
                || normalized.equals("java.exe")
                || normalized.equals("javaw")
                || normalized.equals("javaw.exe");
    }

    private static String formatMegabytes(long bytes) {
        return String.format(Locale.US, "%.1f MB", bytes / (1024d * 1024d));
    }

    private record UiUpdate(String status, Integer progressPercent, Boolean indeterminate) {}

    private final class DownloadAndExtractWorker extends SwingWorker<Boolean, UiUpdate> {
        @Override
        protected Boolean doInBackground() throws Exception {
            Path archivePath = null;
            try {
                var outputDirectory = resolveApplicationExecutableDirectory();
                Files.createDirectories(outputDirectory);
                archivePath = Files.createTempFile("ffmpeg-git-essentials-", ".7z");

                downloadArchive(archivePath);
                if (isCancelled()) {
                    return false;
                }

                publish(new UiUpdate("Extracting ffmpeg.exe and ffprobe.exe...", null, true));
                extractService.extractFilesFromArchive(archivePath, FILES_TO_EXTRACT, outputDirectory);
                verifyExtractedBinaries(outputDirectory);

                publish(new UiUpdate("FFmpeg download and extraction complete.", 100, false));
                return true;
            } finally {
                if (archivePath != null) {
                    Files.deleteIfExists(archivePath);
                }
            }
        }

        @Override
        protected void process(List<UiUpdate> updates) {
            if (updates.isEmpty()) {
                return;
            }

            var latest = updates.getLast();
            if (latest.status() != null) {
                statusLabel.setText(latest.status());
            }

            if (latest.indeterminate() != null) {
                progressBar.setIndeterminate(latest.indeterminate());
                if (latest.indeterminate() && latest.progressPercent() == null) {
                    progressBar.setString("");
                }
            }

            if (latest.progressPercent() != null) {
                var bounded = Math.max(0, Math.min(100, latest.progressPercent()));
                progressBar.setValue(bounded);
                progressBar.setString(bounded + "%");
            }
        }

        @Override
        protected void done() {
            buttonCancel.setEnabled(true);
            buttonCancel.setText("Close");

            try {
                if (isCancelled()) {
                    setCancelledState();
                    return;
                }

                if (Boolean.TRUE.equals(get())) {
                    dispose();
                }
            } catch (CancellationException ignored) {
                setCancelledState();
            } catch (Exception exception) {
                progressBar.setIndeterminate(false);
                progressBar.setValue(0);
                progressBar.setString("Failed");

                var cause = exception.getCause() != null ? exception.getCause() : exception;
                var message = cause.getMessage() != null ? cause.getMessage() : "Failed to download FFmpeg binaries.";
                statusLabel.setText("Download failed.");
                JOptionPane.showMessageDialog(
                        BinaryDownloaderDialog.this,
                        message,
                        "Download failed",
                        JOptionPane.ERROR_MESSAGE
                );
            }
        }

        private void downloadArchive(Path outputPath) throws IOException, InterruptedException {
            publish(new UiUpdate("Connecting to FFmpeg download...", null, true));

            var request = HttpRequest.newBuilder(FFMPEG_ARCHIVE_URI)
                    .GET()
                    .build();
            var response = HTTP_CLIENT.send(request, HttpResponse.BodyHandlers.ofInputStream());
            if (response.statusCode() / 100 != 2) {
                throw new IOException("FFmpeg download failed with HTTP status " + response.statusCode() + ".");
            }

            var expectedBytes = response.headers().firstValueAsLong("content-length").orElse(-1L);
            if (expectedBytes > 0) {
                publish(new UiUpdate("Downloading FFmpeg binaries...", 0, false));
            } else {
                publish(new UiUpdate("Downloading FFmpeg binaries...", null, true));
            }

            long downloadedBytes = 0;
            long lastReportedBytes = 0;
            int lastProgress = -1;

            try (var inputStream = response.body();
                 var outputStream = Files.newOutputStream(
                         outputPath,
                         StandardOpenOption.CREATE,
                         StandardOpenOption.TRUNCATE_EXISTING,
                         StandardOpenOption.WRITE
                 )) {
                var buffer = new byte[DOWNLOAD_BUFFER_SIZE];
                int read;
                while ((read = inputStream.read(buffer)) >= 0) {
                    if (isCancelled()) {
                        throw new InterruptedException("Download cancelled.");
                    }

                    outputStream.write(buffer, 0, read);
                    downloadedBytes += read;

                    if (expectedBytes > 0) {
                        int progress = (int) Math.min(100, (downloadedBytes * 100L) / expectedBytes);
                        if (progress != lastProgress) {
                            lastProgress = progress;
                            publish(new UiUpdate("Downloading FFmpeg binaries...", progress, false));
                        }
                    } else if (downloadedBytes - lastReportedBytes >= 5L * 1024 * 1024) {
                        lastReportedBytes = downloadedBytes;
                        publish(new UiUpdate(
                                "Downloading FFmpeg binaries...",
                                null,
                                true
                        ));
                    }
                }
            }

            if (expectedBytes > 0 && downloadedBytes < expectedBytes) {
                throw new IOException("FFmpeg download was interrupted before completion.");
            }
            if (expectedBytes <= 0) {
                publish(new UiUpdate("Download complete.", 100, false));
            }
        }

        private void verifyExtractedBinaries(Path outputDirectory) throws IOException {
            for (String fileName : FILES_TO_EXTRACT) {
                var filePath = outputDirectory.resolve(fileName);
                if (!Files.isRegularFile(filePath)) {
                    throw new IOException("Expected extracted file not found: " + filePath);
                }
            }
        }
    }

    private void setCancellingState() {
        statusLabel.setText("Cancelling...");
        buttonCancel.setEnabled(false);
    }

    private void setCancelledState() {
        progressBar.setIndeterminate(false);
        progressBar.setValue(0);
        progressBar.setString("Canceled");
        statusLabel.setText("Download cancelled.");
    }
}
