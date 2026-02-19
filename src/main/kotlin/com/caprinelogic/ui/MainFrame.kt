package com.caprinelogic.ui

import com.caprinelogic.BuildInfo
import com.caprinelogic.service.BinaryService
import com.caprinelogic.service.EncodeService
import org.springframework.stereotype.Component
import java.awt.BorderLayout
import java.awt.Container
import java.awt.Dimension
import java.awt.GridBagConstraints
import java.awt.GridBagLayout
import java.awt.Insets
import java.awt.Toolkit
import java.awt.datatransfer.DataFlavor.javaFileListFlavor
import java.awt.event.WindowAdapter
import java.awt.event.WindowEvent
import java.beans.PropertyChangeEvent
import java.io.File
import java.nio.file.Files
import java.nio.file.Path
import java.time.Instant
import java.util.Locale
import java.util.concurrent.CancellationException
import java.util.function.BooleanSupplier
import javax.swing.BorderFactory
import javax.swing.JButton
import javax.swing.JComboBox
import javax.swing.JComponent
import javax.swing.JDialog
import javax.swing.JFileChooser
import javax.swing.JFrame
import javax.swing.JLabel
import javax.swing.JMenu
import javax.swing.JMenuBar
import javax.swing.JMenuItem
import javax.swing.JOptionPane
import javax.swing.JPanel
import javax.swing.JProgressBar
import javax.swing.JSpinner
import javax.swing.JTextField
import javax.swing.SpinnerNumberModel
import javax.swing.SwingUtilities
import javax.swing.SwingWorker
import javax.swing.TransferHandler
import javax.swing.WindowConstants
import javax.swing.filechooser.FileNameExtensionFilter
import kotlin.math.abs

@Component
class MainFrame(
    private val binaryService: BinaryService,
    private val encodeService: EncodeService,
    private val binaryDownloaderDialog: BinaryDownloaderDialog
) : JFrame() {
    private lateinit var progressBar: JProgressBar
    private lateinit var actionButton: JButton
    private lateinit var inputFileField: JTextField
    private lateinit var inputFileButton: JButton
    private lateinit var outputFileField: JTextField
    private lateinit var outputFileButton: JButton
    private lateinit var targetSizeSpinner: JSpinner
    private lateinit var toleranceSpinner: JSpinner
    private lateinit var maxIterationsSpinner: JSpinner
    private lateinit var qualityPresetCombo: JComboBox<String>

    private var infoDialog: JDialog? = null

    @Volatile
    private var binariesReady = false

    @Volatile
    private var closeRequested = false

    private var encodingWorker: EncodingWorker? = null

    fun start() {
        createAndShowUI()
        BinaryCheckWorker().execute()
    }

    private fun createAndShowUI() {
        progressBar = JProgressBar(0, 100).apply {
            isStringPainted = true
            string = "Idle"
            preferredSize = Dimension(preferredSize.width, 16)
            minimumSize = Dimension(0, 16)
        }

        inputFileField = JTextField().apply {
            isEditable = false
            isFocusable = false
        }

        outputFileField = JTextField().apply {
            isEditable = false
            isFocusable = false
        }

        targetSizeSpinner = JSpinner(SpinnerNumberModel(10, 1, null, 1)).apply {
            editor = JSpinner.NumberEditor(this, "# 'MB'")
        }

        toleranceSpinner = JSpinner(SpinnerNumberModel(2.0, 0.25, 50.0, 0.25)).apply {
            editor = JSpinner.NumberEditor(this, "#0.##'%'")
        }

        maxIterationsSpinner = JSpinner(SpinnerNumberModel(15, 1, null, 1))

        qualityPresetCombo = JComboBox(
            arrayOf(
                "1. Fast, decent quality",
                "2. Slow, better quality (recommended)",
                "3. Very slow, better quality",
                "4. Absurdly slow, better quality"
            )
        ).apply {
            selectedIndex = 1
        }

        inputFileButton = JButton("Browse...").apply {
            addActionListener { browseForInputFile() }
        }

        outputFileButton = JButton("Browse...").apply {
            isEnabled = false
            addActionListener { browseForOutputFile() }
        }

        actionButton = JButton(INITIAL_BUTTON_TEXT).apply {
            isEnabled = false
            addActionListener {
                if (isEncodingRunning()) {
                    cancelEncodingIfRunning()
                } else {
                    startEncoding()
                }
            }
        }

        val filePanel = JPanel(GridBagLayout())
        addRowWithTrailingControl(filePanel, 0, "Input file", inputFileField, inputFileButton, false)
        addRowWithTrailingControl(filePanel, 1, "Output file", outputFileField, outputFileButton, false)
        addRow(filePanel, 2, "Target size", targetSizeSpinner, false)
        addRow(filePanel, 3, "Tolerance", toleranceSpinner, false)
        addRow(filePanel, 4, "Max iterations", maxIterationsSpinner, false)
        addRow(filePanel, 5, "Quality preset", qualityPresetCombo, true)

        val centerPanel = JPanel(BorderLayout(0, 8))
        centerPanel.add(filePanel, BorderLayout.NORTH)
        centerPanel.add(progressBar, BorderLayout.SOUTH)

        val root = JPanel(BorderLayout(10, 10))
        root.border = BorderFactory.createEmptyBorder(12, 12, 12, 12)
        root.add(centerPanel, BorderLayout.CENTER)
        root.add(actionButton, BorderLayout.SOUTH)

        installFileDropSupport(root)

        defaultCloseOperation = WindowConstants.DO_NOTHING_ON_CLOSE
        isResizable = false
        title = INITIAL_TITLE
        setWindowIcon()
        jMenuBar = createMenuBar()
        addWindowListener(object : WindowAdapter() {
            override fun windowClosing(event: WindowEvent) {
                closeRequested = true
                if (isEncodingRunning()) {
                    cancelEncodingIfRunning()
                    return
                }

                shutdownNow()
            }
        })

        contentPane = root
        transferHandler = root.transferHandler
        pack()
        setSize(600, size.height)
        setLocationRelativeTo(null)
        isVisible = true
    }

    private fun createMenuBar(): JMenuBar {
        val menuBar = JMenuBar()
        val aboutMenu = JMenu("About")
        val infoItem = JMenuItem("Info")
        infoItem.addActionListener { showInfoDialog() }
        aboutMenu.add(infoItem)
        menuBar.add(aboutMenu)
        return menuBar
    }

    private fun showInfoDialog() {
        if (infoDialog == null) {
            infoDialog = AboutDialog(this)
        }

        infoDialog!!.setLocationRelativeTo(this)
        infoDialog!!.isVisible = true
    }

    private fun browseForInputFile() {
        val chooser = JFileChooser().apply {
            dialogTitle = "Select video to squash"
            isAcceptAllFileFilterUsed = false
            fileFilter = FileNameExtensionFilter(
                "Video files (*.mp4, *.mov, *.mkv, *.avi, *.wmv, *.webm)",
                "mp4", "mov", "mkv", "avi", "wmv", "webm"
            )
        }

        if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
            applyInputFile(chooser.selectedFile.toPath())
        }
    }

    private fun browseForOutputFile() {
        if (inputFileField.text.isBlank()) {
            return
        }

        val chooser = JFileChooser().apply {
            dialogTitle = "Select output MP4 file"
            isAcceptAllFileFilterUsed = false
            fileFilter = FileNameExtensionFilter("MP4 video (*.mp4)", "mp4")
        }

        if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
            val selected = chooser.selectedFile.absolutePath
            val outputPath = if (selected.lowercase(Locale.ROOT).endsWith(".mp4")) selected else "$selected.mp4"
            if (outputPath.equals(inputFileField.text, ignoreCase = true)) {
                JOptionPane.showMessageDialog(
                    this,
                    "Output file cannot be the same as input file",
                    "Error",
                    JOptionPane.ERROR_MESSAGE
                )
                return
            }

            outputFileField.text = outputPath
            updateActionButtonState()
        }
    }

    private fun setWindowIcon() {
        val iconUrl = javaClass.getResource("/icon-32.png")
        if (iconUrl != null) {
            iconImage = Toolkit.getDefaultToolkit().getImage(iconUrl)
        }
    }

    private fun startEncoding() {
        try {
            val inputPath = Path.of(inputFileField.text).toAbsolutePath().normalize()
            if (!Files.isRegularFile(inputPath)) {
                JOptionPane.showMessageDialog(this, "Input video file does not exist", "Error", JOptionPane.ERROR_MESSAGE)
                return
            }

            val outputPath = if (outputFileField.text.isBlank()) {
                defaultOutputFor(inputPath).also { outputFileField.text = it.toString() }
            } else {
                Path.of(outputFileField.text).toAbsolutePath().normalize()
            }

            if (inputPath == outputPath) {
                JOptionPane.showMessageDialog(this, "Output file cannot be the same as input file", "Error", JOptionPane.ERROR_MESSAGE)
                return
            }

            val targetSizeMb = (targetSizeSpinner.value as Number).toInt()
            val tolerancePercent = (toleranceSpinner.value as Number).toDouble()
            val maxIterations = (maxIterationsSpinner.value as Number).toInt()
            val qualityPreset = qualityPresetCombo.selectedIndex + 1
            val request = EncodeService.EncodeRequest(
                inputFile = inputPath,
                outputFile = outputPath,
                targetSizeMb = targetSizeMb,
                tolerancePercent = tolerancePercent,
                maxIterations = maxIterations,
                qualityPreset = qualityPreset
            )

            encodingWorker = EncodingWorker(request).also {
                it.addPropertyChangeListener(::handleWorkerPropertyChange)
            }

            setControlsEnabled(false)
            progressBar.isIndeterminate = false
            progressBar.value = 0
            progressBar.string = "0%"
            actionButton.text = "Cancel"
            actionButton.isEnabled = true
            setFrameTitleWithStatus("Preparing encode...")

            encodingWorker!!.execute()
        } catch (exception: Exception) {
            JOptionPane.showMessageDialog(this, exception.message, "Error", JOptionPane.ERROR_MESSAGE)
        }
    }

    private fun handleWorkerPropertyChange(event: PropertyChangeEvent) {
        if (event.propertyName != "progress") {
            return
        }

        val value = event.newValue as Int
        progressBar.isIndeterminate = false
        progressBar.value = value
        progressBar.string = "$value%"
    }

    private fun isEncodingRunning(): Boolean = encodingWorker?.isDone == false

    private fun cancelEncodingIfRunning() {
        if (!isEncodingRunning()) {
            return
        }

        encodingWorker!!.cancel(true)
        actionButton.isEnabled = false
        setFrameTitleWithStatus("Cancelling...")
    }

    private fun setControlsEnabled(enabled: Boolean) {
        inputFileButton.isEnabled = enabled
        outputFileButton.isEnabled = enabled && inputFileField.text.isNotBlank()
        targetSizeSpinner.isEnabled = enabled
        toleranceSpinner.isEnabled = enabled
        maxIterationsSpinner.isEnabled = enabled
        qualityPresetCombo.isEnabled = enabled
    }

    private fun resetControls() {
        inputFileField.text = ""
        outputFileField.text = ""
        progressBar.isIndeterminate = false
        progressBar.value = 0
    }

    private fun updateActionButtonState() {
        if (isEncodingRunning()) {
            actionButton.isEnabled = true
            return
        }

        val hasInput = inputFileField.text.isNotBlank()
        val hasOutput = outputFileField.text.isNotBlank()
        actionButton.isEnabled = binariesReady && hasInput && hasOutput
    }

    private fun setFrameTitleWithStatus(status: String?) {
        title = if (status.isNullOrBlank()) INITIAL_TITLE else status
    }

    private fun defaultOutputFor(inputFile: Path): Path {
        val fileName = inputFile.fileName.toString()
        val dotIndex = fileName.lastIndexOf('.')
        val stem = if (dotIndex > 0) fileName.substring(0, dotIndex) else fileName
        val generated = "$stem-squashed-${Instant.now().toEpochMilli()}.mp4"
        return inputFile.parent.resolve(generated)
    }

    private fun applyInputFile(inputPath: Path) {
        val normalized = inputPath.toAbsolutePath().normalize()
        inputFileField.text = normalized.toString()
        outputFileField.text = defaultOutputFor(normalized).toString()
        outputFileButton.isEnabled = true
        updateActionButtonState()
    }

    private fun isSupportedVideoFile(path: Path): Boolean {
        if (!Files.isRegularFile(path)) {
            return false
        }

        val fileName = path.fileName ?: return false
        val name = fileName.toString()
        val dotIndex = name.lastIndexOf('.')
        if (dotIndex < 0 || dotIndex == name.length - 1) {
            return false
        }

        val extension = name.substring(dotIndex + 1).lowercase(Locale.ROOT)
        return SUPPORTED_VIDEO_EXTENSIONS.contains(extension)
    }

    private fun installFileDropSupport(root: java.awt.Component) {
        installFileDropSupportRecursively(root)
    }

    private fun installFileDropSupportRecursively(component: java.awt.Component) {
        if (component is JComponent) {
            val fallbackTransferHandler = component.transferHandler
            component.transferHandler = createFileDropTransferHandler(fallbackTransferHandler)
        }

        if (component is Container) {
            for (child in component.components) {
                installFileDropSupportRecursively(child)
            }
        }
    }

    private fun createFileDropTransferHandler(fallbackTransferHandler: TransferHandler?): TransferHandler {
        return object : TransferHandler() {
            override fun canImport(support: TransferSupport): Boolean {
                val isFileDrop = support.isDrop && support.isDataFlavorSupported(javaFileListFlavor)
                if (isFileDrop) {
                    return !isEncodingRunning()
                }

                return fallbackTransferHandler?.canImport(support) == true
            }

            override fun importData(support: TransferSupport): Boolean {
                val isFileDrop = support.isDrop && support.isDataFlavorSupported(javaFileListFlavor)
                if (!isFileDrop) {
                    return fallbackTransferHandler?.importData(support) == true
                }

                if (isEncodingRunning()) {
                    return false
                }

                return try {
                    val transferable = support.transferable
                    @Suppress("UNCHECKED_CAST")
                    val files = transferable.getTransferData(javaFileListFlavor) as List<File>
                    if (files.isEmpty()) {
                        return false
                    }

                    val droppedPath = files.first().toPath()
                    if (!isSupportedVideoFile(droppedPath)) {
                        JOptionPane.showMessageDialog(
                            this@MainFrame,
                            "Dropped item is not a supported video file.",
                            "Unsupported file",
                            JOptionPane.WARNING_MESSAGE
                        )
                        return false
                    }

                    applyInputFile(droppedPath)
                    true
                } catch (exception: Exception) {
                    JOptionPane.showMessageDialog(
                        this@MainFrame,
                        exception.message,
                        "Drop failed",
                        JOptionPane.ERROR_MESSAGE
                    )
                    false
                }
            }
        }
    }

    private inner class EncodingWorker(private val request: EncodeService.EncodeRequest) :
        SwingWorker<EncodeService.EncodeResult, Void?>() {
        private var lastTitleUpdateNanos: Long = 0
        private var lastTitleText = ""

        override fun doInBackground(): EncodeService.EncodeResult {
            return encodeService.resizeVideoToTarget(
                request = request,
                progressListener = EncodeService.ProgressListener { update ->
                    if (update.progressPercent >= 0) {
                        setProgress(update.progressPercent)
                    }

                    updateFrameTitle(update.titleText, update.progressPercent >= 100)
                },
                cancelled = BooleanSupplier { isCancelled }
            )
        }

        private fun updateFrameTitle(titleText: String?, force: Boolean) {
            if (titleText.isNullOrBlank()) {
                return
            }

            val now = System.nanoTime()
            if (!force) {
                if (titleText == lastTitleText) {
                    return
                }
                if (now - lastTitleUpdateNanos < TITLE_UPDATE_MIN_INTERVAL_NANOS) {
                    return
                }
            }

            lastTitleText = titleText
            lastTitleUpdateNanos = now
            SwingUtilities.invokeLater {
                if (encodingWorker === this) {
                    setFrameTitleWithStatus(titleText)
                }
            }
        }

        override fun done() {
            setControlsEnabled(true)
            actionButton.text = INITIAL_BUTTON_TEXT
            encodingWorker = null

            try {
                val result = get()

                progressBar.value = 100
                progressBar.string = "100%"

                val outcome = if (result.success) "Success" else "Partial"
                val delta = result.fileSizeBytes - result.targetSizeBytes
                val deltaSign = if (delta > 0) "+" else ""

                val message = String.format(
                    Locale.US,
                    "%s: wrote %s\nFinal size %s (target %s, delta %s%s)\nIterations %d/%d at %.0f kbps\nTotal time %s",
                    outcome,
                    result.filePath,
                    formatBytes(result.fileSizeBytes),
                    formatBytes(result.targetSizeBytes),
                    deltaSign,
                    formatBytes(abs(delta)),
                    result.iteration,
                    request.maxIterations,
                    result.videoBitrateKbps,
                    formatDuration(result.elapsedSeconds)
                )

                if (!closeRequested) {
                    JOptionPane.showMessageDialog(this@MainFrame, message, outcome, JOptionPane.INFORMATION_MESSAGE)
                }
            } catch (_: CancellationException) {
                progressBar.value = 0
                progressBar.string = "Canceled"
            } catch (exception: Exception) {
                progressBar.value = 0
                progressBar.string = "Failed"
                if (!closeRequested) {
                    val message = exception.cause?.message ?: exception.message
                    JOptionPane.showMessageDialog(this@MainFrame, message, "Error", JOptionPane.ERROR_MESSAGE)
                }
            }

            if (closeRequested) {
                shutdownNow()
                return
            }

            setFrameTitleWithStatus(null)
            resetControls()
            updateActionButtonState()
        }
    }

    private inner class BinaryCheckWorker : SwingWorker<Boolean, Void?>() {
        override fun doInBackground(): Boolean {
            return binaryService.hasBinary("ffmpeg") && binaryService.hasBinary("ffprobe")
        }

        override fun done() {
            try {
                binariesReady = get()
                if (!binariesReady) {
                    val response = JOptionPane.showConfirmDialog(
                        this@MainFrame,
                        "FFmpeg or FFprobe is missing from your system.\nWould you like to download them?",
                        "Missing binaries",
                        JOptionPane.YES_NO_OPTION,
                        JOptionPane.INFORMATION_MESSAGE
                    )
                    if (response != JOptionPane.YES_OPTION) {
                        dispose()
                        return
                    }

                    binaryDownloaderDialog.setLocationRelativeTo(this@MainFrame)
                    binaryDownloaderDialog.isVisible = true
                    binariesReady = binaryService.hasBinary("ffmpeg") && binaryService.hasBinary("ffprobe")

                    if (!binariesReady) {
                        JOptionPane.showMessageDialog(
                            this@MainFrame,
                            "FFmpeg/FFprobe download did not complete.",
                            "Missing binaries",
                            JOptionPane.ERROR_MESSAGE
                        )
                        dispose()
                        return
                    }
                }
            } catch (exception: Exception) {
                binariesReady = false
                JOptionPane.showMessageDialog(this@MainFrame, exception.message, "Error", JOptionPane.ERROR_MESSAGE)
            }

            updateActionButtonState()
        }
    }

    private fun shutdownNow() {
        dispose()
        System.exit(0)
    }

    private companion object {
        const val INITIAL_TITLE: String = BuildInfo.APP_NAME
        const val INITIAL_BUTTON_TEXT = "Squash it!"
        val SUPPORTED_VIDEO_EXTENSIONS = setOf("mp4", "mov", "mkv", "avi", "wmv", "webm")
        const val TITLE_UPDATE_MIN_INTERVAL_NANOS = 250_000_000L

        fun addRow(
            panel: JPanel,
            row: Int,
            labelText: String,
            field: JComponent,
            isLastRow: Boolean
        ) {
            val bottomInset = if (isLastRow) 0 else 8
            val insets = Insets(0, 0, bottomInset, 8)
            panel.add(JLabel(labelText), createConstraints(0, row, 0.0, GridBagConstraints.NONE, insets))
            panel.add(field, createConstraints(1, row, 1.0, GridBagConstraints.HORIZONTAL, insets))
        }

        fun addRowWithTrailingControl(
            panel: JPanel,
            row: Int,
            labelText: String,
            field: JComponent,
            trailingControl: JComponent,
            isLastRow: Boolean
        ) {
            val bottomInset = if (isLastRow) 0 else 8
            addRow(panel, row, labelText, field, isLastRow)
            val trailingInsets = Insets(0, 0, bottomInset, 0)
            panel.add(trailingControl, createConstraints(2, row, 0.0, GridBagConstraints.NONE, trailingInsets))
        }

        fun createConstraints(
            gridX: Int,
            gridY: Int,
            weightX: Double,
            fill: Int,
            insets: Insets
        ): GridBagConstraints {
            return GridBagConstraints().apply {
                this.gridx = gridX
                this.gridy = gridY
                this.weightx = weightX
                this.fill = fill
                this.anchor = GridBagConstraints.WEST
                this.insets = insets
            }
        }

        fun formatBytes(bytes: Long): String {
            if (bytes < 1024) {
                return "$bytes B"
            }

            var value = bytes.toDouble()
            val units = arrayOf("B", "KB", "MB", "GB", "TB")
            var unitIndex = 0

            while (value >= 1024 && unitIndex < units.size - 1) {
                value /= 1024
                unitIndex++
            }

            return String.format(Locale.US, "%.2f %s", value, units[unitIndex])
        }

        fun formatDuration(totalSeconds: Double): String {
            val seconds = maxOf(0.0, totalSeconds).toInt()
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
    }
}
