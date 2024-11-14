package com.voltum.voltumscript.lsp

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.options.Configurable
import com.intellij.openapi.progress.runBlockingCancellable
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.ide.progress.runWithModalProgressBlocking
import com.intellij.platform.lang.lsWidget.LanguageServicePopupSection
import com.intellij.platform.lang.lsWidget.OpenSettingsAction
import com.intellij.platform.lsp.api.LspServer
import com.intellij.platform.lsp.api.LspServerManager
import com.intellij.platform.lsp.api.LspServerState.*
import com.intellij.platform.lsp.api.lsWidget.LspServerWidgetItem
import com.voltum.voltumscript.Icons
import kotlinx.coroutines.delay

class VoltumLspServerWidgetItem : LspServerWidgetItem {
    private var settingsPageClass: Class<out Configurable>? = null

    constructor(
        lspServer: LspServer,
        currentFile: VirtualFile?,
        settingsPageClass: Class<out Configurable>? = null
    ) : super(lspServer, currentFile, Icons.Logo, settingsPageClass) {
        this.settingsPageClass = settingsPageClass
    }


    override fun createWidgetMainAction(): AnAction =
        settingsPageClass?.let {
            OpenSettingsAction(it, widgetActionText, statusBarIcon)
        }
            ?: object : AnAction(widgetActionText, null, statusBarIcon) {
                override fun actionPerformed(e: AnActionEvent) {
                    showLspConsoleView()
                }
            }

    fun showLspConsoleView() {
        val consoleWindow = LspConsoleViewFactory.getWindow(lspServer.project)
        consoleWindow?.show()
    }

    override fun createAdditionalInlineActions(): List<AnAction> {
        val actions = mutableListOf<AnAction>()
        actions.addAll(super.createAdditionalInlineActions())
        if (widgetActionLocation == LanguageServicePopupSection.ForCurrentFile) {
            when (lspServer.state) {
                Initializing, Running -> actions.add(StopLspServerAction(lspServer))
                ShutdownNormally      -> Unit // do nothing
                ShutdownUnexpectedly  -> Unit
            }
        }
        actions.add(ShowLspConsoleAction(lspServer))
        return actions
    }
}

private class StopLspServerAction(
    private val lspServer: LspServer,
) : AnAction("Temp Stop LSP", null, AllIcons.Actions.StopRefresh), DumbAware {
    override fun actionPerformed(e: AnActionEvent) {
        val manager = LspServerManager.getInstance(lspServer.project)

        manager.stopServers(VoltumLspServerSupportProvider::class.java)
        thisLogger().warn("Stopped LSP server")
        runWithModalProgressBlocking(lspServer.project, "Starting LSP server") {
            thisLogger().warn("Waiting for 1 second")
            delay(1000)
            manager.startServersIfNeeded(VoltumLspServerSupportProvider::class.java)
            thisLogger().warn("Started LSP server")
        }
    }
}