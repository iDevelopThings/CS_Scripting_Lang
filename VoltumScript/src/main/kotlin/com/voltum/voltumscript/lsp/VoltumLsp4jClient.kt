package com.voltum.voltumscript.lsp

import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.project.Project
import com.intellij.platform.lsp.api.Lsp4jClient
import com.intellij.platform.lsp.api.LspServerNotificationsHandler
import org.eclipse.lsp4j.*
import org.eclipse.lsp4j.services.LanguageClient
import java.util.concurrent.CompletableFuture

class VoltumLspServerNotificationHandlerProxy(
    private val serverNotificationsHandler: LspServerNotificationsHandler,
    private val project: Project
) : LspServerNotificationsHandler {

    override fun applyEdit(params: ApplyWorkspaceEditParams) = serverNotificationsHandler.applyEdit(params)
    override fun registerCapability(params: RegistrationParams) = serverNotificationsHandler.registerCapability(params)
    override fun unregisterCapability(params: UnregistrationParams) = serverNotificationsHandler.unregisterCapability(params)
    override fun telemetryEvent(`object`: Any) = serverNotificationsHandler.telemetryEvent(`object`)
    override fun publishDiagnostics(params: PublishDiagnosticsParams) = serverNotificationsHandler.publishDiagnostics(params)
    override fun showMessage(params: MessageParams) = serverNotificationsHandler.showMessage(params)
    override fun showMessageRequest(params: ShowMessageRequestParams) = serverNotificationsHandler.showMessageRequest(params)
    override fun showDocument(params: ShowDocumentParams) = serverNotificationsHandler.showDocument(params)
    override fun logMessage(params: MessageParams) {
//        serverNotificationsHandler.logMessage(params)

        LspConsoleViewFactory.getConsole(project)?.print(params)
    }

    override fun workspaceFolders() = serverNotificationsHandler.workspaceFolders()
    override fun configuration(params: ConfigurationParams) = serverNotificationsHandler.configuration(params)
    override fun createProgress(params: WorkDoneProgressCreateParams) = serverNotificationsHandler.createProgress(params)
    override fun notifyProgress(params: ProgressParams) = serverNotificationsHandler.notifyProgress(params)
    override fun logTrace(params: LogTraceParams) = serverNotificationsHandler.logTrace(params)
    override fun refreshSemanticTokens() = serverNotificationsHandler.refreshSemanticTokens()
    override fun refreshCodeLenses() = serverNotificationsHandler.refreshCodeLenses()
    override fun refreshInlayHints() = serverNotificationsHandler.refreshInlayHints()
    override fun refreshInlineValues() = serverNotificationsHandler.refreshInlineValues()
    override fun refreshDiagnostics() = serverNotificationsHandler.refreshDiagnostics()

}

class VoltumLsp4jClient(
    private val serverNotificationsHandler: LspServerNotificationsHandler,
    private val project: Project
) : Lsp4jClient(
    VoltumLspServerNotificationHandlerProxy(serverNotificationsHandler, project)
), LanguageClient 