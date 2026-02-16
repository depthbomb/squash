package com.caprinelogic.service

import org.springframework.stereotype.Component
import java.io.BufferedReader
import java.io.InputStreamReader
import java.nio.file.Files
import java.nio.file.Path

@Component
class BinaryService {
    fun getBinaryPath(name: String): Path? {
        val localPath = appDirectory().resolve("$name.exe")
        if (Files.exists(localPath) && Files.isRegularFile(localPath)) {
            return localPath
        }

        return try {
            val process = ProcessBuilder("where", name)
                .redirectErrorStream(true)
                .start()

            BufferedReader(InputStreamReader(process.inputStream)).use { reader ->
                val firstResult = reader.readLine()
                if (process.waitFor() == 0 && !firstResult.isNullOrBlank()) {
                    Path.of(firstResult.trim())
                } else {
                    null
                }
            }
        } catch (_: Exception) {
            null
        }
    }

    fun hasBinary(name: String): Boolean = getBinaryPath(name) != null

    private fun appDirectory(): Path {
        return try {
            Path.of(
                BinaryService::class.java
                    .protectionDomain
                    .codeSource
                    .location
                    .toURI()
            ).parent
        } catch (_: Exception) {
            Path.of(System.getProperty("user.dir"))
        }
    }
}