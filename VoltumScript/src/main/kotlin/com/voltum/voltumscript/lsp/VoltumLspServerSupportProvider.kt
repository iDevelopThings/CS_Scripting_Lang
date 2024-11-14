package com.voltum.voltumscript.lsp

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.LspServer
import com.intellij.platform.lsp.api.LspServerSupportProvider
import com.intellij.platform.lsp.api.lsWidget.LspServerWidgetItem
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.runtime.runtimeSettings

class VoltumLspServerSupportProvider : LspServerSupportProvider {
    override fun fileOpened(project: Project, file: VirtualFile, serverStarter: LspServerSupportProvider.LspServerStarter) {
        if (!runtimeSettings.isLspEnabled)
            return

        if (file.extension == Constants.FILE_EXTENSION) {
            serverStarter.ensureServerStarted(VoltumLspServerDescriptor(project))
        }
    }

    override fun createLspServerWidgetItem(lspServer: LspServer, currentFile: VirtualFile?): LspServerWidgetItem =
        VoltumLspServerWidgetItem(lspServer, currentFile)
//        LspServerWidgetItem(lspServer, currentFile, Icons.Logo)
}


