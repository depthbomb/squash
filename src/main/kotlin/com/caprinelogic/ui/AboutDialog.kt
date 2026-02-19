package com.caprinelogic.ui

import com.caprinelogic.BuildInfo
import java.awt.Cursor
import java.awt.Desktop
import java.awt.Dimension
import java.awt.FlowLayout
import java.awt.Frame
import java.awt.Image
import java.awt.event.MouseAdapter
import java.awt.event.MouseEvent
import java.net.URI
import javax.swing.BorderFactory
import javax.swing.Box
import javax.swing.BoxLayout
import javax.swing.ImageIcon
import javax.swing.JComponent
import javax.swing.JDialog
import javax.swing.JLabel
import javax.swing.JOptionPane
import javax.swing.JPanel
import javax.swing.SwingConstants
import javax.swing.WindowConstants

class AboutDialog(owner: Frame) : JDialog(owner, "About", true) {
    init {
        defaultCloseOperation = WindowConstants.HIDE_ON_CLOSE

        val content = JPanel().apply {
            border = BorderFactory.createEmptyBorder(12, 12, 12, 12)
            layout = BoxLayout(this, BoxLayout.Y_AXIS)
        }

        val iconLabel = createInfoIconLabel()
        if (iconLabel != null) {
            val iconPanel = JPanel(FlowLayout(FlowLayout.CENTER, 0, 0)).apply { isOpaque = false }
            iconPanel.add(iconLabel)
            content.add(iconPanel)
            content.add(Box.createVerticalStrut(10))
        }

        content.add(createCenteredLabel("${BuildInfo.APP_NAME} ${BuildInfo.APP_VERSION}"))
        content.add(Box.createVerticalStrut(4))

        val linksPanel = JPanel(FlowLayout(FlowLayout.CENTER, 12, 0)).apply { isOpaque = false }
        linksPanel.add(createLinkLabel("GitHub", BuildInfo.APP_REPO_URL))
        linksPanel.add(createLinkLabel("Issues", BuildInfo.APP_ISSUES_URL))
        linksPanel.add(createLinkLabel("Releases", BuildInfo.APP_RELEASES_URL))
        content.add(linksPanel)

        contentPane = content
        pack()
        isResizable = false
        setSize(Dimension(250, preferredSize.height))
    }

    private fun createCenteredLabel(text: String): JLabel {
        return JLabel(text, SwingConstants.CENTER).apply {
            alignmentX = JComponent.CENTER_ALIGNMENT
        }
    }

    private fun createInfoIconLabel(): JLabel? {
        val iconUrl = javaClass.getResource("/icon.png") ?: return null
        val iconImage = ImageIcon(iconUrl).image
        val scaled = iconImage.getScaledInstance(64, 64, Image.SCALE_SMOOTH)
        return JLabel(ImageIcon(scaled))
    }

    private fun createLinkLabel(text: String, url: String): JLabel {
        return JLabel("<html><a href='$url'>$text</a></html>").apply {
            cursor = Cursor.getPredefinedCursor(Cursor.HAND_CURSOR)
            addMouseListener(object : MouseAdapter() {
                override fun mouseClicked(event: MouseEvent) {
                    openLink(url)
                }
            })
        }
    }

    private fun openLink(url: String) {
        try {
            Desktop.getDesktop().browse(URI.create(url))
        } catch (_: Exception) {
            JOptionPane.showMessageDialog(
                this,
                "Failed to open link: $url",
                "Error",
                JOptionPane.ERROR_MESSAGE
            )
        }
    }
}
