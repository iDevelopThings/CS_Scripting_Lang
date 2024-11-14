package com.voltum.voltumscript.lang.completion

import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.voltum.voltumscript.lang.VoltumProcessor
import com.voltum.voltumscript.psi.VoltumFunction


class FunctionDeclarationReferenceProcessor : VoltumProcessor<VoltumFunction> {
    
    constructor(name: String, fromElement: PsiElement) : super(name, fromElement)
    
    override fun execute(element: PsiElement, state: ResolveState): Boolean {
        if (element == fromElement || element !is VoltumFunction)
            return true

        if (element.name != name)
            return true

        addResult(element)

        return false
    }
}
