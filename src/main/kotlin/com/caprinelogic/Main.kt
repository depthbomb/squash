package com.caprinelogic

import com.caprinelogic.config.AppConfig
import com.caprinelogic.ui.MainFrame
import com.formdev.flatlaf.FlatDarkLaf
import com.formdev.flatlaf.FlatIntelliJLaf
import org.springframework.context.annotation.AnnotationConfigApplicationContext
import java.io.BufferedReader
import java.io.InputStreamReader
import java.util.*
import javax.swing.SwingUtilities

fun main() {
    if (isWindowsDarkMode()) {
        FlatDarkLaf.setup()
    } else {
        FlatIntelliJLaf.setup()
    }

    val context = AnnotationConfigApplicationContext(AppConfig::class.java)
    Runtime.getRuntime().addShutdownHook(Thread(context::close))
    SwingUtilities.invokeLater { context.getBean(MainFrame::class.java).start() }
}

private fun isWindowsDarkMode(): Boolean {
    val process = try {
        ProcessBuilder(
            "reg",
            "query",
            "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
            "/v",
            "AppsUseLightTheme"
        ).start()
    } catch (_: Exception) {
        return false
    }

    return try {
        BufferedReader(InputStreamReader(process.inputStream)).use { reader ->
            reader.lineSequence().any { line ->
                val normalized = line.trim().lowercase(Locale.ROOT)
                normalized.contains("appsuselighttheme") && normalized.endsWith("0x0")
            }
        }
    } finally {
        process.destroy()
    }
}
