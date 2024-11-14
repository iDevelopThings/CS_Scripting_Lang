package com.voltum.voltumscript.lsp

import com.intellij.codeInsight.intention.IntentionAction
import com.intellij.execution.process.OSProcessHandler
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessListener
import com.intellij.execution.process.ProcessOutputTypes
import com.intellij.lang.annotation.AnnotationHolder
import com.intellij.openapi.Disposable
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.TextRange
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.*
import com.intellij.platform.lsp.api.customization.LspDiagnosticsSupport
import com.intellij.platform.lsp.api.customization.LspSemanticTokensSupport
import com.intellij.util.io.BaseOutputReader
import com.intellij.util.messages.MessageBusConnection
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.runtime.VOLTUM_LSP_REBUILT_TOPIC
import com.voltum.voltumscript.runtime.VoltumLspRebuiltListener
import com.voltum.voltumscript.runtime.runtime
import com.voltum.voltumscript.runtime.runtimeSettings
import org.eclipse.lsp4j.*

class VoltumLspServerDescriptor(
    project: Project,
    private val messageBus: MessageBusConnection = project.messageBus.connect()
) : ProjectWideLspServerDescriptor(project, "Voltum"), Disposable {
    val log = logger<VoltumLspServerDescriptor>()
    
    override val lspSemanticTokensSupport = LspSemanticTokensSupport()
    override val lspDiagnosticsSupport: LspDiagnosticsSupport = object : LspDiagnosticsSupport() {
        override fun createAnnotation(holder: AnnotationHolder, diagnostic: Diagnostic, textRange: TextRange, quickFixes: List<IntentionAction>) {
//            LspConsoleViewFactory.getConsole(project)?.printStdErr("Diagnostic: ${diagnostic.message}\n")
            super.createAnnotation(holder, diagnostic, textRange, quickFixes)
        }
    }

    override val clientCapabilities: ClientCapabilities
        get() {
            val capabilities = super.clientCapabilities
            
            capabilities.workspace.symbol = SymbolCapabilities().apply { 
                resolveSupport = WorkspaceSymbolResolveSupportCapabilities()
                symbolKind = SymbolKindCapabilities().apply {
                    valueSet = SymbolKind.entries
                }
            }
            
            return capabilities
        }
    
    override fun createLsp4jClient(handler: LspServerNotificationsHandler): Lsp4jClient =
        VoltumLsp4jClient(handler, project)

    override fun isSupportedFile(file: VirtualFile) = file.extension == Constants.FILE_EXTENSION
    override fun createCommandLine() = runtimeSettings.createLspCommandLine()
    override fun getLanguageId(file: VirtualFile): String {
        if (file.extension == Constants.FILE_EXTENSION) {
            return Constants.LSP_LANGUAGE_ID
        }
        return super.getLanguageId(file)
    }

    override fun startServerProcess(): OSProcessHandler {
        val startingCommandLine = createCommandLine().withCharset(Charsets.UTF_8)
        LOG.info("$this: starting LSP server: $startingCommandLine")
        var handler = object : OSProcessHandler(startingCommandLine) {
            override fun readerOptions(): BaseOutputReader.Options = BaseOutputReader.Options.forMostlySilentProcess()
        }
        
        handler.addProcessListener(object : ProcessListener {
            override fun onTextAvailable(event: ProcessEvent, outputType: Key<*>) {
                super.onTextAvailable(event, outputType)

                if (outputType === ProcessOutputTypes.STDERR) {
                    LspConsoleViewFactory.getConsole(project)?.printStdErr(event.text)
                }
            }
        })
        
        return handler
    }

    override val lspServerListener = object : LspServerListener {
        override fun serverInitialized(params: InitializeResult) {
            super.serverInitialized(params)
            
            project.runtime.startListener()

            messageBus.subscribe(VOLTUM_LSP_REBUILT_TOPIC, object : VoltumLspRebuiltListener {
                override fun lspRebuilt() {
                    LspServerManager.getInstance(project)
                        .stopAndRestartIfNeeded(VoltumLspServerSupportProvider::class.java)
                }
            })

        }

    }

    override fun dispose() {
        messageBus.dispose()
    }
}