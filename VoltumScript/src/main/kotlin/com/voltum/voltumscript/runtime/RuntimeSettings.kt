package com.voltum.voltumscript.runtime

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ScriptRunnerUtil
import com.intellij.openapi.components.*
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.util.xmlb.XmlSerializerUtil
import com.voltum.voltumscript.Constants
import java.nio.file.Path
import kotlin.io.path.Path
import kotlin.io.path.div

val runtimeSettings get() = service<RuntimeSettings>()

@State(name = "RuntimeSettings", storages = [Storage("voltum_settings.xml")], category = SettingsCategory.TOOLS)
class RuntimeSettings : PersistentStateComponent<RuntimeSettings> {

    var toolInstallationPath: String? = Constants.RUNTIME_PATH_NAME
    var isDefinedOnPath = true
    var isLspEnabled = true

    var useReleaseConfig = false

    override fun getState(): RuntimeSettings = this
    override fun loadState(state: RuntimeSettings) = XmlSerializerUtil.copyBean(state, this)

    fun checkValidToolPath(): Boolean {
        thisLogger().warn("Checking tool path -> ${this.toolInstallationPath}")
        if (this.isDefinedOnPath) {
            return ScriptRunnerUtil.isExecutableInPath(Constants.RUNTIME_PATH_NAME)
        }

        if (!this.toolInstallationPath.isNullOrEmpty()) {
            thisLogger().warn("Checking tool path #2 -> ${this.toolInstallationPath}")
            if (ScriptRunnerUtil.isExecutableInPath(this.toolInstallationPath!!)) {
                return true
            }

            thisLogger().warn("Checking actual tool path exists -> ${this.toolInstallationPath}")
            val file = java.io.File(this.toolInstallationPath!!)
            if (file.exists()) {
                return true
            }
        }

        return false
    }

    fun getCorrectPath(): String {
        thisLogger().warn("Checking correct tool path -> ${this.toolInstallationPath}")
        if (this.isDefinedOnPath) {
            return Constants.RUNTIME_PATH_NAME
        }

        if (!this.toolInstallationPath.isNullOrEmpty()) {
            if (ScriptRunnerUtil.isExecutableInPath(this.toolInstallationPath!!)) {
                return Constants.RUNTIME_PATH_NAME
            }
            val file = java.io.File(this.toolInstallationPath!!)
            if (file.exists()) {
                return file.absolutePath
            }
        }

        return Constants.RUNTIME_PATH_NAME
    }

    fun getToolProjectPath() = Path("F:\\c#\\CSScriptingLang\\")

    fun getBaseToolPath(lsp: Boolean = false): Path {
        var p = getToolProjectPath()
        if (lsp) {
            p /= "CSScriptingLang.LSP"
        } else {
            p /= "CSScriptingLang"
        }

        p /= "bin"
        
        if (useReleaseConfig) {
            p /= "Release"
        } else {
            p /= "Debug"
        }
        p /= "net8.0"

        return p
    }

    fun getLspPath() = getBaseToolPath(true) / "CSScriptingLang.LSP.exe"

    fun getMetaJsonPath() = getBaseToolPath() / "BindingsMeta.json"
    fun getStdLibPath() = getBaseToolPath() / "StdLibFiles"
    fun getMetaJson() = getMetaJsonPath().toFile().readText()

    fun createLspCommandLine() = GeneralCommandLine().apply {
        withExePath(getLspPath().toString())
        withParameters("--stdio")
    }

    companion object {
        @JvmStatic
        fun getInstance() = service<RuntimeSettings>()
    }

}