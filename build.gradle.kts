import com.github.jengelman.gradle.plugins.shadow.tasks.ShadowJar
import org.apache.tools.ant.filters.ReplaceTokens

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
val buildInfoOutputDir = layout.buildDirectory.dir("generated/sources/buildInfo/java")

fun escapeJavaStringLiteral(value: String): String = buildString {
    value.forEach { ch ->
        when (ch) {
            '\\' -> append("\\\\")
            '"' -> append("\\\"")
            '\n' -> append("\\n")
            '\r' -> append("\\r")
            '\t' -> append("\\t")
            else -> append(ch)
        }
    }
}

repositories {
    mavenCentral()
}

dependencies {
    implementation("com.formdev:flatlaf:3.7")
    implementation("com.formdev:flatlaf-intellij-themes:3.7")
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

val buildInfoTokens = mapOf(
    "APP_NAME" to escapeJavaStringLiteral(appDisplayName),
    "APP_VERSION" to escapeJavaStringLiteral(appVersion),
    "APP_VENDOR" to escapeJavaStringLiteral(appVendor),
    "APP_REPO_URL" to escapeJavaStringLiteral(appRepoUrl),
    "APP_RELEASES_URL" to escapeJavaStringLiteral(appReleasesUrl),
    "APP_ISSUES_URL" to escapeJavaStringLiteral(appIssuesUrl)
)

val generateBuildInfo = tasks.register<Copy>("generateBuildInfo") {
    from("src/main/templates") {
        include("BuildInfo.java.template")
        rename("BuildInfo.java.template", "BuildInfo.java")
        filter<ReplaceTokens>("tokens" to buildInfoTokens)
    }
    into(buildInfoOutputDir.map { it.dir("com/caprinelogic") })
    filteringCharset = "UTF-8"
    inputs.properties(buildInfoTokens)
}

sourceSets {
    named("main") {
        java.srcDir(buildInfoOutputDir)
    }
}

tasks.withType<JavaCompile>().configureEach {
    dependsOn(generateBuildInfo)
    options.encoding = "UTF-8"
}

tasks.named("compileKotlin") {
    dependsOn(generateBuildInfo)
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
