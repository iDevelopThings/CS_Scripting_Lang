package com.voltum.voltumscript.runtime.std

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.project.DumbAwareAction
import com.voltum.voltumscript.ext.Printer
import com.voltum.voltumscript.lang.index.VoltumNamedElementIndex
import com.voltum.voltumscript.psi.ext.greenStub

class DumpLibMetaAction : DumbAwareAction() {
    companion object {
        val log = logger<DumpLibMetaAction>()
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        val meta = project.stdMeta

        val printer = Printer()
        meta.debugString(printer)
        val str = printer.toString()

        log.warn(str)
        log.warn("..")
        log.warn("Named Element Index Values:")

        VoltumNamedElementIndex.getAllKeyValues(project).forEach {
            val stub = it.second.greenStub()
            log.warn("  - ${it.first} -> $stub -> ${it.second}")           

        }

    }

}

class ReloadLibMeta : DumbAwareAction() {
    companion object {
        val log = logger<ReloadLibMeta>()
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.stdMeta.load()
    }

}
