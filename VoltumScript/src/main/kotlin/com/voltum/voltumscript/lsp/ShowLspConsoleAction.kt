package com.voltum.voltumscript.lsp

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.platform.lsp.api.LspServer
import com.voltum.voltumscript.VoltumBundle

class ShowLspConsoleAction(
    private val lspServer: LspServer,
) : AnAction(VoltumBundle.message("lsp.action.ShowConsoleAction.text"), null, AllIcons.Debugger.Console), DumbAware {
    override fun actionPerformed(e: AnActionEvent) {
        // get the tool window with id `VoltumLSP`
        val consoleWindow = LspConsoleViewFactory.getWindow(e.project!!)
        val consoleView = LspConsoleViewFactory.getConsole(e.project!!)
        
        consoleWindow?.show()
    }
}