package com.voltum.voltumscript.lang.completion

import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.voltum.voltumscript.lang.VoltumProcessor
import com.voltum.voltumscript.psi.VoltumNamedElement
import com.voltum.voltumscript.psi.VoltumVariableDeclaration

class VariableReferenceProcessor : VoltumProcessor<VoltumNamedElement> {
    constructor(name: String, fromElement: PsiElement) : super(name, fromElement)
    
    override fun execute(element: PsiElement, state: ResolveState): Boolean {        
        if (element == fromElement || element !is VoltumVariableDeclaration)
            return true

        val varId = element.varIdList.find { it.name == name }
        if(varId != null) {
            addResult(varId)
            return false
        }

        return true
    }
}

