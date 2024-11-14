package com.voltum.voltumscript.lang.types

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.currentClassLogger
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.psi.impl.source.tree.LeafPsiElement
import com.voltum.voltumscript.ext.buildPrinter
import com.voltum.voltumscript.ext.elementUnderCaretInEditor
import com.voltum.voltumscript.ext.psiFile
import com.voltum.voltumscript.psi.VoltumFile
import com.voltum.voltumscript.psi.ext.inference
import com.voltum.voltumscript.psi.ext.prototype
import com.voltum.voltumscript.psi.ext.tryFoldType

class DumpTypeInformationAction : DumbAwareAction() {
    companion object {
        val log = logger<DumpTypeInformationAction>()
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val file = e.dataContext.psiFile as? VoltumFile ?: return
        var element = e.dataContext.elementUnderCaretInEditor ?: return
        if(element is LeafPsiElement)
            element = element.parent
        
        val text = buildPrinter {
            ln("Dumping Type Information for: ${element.text}")

            i {
                val inference = element.inference
                val proto = element.prototype

                val folded = element.tryFoldType(proto)
                
                if(folded == null) {
                    ln("Folded type is null, inference: $inference, proto: $proto")
                } else {
                    ln("Folded type: $folded")
                }
                
            }
            
        }

        currentClassLogger().warn(text)

        /*
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

        }*/

    }

}
