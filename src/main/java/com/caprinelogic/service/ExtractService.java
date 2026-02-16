package com.caprinelogic.service;

import org.springframework.stereotype.Component;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.InvalidPathException;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Optional;

@Component
public class ExtractService {
    private static final String SEVEN_ZIP_EXECUTABLE = "7za.exe";

    public void extractFilesFromArchive(Path archivePath, String[] fileNames, Path directory) throws IOException, InterruptedException {
        var binPath = getSevenZipExecutablePath().toString();
        var command = new ArrayList<String>(8);
        command.add(binPath);
        command.add("e");
        command.add(archivePath.toString());
        command.add("-r");
        command.add("-o" + directory.toString());
        command.add("-aoa");
        command.addAll(Arrays.asList(fileNames));

        new ProcessBuilder(command)
                .redirectErrorStream(true)
                .start()
                .waitFor();
    }

    private Path getSevenZipExecutablePath() {
        return findSevenZipExecutable().orElseThrow(() -> new IllegalStateException("Could not locate 7za.exe."));
    }

    private Optional<Path> findSevenZipExecutable() {
        return findNextToApplicationExecutable().or(this::findOnPath);
    }

    private Optional<Path> findNextToApplicationExecutable() {
        return ProcessHandle.current()
            .info()
            .command()
            .map(Path::of)
            .map(Path::toAbsolutePath)
            .map(Path::normalize)
            .map(Path::getParent)
            .map(parent -> parent.resolve(SEVEN_ZIP_EXECUTABLE))
            .filter(Files::isRegularFile);
    }

    private Optional<Path> findOnPath() {
        var pathValue = System.getenv("PATH");
        if (pathValue == null || pathValue.isBlank()) {
            return Optional.empty();
        }

        return Arrays.stream(pathValue.split(File.pathSeparator))
            .map(this::stripWrappingQuotes)
            .map(String::trim)
            .filter(entry -> !entry.isEmpty())
            .map(this::resolveSevenZipFromDirectory)
            .flatMap(Optional::stream)
            .findFirst();
    }

    private Optional<Path> resolveSevenZipFromDirectory(String directory) {
        try {
            var candidate = Path.of(directory).resolve(SEVEN_ZIP_EXECUTABLE).toAbsolutePath().normalize();

            return Files.isRegularFile(candidate) ? Optional.of(candidate) : Optional.empty();
        } catch (InvalidPathException ignored) {
            return Optional.empty();
        }
    }

    private String stripWrappingQuotes(String value) {
        if (value.length() >= 2 && value.startsWith("\"") && value.endsWith("\"")) {
            return value.substring(1, value.length() - 1);
        }

        return value;
    }
}
