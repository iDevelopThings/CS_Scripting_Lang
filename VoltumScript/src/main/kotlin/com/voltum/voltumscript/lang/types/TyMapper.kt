@file:Suppress("UNCHECKED_CAST")

package com.voltum.voltumscript.lang.types

import com.intellij.psi.PsiElement
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.psi.VoltumTypes
import java.lang.reflect.Field
import kotlin.reflect.KClass
import kotlin.reflect.full.companionObject
import kotlin.reflect.full.companionObjectInstance

enum class TyKind(
    val classType: KClass<out Ty>,
    val psiElementType: List<IElementType>,
    val constructable: Boolean,
) {
    Unknown(TyUnknown::class, listOf(), false),
    Null(TyNull::class, listOf(VoltumTypes.LITERAL_NULL), false),
    Unit(TyUnit::class, listOf(), false),
    Bool(TyBool::class, listOf(VoltumTypes.LITERAL_BOOL), false),
    String(TyString::class, listOf(VoltumTypes.LITERAL_STRING), false),
    Array(TyArray::class, listOf(VoltumTypes.LIST_VALUE), true),
    Int32(TyInt32::class, listOf(VoltumTypes.LITERAL_INT), false),
    Int64(TyInt64::class, listOf(VoltumTypes.LITERAL_INT), false),
    Double(TyDouble::class, listOf(VoltumTypes.LITERAL_FLOAT), false),
    Float(TyFloat::class, listOf(VoltumTypes.LITERAL_FLOAT), false),
    Object(TyObject::class, listOf(VoltumTypes.DICTIONARY_VALUE), true),
    Function(TyFunction::class, listOf(VoltumTypes.FUNC_DECLARATION), true),

    //    Dictionary(TyDictionary::class, listOf(VoltumTypes.DICTIONARY_EXPR), true),
    Struct(TyStruct::class, listOf(VoltumTypes.TYPE_DECLARATION), true),
    ;

    val companionObject: TyCompanion<*>? by lazy {
        classType.companionObjectInstance as? TyCompanion<*> ?: return@lazy null
    }
    /* val instanceField: Field? by lazy {
         classType.java.getDeclaredField("INSTANCE").apply {
             isAccessible = true
         }
     }
     val companionObjectValue get() = instanceField?.get(classType.companionObjectInstance) as Ty?
 
     val typeConstructorField: Field? by lazy {
         classType.java.getDeclaredField("typeConstructor").apply {
             isAccessible = true
         }
     }*/

    //    val typeConstructor: TypeConstructor? get() = typeConstructorField?.get(classType.companionObjectInstance) as TypeConstructor?
    val typeConstructor: TypeConstructor?
        get() = companionObject?.typeConstructor

    fun <T : Ty> createInstance(el: PsiElement? = null, namePrefix: kotlin.String? = null): T? {
        if (!constructable) {
            // get the `INSTANCE` function from the companion object of the class
            //val field = classType.java.getDeclaredField("INSTANCE")
            //field.isAccessible = true
            //val result =  field.get(classType.companionObjectInstance) as T?
            //return result
            return companionObject?.INSTANCE as? T
        }

        val inst = typeConstructor?.create(el) as T?
        if (namePrefix != null && inst != null) {
            inst.name = namePrefix + inst.name
        }

        /*classType.java.getDeclaredField("typeConstructor")

        var inst = when (el) {
            null -> classType.java.getDeclaredConstructor().newInstance() as T?
            else -> classType.java.getDeclaredConstructor(PsiElement::class.java).newInstance(el) as T?
        }
        
        if (namePrefix != null && inst != null) {
            inst.name = namePrefix + inst.name
        }*/

        return inst
    }

    companion object {
        fun fromClass(clazz: KClass<out Ty>): TyKind {
            return entries.firstOrNull { it.classType == clazz } ?: Unknown
        }

        fun findByName(name: kotlin.String): TyKind {
            return entries.firstOrNull {
                it.name.equals(name, ignoreCase = true)
            } ?: Unknown
        }

        fun findType(element: PsiElement): TyKind {
            return entries.firstOrNull {
                if (it.psiElementType.contains(element.node.elementType)) {
                    return@firstOrNull true
                }

                element.node.firstChildNode?.let { first ->
                    if (it.psiElementType.contains(first.elementType)) {
                        return@firstOrNull true
                    }
                }

                return@firstOrNull false
            } ?: Unknown
        }
    }
}

class TyReference : Ty {
    enum class ReferenceType {
        FromKind,
        FromTy,
    }

    override var name: String
    var type: ReferenceType = ReferenceType.FromKind
    var referenceToKind: TyKind? = null
    var referenceTo: Ty? = null

    constructor(referenceToKind: TyKind, name: String = "reference") : super() {
        this.type = ReferenceType.FromKind
        this.referenceToKind = referenceToKind
        this.name = name
    }

    constructor(ty: Ty?, name: String = "reference(resolved)") : super() {
        this.type = ReferenceType.FromTy
        this.referenceTo = ty
        this.name = name
    }

    override fun toString(): String {
        return "${this::class.simpleName}(id=$id, name=$name, flags=${PrototypeFlag.dump(flags)})"
    }

    fun resolve(element: PsiElement? = null): Ty? {
        if (type == ReferenceType.FromTy) {
            return referenceTo
        }
        if(type == ReferenceType.FromKind) {
            return referenceToKind?.createInstance<Ty>(element, "(type reference:${referenceToKind?.name}) -> ")
        }
        
        throw IllegalStateException("Unknown reference type")
    }
}

//class TyUnknown : Ty() {
//class TyNull : TyPrimitive() {
//class TyUnit : TyPrimitive() {
//class TyBool : TyPrimitive() {
//class TyString : TyPrimitive() {
//class TyArray : TyValue() {
//class TyInt32 : TyNumber() {
//class TyInt64 : TyNumber() {
//class TyDouble : TyNumber() {
//class TyFloat : TyNumber() {
//class TyObject : TyPrimitive {
//class TyFunction: TyObject {
//class TyDictionary: TyObject {
//class TyStruct : TyObject {