package com.caprinelogic.ui;

import com.caprinelogic.BuildInfo;

import javax.swing.*;
import java.awt.*;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.net.URI;

public class AboutDialog extends JDialog {
    public AboutDialog(Frame owner) {
        super(owner, "About", true);

        setDefaultCloseOperation(WindowConstants.HIDE_ON_CLOSE);

        var content = new JPanel();
        content.setBorder(BorderFactory.createEmptyBorder(12, 12, 12, 12));
        content.setLayout(new BoxLayout(content, BoxLayout.Y_AXIS));

        var iconLabel = createInfoIconLabel();
        if (iconLabel != null) {
            var iconPanel = new JPanel(new FlowLayout(FlowLayout.CENTER, 0, 0));
            iconPanel.setOpaque(false);
            iconPanel.add(iconLabel);
            content.add(iconPanel);
            content.add(Box.createVerticalStrut(10));
        }

        content.add(createCenteredLabel(BuildInfo.APP_NAME + " " + BuildInfo.APP_VERSION));
        content.add(Box.createVerticalStrut(4));

        var linksPanel = new JPanel(new FlowLayout(FlowLayout.CENTER, 12, 0));
        linksPanel.setOpaque(false);
        linksPanel.add(createLinkLabel("GitHub", BuildInfo.APP_REPO_URL));
        linksPanel.add(createLinkLabel("Issues", BuildInfo.APP_ISSUES_URL));
        linksPanel.add(createLinkLabel("Releases", BuildInfo.APP_RELEASES_URL));
        content.add(linksPanel);

        setContentPane(content);
        pack();
        setResizable(false);
        setSize(new Dimension(250, getPreferredSize().height));
    }

    private JLabel createCenteredLabel(String text) {
        var label = new JLabel(text, SwingConstants.CENTER);
        label.setAlignmentX(JComponent.CENTER_ALIGNMENT);
        return label;
    }

    private JLabel createInfoIconLabel() {
        var iconUrl = getClass().getResource("/icon.png");
        if (iconUrl == null) {
            return null;
        }

        var iconImage = new ImageIcon(iconUrl).getImage();
        var scaled = iconImage.getScaledInstance(64, 64, Image.SCALE_SMOOTH);
        return new JLabel(new ImageIcon(scaled));
    }

    private JLabel createLinkLabel(String text, String url) {
        var linkLabel = new JLabel(String.format("<html><a href='%s'>%s</a></html>", url, text));
        linkLabel.setCursor(Cursor.getPredefinedCursor(Cursor.HAND_CURSOR));
        linkLabel.addMouseListener(new MouseAdapter() {
            @Override
            public void mouseClicked(MouseEvent event) {
                openLink(url);
            }
        });
        return linkLabel;
    }

    private void openLink(String url) {
        try {
            Desktop.getDesktop().browse(URI.create(url));
        } catch (Exception exception) {
            JOptionPane.showMessageDialog(
                    this,
                    "Failed to open link: " + url,
                    "Error",
                    JOptionPane.ERROR_MESSAGE
            );
        }
    }
}
