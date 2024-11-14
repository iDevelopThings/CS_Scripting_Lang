package com.voltum.voltumscript.lsp

import com.intellij.execution.impl.ConsoleViewImpl
import com.intellij.execution.process.ProcessOutputTypes
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.platform.lsp.api.*
import com.intellij.ui.JBColor
import com.intellij.util.ui.JBUI
import com.voltum.voltumscript.ext.JsonUtils.tryParseJsonObject
import org.eclipse.lsp4j.MessageParams
import org.eclipse.lsp4j.MessageType
import java.awt.BorderLayout
import javax.swing.BorderFactory
import javax.swing.JComponent

class LspConsoleView : ConsoleViewImpl {
    var categoryEnabled = false

    constructor(project: Project, viewer: Boolean) : super(project, viewer) {
        LspServerManager.getInstance(project).addLspServerManagerListener(
            object : LspServerManagerListener {
                override fun serverStateChanged(lspServer: LspServer) {
                    if (lspServer.providerClass != VoltumLspServerSupportProvider::class.java) {
                        return
                    }
                    log.warn("LSP Server state changed to ${lspServer.state}")
                    if (lspServer.state == LspServerState.Initializing) {
                        clear()
                    }
                    if (lspServer.state == LspServerState.ShutdownUnexpectedly) {
                        LspServerManager.getInstance(lspServer.project).startServersIfNeeded(VoltumLspServerSupportProvider::class.java)
                    }
                }

                override fun fileOpened(lspServer: LspServer, file: VirtualFile) {}
                override fun diagnosticsReceived(lspServer: LspServer, file: VirtualFile) {}
            },
            this,
            true
        )

        addCustomConsoleAction(object : AnAction("Toggle Category") {
            override fun actionPerformed(e: AnActionEvent) {
                categoryEnabled = !categoryEnabled
                log.warn("Category enabled: $categoryEnabled")
            }
        })
    }

    companion object {
        val log = logger<LspConsoleView>()
        val EXPIRED_ENTRY = ConsoleViewContentType("LOG_EXPIRED_ENTRY", ConsoleViewContentType.LOG_EXPIRED_ENTRY)
    }

    fun printStdErr(text: String) {
        val decoder = VoltumAnsiEscapeDecoder()
        decoder.escapeText(text, ProcessOutputTypes.STDERR) { chunk: String?, attributes: Key<*>? ->
            print(chunk!!, ConsoleViewContentType.getConsoleViewType(attributes!!))
        }
    }

    fun print(params: MessageParams) {
        val decoder = VoltumAnsiEscapeDecoder()

        val messageType = when (params.type) {
            MessageType.Log     -> ConsoleViewContentType.NORMAL_OUTPUT
            MessageType.Info    -> ConsoleViewContentType.LOG_INFO_OUTPUT
            MessageType.Warning -> ConsoleViewContentType.LOG_WARNING_OUTPUT
            MessageType.Error   -> ConsoleViewContentType.LOG_ERROR_OUTPUT
            else                -> ConsoleViewContentType.SYSTEM_OUTPUT
        }

        val jsonMessage = tryParseJsonObject<LspMessageJson>(params.message)

        print(params.type.toString() + ": ", messageType)

        decoder.escapeText(
            jsonMessage?.Timestamp?.let { "$it " } ?: "",
            ProcessOutputTypes.STDOUT
        ) { chunk: String?, attributes: Key<*>? ->
            print(chunk!!, EXPIRED_ENTRY)
        }

        if (categoryEnabled) {
            decoder.escapeText(
                jsonMessage?.Category?.trimStart('\"')?.trimEnd('\"')?.let { "$it " } ?: "",
                ProcessOutputTypes.STDOUT
            ) { chunk: String?, attributes: Key<*>? ->
                print(chunk!!, EXPIRED_ENTRY)
            }
        }
        decoder.escapeText(
            (jsonMessage?.FormattedMessage ?: "") + "\n",
            ProcessOutputTypes.STDOUT
        ) { chunk: String?, attributes: Key<*>? ->
            // Try to detect absolute file paths and make them clickable
            /*val matcher = Regex("([A-Za-z]:)?[\\/\\\\][^\\s]+").find(chunk!!)
            if (matcher != null) {
                val path = matcher.value
                val start = matcher.range.first
                val end = matcher.range.last + 1
                val file = path.replace("\\", "/")
                val url = "file://$file"
                val clickable = chunk.substring(0, start) + url + chunk.substring(end)
                print(clickable, messageType)
            } else {
                print(chunk!!, messageType)
            }*/
            print(chunk!!, messageType/*ConsoleViewContentType.getConsoleViewType(attributes!!)*/)
        }
        
        if(jsonMessage?.Exception != null) {
           decoder.escapeText(
               jsonMessage.Exception.toString() + "\n",
               ProcessOutputTypes.STDOUT
           ) { chunk: String?, attributes: Key<*>? ->
               print(chunk!!, ConsoleViewContentType.getConsoleViewType(attributes!!))
           }
        }

        // print(jsonMessage?.Timestamp?.let { it + " " } ?: "", EXPIRED_ENTRY)
        // print(jsonMessage?.Category?.trimStart('\"')?.trimEnd('\"')?.let { "$it " } ?: "", EXPIRED_ENTRY)
        // print(jsonMessage?.FormattedMessage ?: "", messageType)
        // print("\n", ConsoleViewContentType.LOG_VERBOSE_OUTPUT)

    }


}

data class LspMessageJson(
    val Exception: Any? = null,
    val FormattedMessage: String = "",
    val Category: String = "",
    val Timestamp: String = "",
    val Message: LspMessageContent
)

data class LspMessageContent(
    val Level: String = "",
    val MessageTemplate: String = "",
    val Properties: MutableMap<String, Any> = mutableMapOf(),
    val Timestamp: String = ""
)

class LspConsoleViewFactory : DumbAware, ToolWindowFactory {
    var consoleView: LspConsoleView? = null

    /*override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val manager = toolWindow.contentManager

        consoleView = LspConsoleView(project, true)
        
        val consoleContent = manager.factory.createContent(consoleView!!.component, "LSP Output", false)
        manager.addContent(consoleContent)

    }*/
    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val manager = toolWindow.contentManager

        val container = JBUI.Panels.simplePanel()
        manager.addContent(manager.factory.createContent(container, "LSP Output", false))

        consoleView = LspConsoleView(project, true)

        // must call getComponent before createConsoleActions()
        val consoleViewComponent: JComponent = consoleView!!.getComponent()

        val actionGroup = DefaultActionGroup()
        actionGroup.addAll(*consoleView!!.createConsoleActions())

        val toolbar = ActionManager.getInstance().createActionToolbar("console_toolbar", actionGroup, false)
        toolbar.targetComponent = consoleViewComponent


        container.add(consoleViewComponent, BorderLayout.CENTER)
        container.add(toolbar.component, BorderLayout.WEST)

        // Add a border to make things look nicer.
        consoleViewComponent.border = JBUI.Borders.customLineLeft(JBUI.CurrentTheme.ToolWindow.borderColor())

//        val consoleContent = manager.factory.createContent(container, "LSP Output", false)
//        manager.addContent(consoleContent)

    }

    companion object {
        const val TOOL_WINDOW_ID = "VoltumLSP"

        fun getWindow(project: Project) = ToolWindowManager.getInstance(project).getToolWindow(TOOL_WINDOW_ID)
        fun getConsole(project: Project): LspConsoleView? {
            val toolWindow = getWindow(project)
            val content = toolWindow?.contentManager?.getContent(0)

            val lspConsoleView = content?.component?.getComponent(0) as? LspConsoleView

            /*toolWindow?.contentManager?.contentsRecursively?.forEach {
                thisLogger().warn("Content: ${it.displayName}")
                it.component.components?.withIndex()?.forEach { (index, component) ->
                    thisLogger().warn("Component($index): ${component.name}->${component.javaClass.simpleName}")
                }
            }*/

            return lspConsoleView
        }
    }
}