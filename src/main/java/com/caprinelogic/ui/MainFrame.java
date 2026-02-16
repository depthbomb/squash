package com.caprinelogic.ui;

import com.caprinelogic.service.BinaryService;
import com.caprinelogic.service.EncodeService;
import org.springframework.stereotype.Component;

import javax.swing.*;
import javax.swing.filechooser.FileNameExtensionFilter;
import java.awt.*;
import java.awt.datatransfer.DataFlavor;
import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;
import java.beans.PropertyChangeEvent;
import java.io.File;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.util.Locale;
import java.util.Set;
import java.util.concurrent.CancellationException;

@Component
public final class MainFrame extends JFrame {
    private static final String INITIAL_TITLE = "Squash";
    private static final String INITIAL_BUTTON_TEXT = "Squash it!";
    private static final Set<String> SUPPORTED_VIDEO_EXTENSIONS = Set.of(
        "mp4", "mov", "mkv", "avi", "wmv", "webm"
    );

    private final BinaryService binaryService;
    private final EncodeService encodeService;

    //#region UI
    private JProgressBar progressBar;
    private JButton actionButton;
    private JTextField inputFileField;
    private JButton inputFileButton;
    private JTextField outputFileField;
    private JButton outputFileButton;
    private JSpinner targetSizeSpinner;
    private JSpinner toleranceSpinner;
    private JSpinner maxIterationsSpinner;
    private JComboBox<String> qualityPresetCombo;
    //#endregion

    private volatile boolean binariesReady;
    private volatile boolean closeRequested;
    private EncodingWorker encodingWorker;

    public MainFrame(BinaryService binaryService, EncodeService encodeService) {
        this.binaryService = binaryService;
        this.encodeService = encodeService;
    }

    public void start() {
        createAndShowUI();
        new BinaryCheckWorker().execute();
    }

    private void createAndShowUI() {
        progressBar = new JProgressBar(0, 100);
        progressBar.setStringPainted(true);
        progressBar.setString("Idle");
        progressBar.setPreferredSize(new Dimension(progressBar.getPreferredSize().width, 16));
        progressBar.setMinimumSize(new Dimension(0, 16));

        inputFileField = new JTextField();
        inputFileField.setEditable(false);
        inputFileField.setFocusable(false);

        outputFileField = new JTextField();
        outputFileField.setEditable(false);
        outputFileField.setFocusable(false);

        targetSizeSpinner = new JSpinner(new SpinnerNumberModel(10, 1, null, 1));
        targetSizeSpinner.setEditor(new JSpinner.NumberEditor(targetSizeSpinner, "# 'MB'"));

        toleranceSpinner = new JSpinner(new SpinnerNumberModel(2.0, 0.25, 50.0, 0.25));
        toleranceSpinner.setEditor(new JSpinner.NumberEditor(toleranceSpinner, "#0.##'%'"));

        maxIterationsSpinner = new JSpinner(new SpinnerNumberModel(15, 1, null, 1));

        qualityPresetCombo = new JComboBox<>(new String[]{
            "1. Fast, decent quality",
            "2. Slow, better quality (recommended)",
            "3. Very slow, better quality",
            "4. Absurdly slow, better quality"
        });
        qualityPresetCombo.setSelectedIndex(1);

        actionButton = new JButton(INITIAL_BUTTON_TEXT);
        actionButton.setEnabled(false);
        actionButton.addActionListener(_ -> {
            if (isEncodingRunning()) {
                cancelEncodingIfRunning();
            } else {
                startEncoding();
            }
        });

        var filePanel = new JPanel(new GridBagLayout());
        var c = new GridBagConstraints();
        c.insets = new Insets(0, 0, 8, 8);
        c.gridy = 0;
        c.gridx = 0;
        c.anchor = GridBagConstraints.WEST;
        filePanel.add(new JLabel("Input file"), c);

        c.gridx = 1;
        c.weightx = 1.0;
        c.fill = GridBagConstraints.HORIZONTAL;
        filePanel.add(inputFileField, c);

        c.gridx = 2;
        c.weightx = 0;
        c.fill = GridBagConstraints.NONE;
        c.insets = new Insets(0, 0, 8, 0);
        inputFileButton = new JButton("Browse...");
        inputFileButton.addActionListener(_ -> browseForInputFile());
        filePanel.add(inputFileButton, c);

        c.gridy = 1;
        c.gridx = 0;
        c.insets = new Insets(0, 0, 8, 8);
        c.anchor = GridBagConstraints.WEST;
        filePanel.add(new JLabel("Output file"), c);

        c.gridx = 1;
        c.weightx = 1.0;
        c.fill = GridBagConstraints.HORIZONTAL;
        filePanel.add(outputFileField, c);

        c.gridx = 2;
        c.weightx = 0;
        c.fill = GridBagConstraints.NONE;
        c.insets = new Insets(0, 0, 8, 0);
        outputFileButton = new JButton("Browse...");
        outputFileButton.setEnabled(false);
        outputFileButton.addActionListener(_ -> browseForOutputFile());
        filePanel.add(outputFileButton, c);

        c.gridy = 2;
        c.gridx = 0;
        c.insets = new Insets(0, 0, 8, 8);
        c.weightx = 0;
        c.fill = GridBagConstraints.NONE;
        c.anchor = GridBagConstraints.WEST;
        filePanel.add(new JLabel("Target size"), c);

        c.gridx = 1;
        c.weightx = 1.0;
        c.fill = GridBagConstraints.HORIZONTAL;
        filePanel.add(targetSizeSpinner, c);

        c.gridy = 3;
        c.gridx = 0;
        c.insets = new Insets(0, 0, 8, 8);
        c.weightx = 0;
        c.fill = GridBagConstraints.NONE;
        filePanel.add(new JLabel("Tolerance"), c);

        c.gridx = 1;
        c.weightx = 1.0;
        c.fill = GridBagConstraints.HORIZONTAL;
        filePanel.add(toleranceSpinner, c);

        c.gridy = 4;
        c.gridx = 0;
        c.insets = new Insets(0, 0, 8, 8);
        c.weightx = 0;
        c.fill = GridBagConstraints.NONE;
        filePanel.add(new JLabel("Max iterations"), c);

        c.gridx = 1;
        c.weightx = 1.0;
        c.fill = GridBagConstraints.HORIZONTAL;
        filePanel.add(maxIterationsSpinner, c);

        c.gridy = 5;
        c.gridx = 0;
        c.insets = new Insets(0, 0, 0, 8);
        c.weightx = 0;
        c.fill = GridBagConstraints.NONE;
        filePanel.add(new JLabel("Quality preset"), c);

        c.gridx = 1;
        c.weightx = 1.0;
        c.fill = GridBagConstraints.HORIZONTAL;
        filePanel.add(qualityPresetCombo, c);

        var centerPanel = new JPanel(new BorderLayout(0, 8));
        centerPanel.add(filePanel, BorderLayout.NORTH);
        centerPanel.add(progressBar, BorderLayout.SOUTH);

        var root = new JPanel(new BorderLayout(10, 10));
        root.setBorder(BorderFactory.createEmptyBorder(12, 12, 12, 12));
        root.add(centerPanel, BorderLayout.CENTER);
        root.add(actionButton, BorderLayout.SOUTH);
        installFileDropSupport(root);

        setDefaultCloseOperation(WindowConstants.DO_NOTHING_ON_CLOSE);
        setResizable(false);
        setTitle(INITIAL_TITLE);
        setWindowIcon();
        addWindowListener(new WindowAdapter() {
            @Override
            public void windowClosing(WindowEvent event) {
                closeRequested = true;
                if (isEncodingRunning()) {
                    cancelEncodingIfRunning();
                    return;
                }

                shutdownNow();
            }
        });
        setContentPane(root);
        setTransferHandler(root.getTransferHandler());
        pack();
        setSize(600, getSize().height);
        setLocationRelativeTo(null);
        setVisible(true);
    }

    private void browseForInputFile() {
        var chooser = new JFileChooser();
        chooser.setDialogTitle("Select video to squash");
        chooser.setAcceptAllFileFilterUsed(false);
        chooser.setFileFilter(new FileNameExtensionFilter(
            "Video files (*.mp4, *.mov, *.mkv, *.avi, *.wmv, *.webm)",
            "mp4", "mov", "mkv", "avi", "wmv", "webm"
        ));

        if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
            applyInputFile(chooser.getSelectedFile().toPath());
        }
    }

    private void browseForOutputFile() {
        if (inputFileField.getText().isBlank()) {
            return;
        }

        var chooser = new JFileChooser();
        chooser.setDialogTitle("Select output MP4 file");
        chooser.setAcceptAllFileFilterUsed(false);
        chooser.setFileFilter(new FileNameExtensionFilter("MP4 video (*.mp4)", "mp4"));

        if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
            var selected = chooser.getSelectedFile().getAbsolutePath();
            var outputPath = selected.toLowerCase(Locale.ROOT).endsWith(".mp4") ? selected : selected + ".mp4";
            if (outputPath.equalsIgnoreCase(inputFileField.getText())) {
                JOptionPane.showMessageDialog(
                        this,
                        "Output file cannot be the same as input file",
                        "Error",
                        JOptionPane.ERROR_MESSAGE
                );
                return;
            }

            outputFileField.setText(outputPath);
            updateActionButtonState();
        }
    }

    private void setWindowIcon() {
        var iconUrl = getClass().getResource("/icon-32.png");
        if (iconUrl != null) {
            setIconImage(Toolkit.getDefaultToolkit().getImage(iconUrl));
        }
    }

    private void startEncoding() {
        try {
            var inputPath = Path.of(inputFileField.getText()).toAbsolutePath().normalize();
            if (!Files.isRegularFile(inputPath)) {
                JOptionPane.showMessageDialog(this, "Input video file does not exist", "Error", JOptionPane.ERROR_MESSAGE);
                return;
            }

            Path outputPath;
            if (outputFileField.getText().isBlank()) {
                outputPath = defaultOutputFor(inputPath);
                outputFileField.setText(outputPath.toString());
            } else {
                outputPath = Path.of(outputFileField.getText()).toAbsolutePath().normalize();
            }

            if (inputPath.equals(outputPath)) {
                JOptionPane.showMessageDialog(this, "Output file cannot be the same as input file", "Error", JOptionPane.ERROR_MESSAGE);
                return;
            }

            var targetSizeMb = ((Number) targetSizeSpinner.getValue()).intValue();
            var tolerancePercent = ((Number) toleranceSpinner.getValue()).doubleValue();
            var maxIterations = ((Number) maxIterationsSpinner.getValue()).intValue();
            var qualityPreset = qualityPresetCombo.getSelectedIndex() + 1;
            var request = new EncodeService.EncodeRequest(
                inputPath,
                outputPath,
                targetSizeMb,
                tolerancePercent,
                maxIterations,
                qualityPreset
            );

            encodingWorker = new EncodingWorker(request);
            encodingWorker.addPropertyChangeListener(this::handleWorkerPropertyChange);

            setControlsEnabled(false);
            progressBar.setIndeterminate(false);
            progressBar.setValue(0);
            progressBar.setString("0%");
            actionButton.setText("Cancel");
            actionButton.setEnabled(true);
            setFrameTitleWithStatus("Preparing encode...");

            encodingWorker.execute();
        } catch (Exception exception) {
            JOptionPane.showMessageDialog(this, exception.getMessage(), "Error", JOptionPane.ERROR_MESSAGE);
        }
    }

    private void handleWorkerPropertyChange(PropertyChangeEvent event) {
        if (!"progress".equals(event.getPropertyName())) {
            return;
        }

        var value = (Integer) event.getNewValue();
        progressBar.setIndeterminate(false);
        progressBar.setValue(value);
        progressBar.setString(value + "%");
    }

    private boolean isEncodingRunning() {
        return encodingWorker != null && !encodingWorker.isDone();
    }

    private void cancelEncodingIfRunning() {
        if (!isEncodingRunning()) {
            return;
        }

        encodingWorker.cancel(true);
        actionButton.setEnabled(false);
        setFrameTitleWithStatus("Cancelling...");
    }

    private void setControlsEnabled(boolean enabled) {
        inputFileButton.setEnabled(enabled);
        outputFileButton.setEnabled(enabled && !inputFileField.getText().isBlank());
        targetSizeSpinner.setEnabled(enabled);
        toleranceSpinner.setEnabled(enabled);
        maxIterationsSpinner.setEnabled(enabled);
        qualityPresetCombo.setEnabled(enabled);
    }

    private void resetControls() {
        inputFileField.setText("");
        outputFileField.setText("");
        progressBar.setIndeterminate(false);
        progressBar.setValue(0);
    }

    private void updateActionButtonState() {
        if (isEncodingRunning()) {
            actionButton.setEnabled(true);
            return;
        }

        boolean hasInput = !inputFileField.getText().isBlank();
        boolean hasOutput = !outputFileField.getText().isBlank();
        actionButton.setEnabled(binariesReady && hasInput && hasOutput);
    }

    private void setFrameTitleWithStatus(String status) {
        if (status == null || status.isBlank()) {
            setTitle(INITIAL_TITLE);
        } else {
            setTitle(status);
        }
    }

    private Path defaultOutputFor(Path inputFile) {
        var fileName = inputFile.getFileName().toString();
        var dotIndex = fileName.lastIndexOf('.');
        var stem = dotIndex > 0 ? fileName.substring(0, dotIndex) : fileName;
        var generated = String.format("%s-squashed-%s.mp4", stem, Instant.now().toEpochMilli());

        return inputFile.getParent().resolve(generated);
    }

    private void applyInputFile(Path inputPath) {
        var normalized = inputPath.toAbsolutePath().normalize();
        inputFileField.setText(normalized.toString());
        outputFileField.setText(defaultOutputFor(normalized).toString());
        outputFileButton.setEnabled(true);
        updateActionButtonState();
    }

    private boolean isSupportedVideoFile(Path path) {
        if (!Files.isRegularFile(path)) {
            return false;
        }

        var fileName = path.getFileName();
        if (fileName == null) {
            return false;
        }

        var name = fileName.toString();
        var dotIndex = name.lastIndexOf('.');
        if (dotIndex < 0 || dotIndex == name.length() - 1) {
            return false;
        }

        var extension = name.substring(dotIndex + 1).toLowerCase(Locale.ROOT);
        return SUPPORTED_VIDEO_EXTENSIONS.contains(extension);
    }

    private void installFileDropSupport(JComponent target) {
        target.setTransferHandler(new TransferHandler() {
            @Override
            public boolean canImport(TransferSupport support) {
                return support.isDrop() && support.isDataFlavorSupported(DataFlavor.javaFileListFlavor);
            }

            @Override
            public boolean importData(TransferSupport support) {
                if (!canImport(support)) {
                    return false;
                }

                if (isEncodingRunning()) {
                    return false;
                }

                try {
                    var transferable = support.getTransferable();
                    @SuppressWarnings("unchecked")
                    var files = (java.util.List<File>) transferable.getTransferData(DataFlavor.javaFileListFlavor);
                    if (files.isEmpty()) {
                        return false;
                    }

                    var droppedPath = files.getFirst().toPath();
                    if (!isSupportedVideoFile(droppedPath)) {
                        JOptionPane.showMessageDialog(
                            MainFrame.this,
                            "Dropped item is not a supported video file.",
                            "Unsupported file",
                            JOptionPane.WARNING_MESSAGE
                        );
                        return false;
                    }

                    applyInputFile(droppedPath);
                    return true;
                } catch (Exception exception) {
                    JOptionPane.showMessageDialog(
                        MainFrame.this,
                        exception.getMessage(),
                        "Drop failed",
                        JOptionPane.ERROR_MESSAGE
                    );
                    return false;
                }
            }
        });
    }

    private final class EncodingWorker extends SwingWorker<EncodeService.EncodeResult, Void> {
        private static final long TITLE_UPDATE_MIN_INTERVAL_NANOS = 250_000_000L;
        private final EncodeService.EncodeRequest request;
        private long lastTitleUpdateNanos;
        private String lastTitleText = "";

        private EncodingWorker(EncodeService.EncodeRequest request) {
            this.request = request;
        }

        @Override
        protected EncodeService.EncodeResult doInBackground() throws Exception {
            return encodeService.resizeVideoToTarget(
                request,
                update -> {
                    if (update.progressPercent() >= 0) {
                        setProgress(update.progressPercent());
                    }

                    updateFrameTitle(update.titleText(), update.progressPercent() >= 100);
                },
                this::isCancelled
            );
        }

        private void updateFrameTitle(String titleText, boolean force) {
            if (titleText == null || titleText.isBlank()) {
                return;
            }

            long now = System.nanoTime();
            if (!force) {
                if (titleText.equals(lastTitleText)) {
                    return;
                }
                if (now - lastTitleUpdateNanos < TITLE_UPDATE_MIN_INTERVAL_NANOS) {
                    return;
                }
            }

            lastTitleText = titleText;
            lastTitleUpdateNanos = now;
            SwingUtilities.invokeLater(() -> {
                if (encodingWorker == this) {
                    setFrameTitleWithStatus(titleText);
                }
            });
        }

        @Override
        protected void done() {
            setControlsEnabled(true);
            actionButton.setText(INITIAL_BUTTON_TEXT);
            encodingWorker = null;

            try {
                var result = get();

                progressBar.setValue(100);
                progressBar.setString("100%");

                var outcome = result.success() ? "Success" : "Partial";
                long delta = result.fileSizeBytes() - result.targetSizeBytes();
                var deltaSign = delta > 0 ? "+" : "";

                var message = String.format(
                    Locale.US,
                    "%s: wrote %s\nFinal size %s (target %s, delta %s%s)\nIterations %d/%d at %.0f kbps\nTotal time %s",
                    outcome,
                    result.filePath(),
                    formatBytes(result.fileSizeBytes()),
                    formatBytes(result.targetSizeBytes()),
                    deltaSign,
                    formatBytes(Math.abs(delta)),
                    result.iteration(),
                    request.maxIterations(),
                    result.videoBitrateKbps(),
                    formatDuration(result.elapsedSeconds())
                );

                if (!closeRequested) {
                    JOptionPane.showMessageDialog(MainFrame.this, message, outcome, JOptionPane.INFORMATION_MESSAGE);
                }
            } catch (CancellationException cancellationException) {
                progressBar.setValue(0);
                progressBar.setString("Canceled");
            } catch (Exception exception) {
                progressBar.setValue(0);
                progressBar.setString("Failed");
                if (!closeRequested) {
                    String message = exception.getCause() != null ? exception.getCause().getMessage() : exception.getMessage();
                    JOptionPane.showMessageDialog(MainFrame.this, message, "Error", JOptionPane.ERROR_MESSAGE);
                }
            }

            if (closeRequested) {
                shutdownNow();
                return;
            }

            setFrameTitleWithStatus(null);
            resetControls();
            updateActionButtonState();
        }
    }

    private final class BinaryCheckWorker extends SwingWorker<Boolean, Void> {
        @Override
        protected Boolean doInBackground() {
            return binaryService.hasBinary("ffmpeg") && binaryService.hasBinary("ffprobe");
        }

        @Override
        protected void done() {
            try {
                binariesReady = get();
                if (!binariesReady) {
                    JOptionPane.showMessageDialog(
                        MainFrame.this,
                        "FFmpeg and FFprobe are required and were not found in the app directory or PATH.",
                        "Missing binaries",
                        JOptionPane.ERROR_MESSAGE
                    );
                }
            } catch (Exception exception) {
                binariesReady = false;
                JOptionPane.showMessageDialog(MainFrame.this, exception.getMessage(), "Error", JOptionPane.ERROR_MESSAGE);
            }

            updateActionButtonState();
        }
    }

    private static String formatBytes(long bytes) {
        if (bytes < 1024) {
            return bytes + " B";
        }

        double value = bytes;
        String[] units = {"B", "KB", "MB", "GB", "TB"};
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.length - 1) {
            value /= 1024;
            unitIndex++;
        }

        return String.format(Locale.US, "%.2f %s", value, units[unitIndex]);
    }

    private static String formatDuration(double totalSeconds) {
        int seconds = (int) Math.max(0, totalSeconds);
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int remainder = seconds % 60;

        if (hours > 0) {
            return String.format(Locale.US, "%dh %02dm %02ds", hours, minutes, remainder);
        }
        if (minutes > 0) {
            return String.format(Locale.US, "%dm %02ds", minutes, remainder);
        }

        return remainder + "s";
    }

    private void shutdownNow() {
        dispose();
        System.exit(0);
    }
}
