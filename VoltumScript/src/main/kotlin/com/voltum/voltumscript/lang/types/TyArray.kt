package com.voltum.voltumscript.lang.types

import com.intellij.psi.PsiElement
import com.voltum.voltumscript.lang.types.TyObject.Companion

class TyArray : TyValue {
    override var name: String = "array"

    constructor()
    constructor(el: PsiElement) : super(el)

    companion object : TyCompanion<TyArray> {
        override val INSTANCE = TyArray().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyArray() else TyArray(it) })
    }
}