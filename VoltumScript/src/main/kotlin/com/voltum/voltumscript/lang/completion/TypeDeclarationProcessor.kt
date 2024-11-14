package com.voltum.voltumscript.lang.completion

import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.voltum.voltumscript.lang.VoltumProcessor
import com.voltum.voltumscript.psi.VoltumTypeDeclaration

class TypeDeclarationProcessor : VoltumProcessor<VoltumTypeDeclaration> {
    constructor(name: String, fromElement: PsiElement) : super(name, fromElement)

    override fun execute(element: PsiElement, state: ResolveState): Boolean {
        if (element == fromElement || element !is VoltumTypeDeclaration)
            return true
        
        if(element.name == name) {
            addResult(element)
            return false
        }

        return true
    }
}