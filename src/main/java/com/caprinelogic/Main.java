package com.caprinelogic;

import com.caprinelogic.config.AppConfig;
import com.caprinelogic.ui.MainFrame;
import com.formdev.flatlaf.intellijthemes.FlatArcDarkOrangeIJTheme;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.context.annotation.AnnotationConfigApplicationContext;

import javax.swing.*;

public final class Main {
    static void main() {
        FlatArcDarkOrangeIJTheme.setup();
        ConfigurableApplicationContext context = new AnnotationConfigApplicationContext(AppConfig.class);
        Runtime.getRuntime().addShutdownHook(new Thread(context::close));
        SwingUtilities.invokeLater(() -> context.getBean(MainFrame.class).start());
    }
}
