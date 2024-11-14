import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.gradle.api.tasks.testing.logging.TestLogEvent
import org.jetbrains.intellij.platform.gradle.TestFrameworkType
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

group = "com.voltum"
version = "1.0-SNAPSHOT"


fun properties(key: String) = providers.gradleProperty(key)
fun environment(key: String) = providers.environmentVariable(key)

fun hasProp(name: String): Boolean = extra.has(name)
fun prop(name: String): String = extra.properties[name] as? String ?: error("Property `$name` is not defined in gradle.properties")
fun prop(name: String, default: String): String = extra.properties[name] as? String ?: default

plugins {
    id("java")
    id("org.jetbrains.kotlin.jvm") version "1.9.25"
    id("org.jetbrains.intellij.platform") version "2.1.0"
    id("org.jetbrains.gradle.plugin.idea-ext") version "1.1.9"
//    id("org.jetbrains.intellij.platform.migration") version "2.1.0"
}


repositories {
    mavenCentral()
    intellijPlatform {
        intellijDependencies()
        defaultRepositories()
    }
//    maven("https://jitpack.io")
}

dependencies {

    intellijPlatform {
        intellijIdeaUltimate("2024.2.3")

        pluginVerifier()
//        zipSigner()
        instrumentationTools()

        testFramework(TestFrameworkType.Platform)
        plugins(providers.gradleProperty("platformPlugins").map { it.split(',') })
        
    }

    val lsp4ijVersion = "0.7.0"
    intellijPlatformPluginDependency("com.jetbrains.plugins:com.redhat.devtools.lsp4ij:$lsp4ijVersion")
    compileOnly("com.jetbrains.plugins:com.redhat.devtools.lsp4ij:$lsp4ijVersion")
    compileOnly("org.eclipse.lsp4j:org.eclipse.lsp4j:0.21.1")
    //implementation("junit:junit:4.13.2")
    testImplementation("org.opentest4j:opentest4j:1.3.0")
    testImplementation("junit:junit:4.13.2")
}

idea {
    module {
        setDownloadJavadoc(true)
        setDownloadSources(true)
    }
}


intellijPlatform {
    buildSearchableOptions = false
    instrumentCode = true
    autoReload = false
    

    /*pluginVerification {
        ides {
            ide(IntelliJPlatformType.IntellijIdeaUltimate, "2024.2.3")
            local(file("C:\\Users\\Sam8t\\AppData\\Local\\Programs\\IntelliJ IDEA Ultimate"))
            recommended()
//            select {
//                types = listOf(IntelliJPlatformType.IntellijIdeaUltimate)
//                channels = listOf(ProductRelease.Channel.RELEASE)
//                sinceBuild.set("242")
//                untilBuild.set("242.*")
//            }
        }
    }*/

}

allprojects {
    sourceSets["main"].java.srcDirs(
        "src/main/gen"
    )
    sourceSets["main"].kotlin.srcDirs(
        "src/main"
    )
}



tasks {
    withType<JavaCompile> {
        sourceCompatibility = "21"
        targetCompatibility = "21"
    }
    withType<KotlinCompile> {
        kotlinOptions.jvmTarget = "21"
        compilerOptions.freeCompilerArgs.add("-opt-in=kotlin.time.ExperimentalTime")
        compilerOptions.freeCompilerArgs.add("-opt-in=kotlin.contracts.ExperimentalContracts")
    }
    runIde {
//        dependsOn("buildPlugin")
        enabled = true
    }
    prepareSandbox {
        enabled = true
        destinationDir = file("build/sandbox")
    }

    patchPluginXml {
        sinceBuild.set("242")
        untilBuild.set("242.*")
    }

    test {
        systemProperty("java.awt.headless", "true")
        systemProperty("idea.tests.overwrite.data", "false")
        systemProperty("dumpAstTypeNames", "false")
        testLogging {
            showStandardStreams = false// prop("showStandardStreams").toBoolean()
            afterSuite(
                KotlinClosure2<TestDescriptor, TestResult, Unit>(
                    { desc, result ->
                        if (desc.parent == null) { // will match the outermost suite
                            val output =
                                "Results: ${result.resultType} (${result.testCount} tests, ${result.successfulTestCount} passed, ${result.failedTestCount} failed, ${result.skippedTestCount} skipped)"
                            println(output)
                        }
                    })
            )
        }
    }
    /*
    
        // Configure UI tests plugin
        // Read more: https://github.com/JetBrains/intellij-ui-test-robot
        runIdeForUiTests {
            autoReloadPlugins = true
    
            systemProperty("robot-server.port", "8082")
            systemProperty("ide.mac.message.dialogs.as.sheets", "false")
            systemProperty("jb.privacy.policy.text", "<!--999.999-->")
            systemProperty("jb.consents.confirmation.enabled", "false")
        }
    */

    signPlugin {
        certificateChain.set(System.getenv("CERTIFICATE_CHAIN"))
        privateKey.set(System.getenv("PRIVATE_KEY"))
        password.set(System.getenv("PRIVATE_KEY_PASSWORD"))
    }

    publishPlugin {
        token.set(System.getenv("PUBLISH_TOKEN"))
    }

    afterEvaluate {
        tasks.withType<AbstractTestTask> {
            testLogging {
                if (prop("showTestStatus", "false").toBoolean()) {
                    events = setOf(TestLogEvent.STARTED, TestLogEvent.PASSED, TestLogEvent.SKIPPED, TestLogEvent.FAILED)
                }
                exceptionFormat = TestExceptionFormat.FULL
            }
        }

        tasks.withType<Test>().configureEach {
            jvmArgs = listOf("-Xmx2g", "-XX:-OmitStackTraceInFastThrow")

            // We need to prevent the platform-specific shared JNA library to loading from the system library paths,
            // because otherwise it can lead to compatibility issues.
            // Also note that IDEA does the same thing at startup, and not only for tests.
            systemProperty("jna.nosys", "true")

            // The factory should be set up automatically in `IdeaForkJoinWorkerThreadFactory.setupForkJoinCommonPool`,
            // but when tests are launched by Gradle this may not happen because Gradle can use the pool earlier.
            // Setting this factory is critical for `ReadMostlyRWLock` performance, so ensure it is properly set
            systemProperty(
                "java.util.concurrent.ForkJoinPool.common.threadFactory",
                "com.intellij.concurrency.IdeaForkJoinWorkerThreadFactory"
            )
            if (hasProp("excludeTests")) {
                exclude(prop("excludeTests"))
            }
        }
    }
}
