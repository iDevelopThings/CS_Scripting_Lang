package com.voltum.voltumscript.lang.types

import com.intellij.psi.PsiElement
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.psi.VoltumLiteralFloat
import com.voltum.voltumscript.psi.VoltumLiteralInt
import com.voltum.voltumscript.psi.VoltumTypes

open class TyNumber : TyPrimitive {
    override var name: String = "number"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_INT

    constructor()
    constructor(el: PsiElement) : super(el)

    companion object : TyCompanion<TyNumber> {
        override val INSTANCE = TyNumber().apply { setAsDefault(true) }
        override val typeConstructor
            get() = DefaultTypeCtor(this, {
                if (it is VoltumLiteralInt) {
                    if (it.valueInteger.text?.toIntOrNull() != null)
                        return@DefaultTypeCtor TyInt32.typeConstructor.createTyped(it)                    
                    if (it.valueInteger.text?.toLongOrNull() != null)
                        return@DefaultTypeCtor TyInt64.typeConstructor.createTyped(it)
                }
                
                if (it is VoltumLiteralFloat) {
                    if (it.valueFloat.text?.toFloatOrNull() != null)
                        return@DefaultTypeCtor TyFloat.typeConstructor.createTyped(it)                    
                    if (it.valueFloat.text?.toDoubleOrNull() != null)
                        return@DefaultTypeCtor TyDouble.typeConstructor.createTyped(it)
                }

                if (it == null)
                    return@DefaultTypeCtor TyNumber()

                return@DefaultTypeCtor TyNumber(it)
            })


        val all get() = listOf<TyNumber>(TyInt32.INSTANCE, TyInt64.INSTANCE, TyDouble.INSTANCE, TyFloat.INSTANCE, TyNumber.INSTANCE)
        val default get() = TyInt32.INSTANCE
    }
}

class TyInt32 : TyNumber {
    override var name: String = "int32"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_INT

    constructor()
    constructor(el: PsiElement) : super(el)

    companion object : TyCompanion<TyInt32> {
        override val INSTANCE = TyInt32().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyInt32() else TyInt32(it) })
    }
}

class TyInt64 : TyNumber {
    override var name: String = "int64"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_INT

    constructor()
    constructor(el: PsiElement) : super(el)

    companion object : TyCompanion<TyInt64> {
        override val INSTANCE = TyInt64().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyInt64() else TyInt64(it) })
    }
}

class TyDouble : TyNumber {
    override var name: String = "double"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_FLOAT

    constructor()
    constructor(el: PsiElement) : super(el)

    companion object : TyCompanion<TyDouble> {
        override val INSTANCE = TyDouble().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyDouble() else TyDouble(it) })
    }
}

class TyFloat : TyNumber {
    override var name: String = "float"
    override fun psiElementKind(): IElementType? = VoltumTypes.LITERAL_FLOAT

    constructor()
    constructor(el: PsiElement) : super(el)

    companion object : TyCompanion<TyFloat> {
        override val INSTANCE = TyFloat().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyFloat() else TyFloat(it) })
    }
}
