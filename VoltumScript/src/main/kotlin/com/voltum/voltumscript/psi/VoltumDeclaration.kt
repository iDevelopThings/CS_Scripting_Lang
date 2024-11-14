package com.voltum.voltumscript.psi

import com.intellij.psi.PsiElement
import com.intellij.psi.util.parentOfType
import com.voltum.voltumscript.lang.references.VoltumReference


interface VoltumDeclaration : VoltumNamedElement {
    override fun getName(): String?
    override fun setName(name: String): PsiElement?
    override fun getNameId(): VoltumIdent? 
    override fun getNameIdentifier(): PsiElement?
    override fun getReferences(): Array<VoltumReference>
}

val VoltumTypeDeclarationMethodMember.isConstructor: Boolean
    get() = this.nameIdentifier.text == this.parentOfType<VoltumTypeDeclaration>()?.nameIdentifier?.text


fun PsiElement.isFunctionLike() = this is VoltumFunction || this is VoltumFuncId
fun PsiElement.isVariableLike() = this is VoltumVariableDeclaration || this is VoltumVarId
fun PsiElement.isTypeDeclarationLike() = this is VoltumTypeDeclaration || this is VoltumTypeId