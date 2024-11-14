package com.voltum.voltumscript.lang.types

import com.intellij.psi.PsiElement
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.parser.VoltumTokenTypes
import com.voltum.voltumscript.psi.VoltumTypes

abstract class TyPrimitive : TyValue {
    open fun psiElementKind(): IElementType? = null

    constructor()
    constructor(el: PsiElement) : super(el)
}

class TyBool : TyPrimitive() {
    override var name: String = "bool"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_BOOL

    companion object : TyCompanion<TyBool> {
        override val INSTANCE = TyBool().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { TyBool() })
    }
}

class TyString : TyPrimitive() {
    override var name: String = "string"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_STRING

    companion object : TyCompanion<TyString> {
        override val INSTANCE = TyString().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { TyString() })
    }
}

class TyNull : TyPrimitive() {
    override var name: String = "null"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_NULL

    companion object : TyCompanion<TyNull> {
        override val INSTANCE = TyNull().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { TyNull() })
    }
}

class TyUnit : TyPrimitive() {
    override var name: String = "unit"
    override fun psiElementKind(): IElementType? = null

    companion object : TyCompanion<TyUnit> {
        override val INSTANCE = TyUnit().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { TyUnit() })
    }
}

