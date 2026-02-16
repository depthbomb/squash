import com.github.jengelman.gradle.plugins.shadow.tasks.ShadowJar

plugins {
    id("java")
    id("application")
    id("org.jetbrains.kotlin.jvm") version "2.3.0"
    id("com.gradleup.shadow") version "9.3.1"
}

group = "com.caprinelogic"

val appId: String by project
val appName: String by project
val appDisplayName: String by project
val appVendor: String by project
val appVersion: String by project
val appDescription: String by project
val appCopyright: String by project
val appRepoUrl: String by project
val appReleasesUrl: String by project
val appLatestReleaseUrl: String by project
val appIssuesUrl: String by project

val appMainClass = "com.caprinelogic.Main"
val mainManifest = mapOf("Main-Class" to appMainClass)

val jpackageInputDir = layout.buildDirectory.dir("libs")
val jpackageOutputDir = layout.buildDirectory.dir("jpackage/output")
val jpackageIconFile = layout.projectDirectory.file("packaging/icons/icon.ico").asFile

repositories {
    mavenCentral()
}

dependencies {
    implementation("com.formdev:flatlaf:3.7")
    implementation("org.springframework:spring-context:7.0.4")
}

java {
    toolchain {
        languageVersion.set(JavaLanguageVersion.of(25))
    }
}

kotlin {
    jvmToolchain(25)
}

application {
    mainClass.set(appMainClass)
}

tasks.withType<JavaCompile>().configureEach {
    options.encoding = "UTF-8"
}

tasks.jar {
    manifest {
        attributes(mainManifest)
    }
}

val shadowJarTask = tasks.named<ShadowJar>("shadowJar").apply {
    configure {
        archiveClassifier.set("all")
        manifest {
            attributes(mainManifest)
        }
    }
}

val cleanJpackageOutputTask = tasks.register<Delete>("cleanJpackageOutput") {
    delete(jpackageOutputDir)
}

tasks.register<Exec>("jpackageAppImage") {
    group = "distribution"
    dependsOn(shadowJarTask, cleanJpackageOutputTask)

    val shadowArchive = shadowJarTask.flatMap { it.archiveFile }
    val shadowArchiveName = shadowJarTask.flatMap { it.archiveFileName }

    inputs.file(shadowArchive)
    outputs.dir(jpackageOutputDir)

    executable = "jpackage"
    args(
        "--type", "app-image",
        "--name", appName,
        "--input", jpackageInputDir.get().asFile.absolutePath,
        "--main-jar", shadowArchiveName.get(),
        "--main-class", appMainClass,
        "--dest", jpackageOutputDir.get().asFile.absolutePath,
        "--icon", jpackageIconFile.absolutePath,
        "--app-version", appVersion,
        "--description", appDescription,
        "--copyright", appCopyright,
        "--vendor", appVendor
    )
}

tasks.register("jpackageExe") {
    group = "distribution"
    dependsOn("jpackageAppImage")
}

tasks.register<Exec>("createSetup") {
    group = "distribution"

    val definitions = linkedMapOf(
        "AppId" to appId,
        "NameLong" to appDisplayName,
        "Version" to appVersion,
        "Description" to appDescription,
        "Company" to appVendor,
        "ExeBaseName" to appName,
        "ExeName" to "${appName}.exe",
        "Copyright" to appCopyright,
        "RepoUrl" to appRepoUrl,
        "ReleasesUrl" to appReleasesUrl,
        "IssuesUrl" to appIssuesUrl
    )

    executable = "iscc.exe"
    args("packaging/setup/setup.iss")
    args(definitions.map {(key, value) -> "/d$key=$value" })
}