package com.voltum.voltumscript.psi

import com.intellij.psi.PsiElement
import com.intellij.psi.PsiReference
import com.intellij.psi.ResolveState
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.psi.tree.IElementType

interface VoltumBaseStatement : VoltumElement {

}

abstract class VoltumBaseStatementImpl : VoltumElementImpl, VoltumBaseStatement {

    constructor(type: IElementType) : super(type)

  /*  override fun processDeclarations(processor: PsiScopeProcessor, state: ResolveState, lastParent: PsiElement?, place: PsiElement): Boolean {
        

        return true
    }*/

    override fun getReferences(): Array<PsiReference> {
        
        return super.getReferences()
    }

}