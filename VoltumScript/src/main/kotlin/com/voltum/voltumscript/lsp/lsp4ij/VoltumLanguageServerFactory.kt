package com.voltum.voltumscript.lsp.lsp4ij

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.redhat.devtools.lsp4ij.AbstractDocumentMatcher
import com.redhat.devtools.lsp4ij.LanguageServerFactory
import com.redhat.devtools.lsp4ij.client.LanguageClientImpl
import com.redhat.devtools.lsp4ij.client.features.LSPClientFeatures
import com.redhat.devtools.lsp4ij.client.features.LSPDiagnosticFeature
import com.redhat.devtools.lsp4ij.client.features.LSPSemanticTokensFeature
import com.redhat.devtools.lsp4ij.server.OSProcessStreamConnectionProvider
import com.redhat.devtools.lsp4ij.server.StreamConnectionProvider
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.runtime.runtimeSettings
import org.eclipse.lsp4j.services.LanguageServer


class VoltumLanguageServerFactory : LanguageServerFactory {
    override fun createConnectionProvider(p0: Project): StreamConnectionProvider {
        return VoltumStreamConnectionProvider()
    }

    override fun createLanguageClient(project: Project): LanguageClientImpl {
        return VoltumLanguageClient(project)
    }

    override fun getServerInterface(): Class<out LanguageServer> {
        return VoltumLanguageServer::class.java
    }

    override fun createClientFeatures(): LSPClientFeatures {
        return LSPClientFeatures()
            .setSemanticTokensFeature(LSPSemanticTokensFeature())
            .setDiagnosticFeature(LSPDiagnosticFeature().apply { 
                
            })
    }
/*
    override fun isEnabled(project: Project): Boolean = runtimeSettings.isLspEnabled

    override fun setEnabled(enabled: Boolean, project: Project) {
        runtimeSettings.isLspEnabled = enabled
        thisLogger().warn("Setting LSP enabled -> $enabled")
    }*/


}

class VoltumStreamConnectionProvider : OSProcessStreamConnectionProvider() {
    init {
        val commandLine = runtimeSettings.createLspCommandLine()
        super.setCommandLine(commandLine)
    }
}

class VoltumLanguageClient(val p: Project) : LanguageClientImpl(p) {
    
}

interface VoltumLanguageServer : LanguageServer

class VoltumLspDocumentMatcher : AbstractDocumentMatcher() {
    override fun match(vf: VirtualFile, project: Project): Boolean {
        return vf.extension == Constants.FILE_EXTENSION
    }
}