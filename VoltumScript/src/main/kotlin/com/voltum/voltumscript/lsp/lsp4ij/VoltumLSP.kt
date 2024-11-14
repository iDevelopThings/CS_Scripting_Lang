package com.voltum.voltumscript.lsp.lsp4ij

import com.intellij.openapi.Disposable
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.project.Project
import com.redhat.devtools.lsp4ij.LanguageServerItem
import com.redhat.devtools.lsp4ij.LanguageServerManager
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.runtime.RuntimeManager
import java.util.concurrent.CompletableFuture

val Project.lspManager get() = service<LanguageServerManager>()
val Project.voltumLsp get() = service<VoltumLSP>()

@Service(Service.Level.PROJECT)
class VoltumLSP(val project: Project) : Disposable {
    companion object {
        val log = logger<RuntimeManager>()
    }

    override fun dispose() {
    }

    fun getLspItem() = project.lspManager.getLanguageServer(Constants.LSP_LANGUAGE_ID)
    fun getLsp() = getLspItem().thenApply{ 
        if(it != null) {
            return@thenApply it.server
        }
        return@thenApply null
    }
    fun stop() = project.lspManager.stop(Constants.LSP_LANGUAGE_ID)
    fun start() = project.lspManager.start(Constants.LSP_LANGUAGE_ID)
    
}