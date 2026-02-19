package com.caprinelogic.service

import org.springframework.stereotype.Component
import java.io.File
import java.io.IOException
import java.nio.file.Files
import java.nio.file.InvalidPathException
import java.nio.file.Path

@Component
class ExtractService {
    fun extractFilesFromArchive(archivePath: Path, fileNames: Array<String>, directory: Path) {
        val binPath = sevenZipExecutablePath.toString()
        val command = ArrayList<String>(8)
        command.add(binPath)
        command.add("e")
        command.add(archivePath.toString())
        command.add("-r")
        command.add("-o${directory}")
        command.add("-aoa")
        command.addAll(fileNames)

        ProcessBuilder(command)
            .redirectErrorStream(true)
            .start()
            .waitFor()
    }

    private val sevenZipExecutablePath: Path
        get() = findSevenZipExecutable() ?: throw IllegalStateException("Could not locate $SEVEN_ZIP_EXECUTABLE.")

    private fun findSevenZipExecutable(): Path? = findNextToApplicationExecutable() ?: findOnPath()

    private fun findNextToApplicationExecutable(): Path? {
        return ProcessHandle.current()
            .info()
            .command()
            .map(Path::of)
            .map(Path::toAbsolutePath)
            .map(Path::normalize)
            .map(Path::getParent)
            .map { it.resolve(SEVEN_ZIP_EXECUTABLE) }
            .filter(Files::isRegularFile)
            .orElse(null)
    }

    private fun findOnPath(): Path? {
        val pathValue = System.getenv("PATH")
        if (pathValue.isNullOrBlank()) {
            return null
        }

        return pathValue
            .split(File.pathSeparator)
            .asSequence()
            .map(::stripWrappingQuotes)
            .map(String::trim)
            .filter(String::isNotEmpty)
            .map(::resolveSevenZipFromDirectory)
            .firstOrNull { it != null }
    }

    private fun resolveSevenZipFromDirectory(directory: String): Path? {
        return try {
            val candidate = Path.of(directory).resolve(SEVEN_ZIP_EXECUTABLE).toAbsolutePath().normalize()
            if (Files.isRegularFile(candidate)) candidate else null
        } catch (_: InvalidPathException) {
            null
        }
    }

    private fun stripWrappingQuotes(value: String): String {
        return if (value.length >= 2 && value.startsWith("\"") && value.endsWith("\"")) {
            value.substring(1, value.length - 1)
        } else {
            value
        }
    }

    private companion object {
        const val SEVEN_ZIP_EXECUTABLE = "7za.exe"
    }
}
