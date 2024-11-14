package com.voltum.voltumscript.lang.types

import com.intellij.psi.PsiElement
import com.intellij.psi.stubs.StubInputStream
import com.intellij.psi.stubs.StubOutputStream
import com.intellij.psi.tree.IElementType
import com.intellij.psi.util.parentOfType
import com.voltum.voltumscript.ext.buildPrinter
import com.voltum.voltumscript.ext.typeParameterList
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.prototype
import com.voltum.voltumscript.runtime.std.types.TypeMeta

/**
 * The base for all "object" types.
 */
abstract class TyValue : Ty {
    constructor()
    constructor(el: PsiElement) : super(el)
}

open class TyObject : TyPrimitive {
    override var name: String = "object"
    override fun psiElementKind(): IElementType? = VoltumTypes.DICTIONARY_VALUE

    constructor()
    constructor(el: PsiElement) : super(el) {
        if (el is VoltumTypeDeclaration) {
            name = "object:${el.name}"
            addTypeDeclarationData(el)
        }
        if (el is VoltumDictionaryValue) {
            name = "object:dictionary"
            addDictionaryData(el)
        }
    }


    override fun toString(): String {
        return "${this::class.simpleName}(id=$id, name=$name, fields=${fields.count()}, methods=${methods.count()})"
    }

    companion object : TyCompanion<TyObject> {
        override val INSTANCE = TyObject().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyObject() else TyObject(it) })
    }
}

fun Ty.addTypeDeclarationData(el: VoltumTypeDeclaration) {
    el.fields.forEach {
        addField(it)
    }
    el.methods.forEach {
        addMethod(it)
    }
}

fun Ty.addDictionaryData(el: VoltumDictionaryValue) {
    el.dictionaryFieldList.forEach {
        if (it.value is VoltumAnonymousFunc) {
            addMethod(it.getKey(), TyNull.INSTANCE)
        } else {
            addField(it)
        }
    }
}

data class TyFunctionParam(
    val name: String,
    val type: Lazy<Ty?>
)

data class TyTypeParameter(val name: String)

class TyFunction : TyObject {
    override var name: String = "function"
    override fun psiElementKind(): IElementType? = VoltumTypes.FUNC_DECLARATION

    val params = mutableListOf<TyFunctionParam>()
    val typeParameters = mutableListOf<TyTypeParameter>()
    var returnType: Lazy<Ty?> = lazy { TyUnknown.INSTANCE }
    var returnTypeName: String = ""

    constructor()
    constructor(el: PsiElement) : super(el) {
        if (el is VoltumFuncDeclaration) {
            name = el.name

            el.getArguments().forEach {
                params.add(TyFunctionParam(it.nameIdentifier.text, lazy { it.type.tryResolveType() ?: TyUnknown.INSTANCE }))
            }
            el.typeArguments?.typeRefList?.forEach {
                typeParameters.add(TyTypeParameter(it.text))
            }

            val rtTypeRef = el.getReturnType()
            returnTypeName = rtTypeRef?.text ?: "void"
            returnType = lazy {
                var rt = rtTypeRef?.tryResolveType()
                if (rtTypeRef == null) {
                    rt = TyReference(TyKind.Unit)
                }
                rt
            }

            hasTypeParameter = el.typeArguments?.typeRefList?.isNotEmpty() ?: false
        }
    }

    override fun serialize(dataStream: StubOutputStream) {
        super.serialize(dataStream)
        dataStream.writeBoolean(hasTypeParameter)
        dataStream.writeInt(params.size)
        params.forEach {
            dataStream.writeName(it.name)
            serializeType(dataStream, it.type.value)
        }

        dataStream.writeInt(typeParameters.size)
        typeParameters.forEach {
            dataStream.writeName(it.name)
        }
    }

    override fun deserialize(dataStream: StubInputStream) {
        super.deserialize(dataStream)
        hasTypeParameter = dataStream.readBoolean()

        val paramCount = dataStream.readInt()
        repeat(paramCount) {
            val name = dataStream.readNameString()
            val type = Ty.deserialize(dataStream)
            params.add(TyFunctionParam(name!!, lazy { type }))
        }

        val typeParamCount = dataStream.readInt()
        repeat(typeParamCount) {
            val name = dataStream.readNameString()
            typeParameters.add(TyTypeParameter(name!!))
        }

    }

    override fun getCorrectFoldElement(el: PsiElement): PsiElement {
        if(el is VoltumCallExpr) {
            return el
        }
        return el.parentOfType<VoltumCallExpr>()!!
    }
    override fun substituteType(el: PsiElement): FoldedTypeResult? {
        if (el !is VoltumCallExpr)
            throw IllegalArgumentException("Expected VoltumCallExpr, got ${el::class.simpleName}")

        var returnTypeSubst = returnType.value
        val typeParams = mutableListOf<Ty>()
        el.typeArguments.withIndex()?.forEach {
            typeParams.add(it.value.prototype)
            if (returnTypeName == typeParameters[it.index].name) {
                returnTypeSubst = it.value.prototype
            }
        }
        
        val typeMap = typeParameters.zip(typeParams).toMap()

        return CallExprData(
            name = name,
            proto = this,
            typeParameters = typeParams,
            returnType = returnTypeSubst!!,
            typeMap = typeMap,
            element = el
        )
    }

    companion object : TyCompanion<TyFunction> {
        override val INSTANCE = TyFunction().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyFunction() else TyFunction(it) })
    }
}

interface FoldedTypeResult {
    val type: Ty
}
data class CallExprData(
    val name: String,
    val proto: TyFunction,
    val typeParameters: List<Ty>,
    val returnType: Ty,
    val typeMap: Map<TyTypeParameter, Ty>,
    val element: PsiElement
) : FoldedTypeResult {
    
    override val type: Ty
        get() = returnType
    
    override fun toString(): String {
        return buildPrinter { 
            a(name)
            typeParameterList(typeParameters)
            a("(")
            a("params not implemented yet")
            a(")")
            a(" -> ")
            a(returnType.name)
        }
    }
}

/*class TyDictionary : TyObject {
    override var name: String = "dictionary"
    override fun psiElementKind(): IElementType? = VoltumTypes.DICTIONARY_EXPR

    constructor()
    constructor(el: PsiElement) : super(el) {
        if (el is VoltumDictionaryValue) {
            el.dictionaryFieldList.forEach {
                if (it.value is VoltumAnonymousFunc) {
                    addMethod(it.getKey(), TyNull.INSTANCE)
                } else {
                    addField(it)
                }
            }
        }
    }

    companion object : TyCompanion<TyDictionary> {
        override val INSTANCE = TyDictionary().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyDictionary() else TyDictionary(it) })
    }
}*/

class TyStruct : TyObject {
    override var name: String = "struct"

    constructor()
    constructor(el: PsiElement) : super(el) {
        if (el is VoltumTypeDeclaration)
            name = "struct:${el.name}"
    }

    override fun toString(): String {
        return "${this::class.simpleName}(id=$id, name=$name, fields=${fields.count()}, methods=${methods.count()})"
    }

    companion object : TyCompanion<TyStruct> {
        override val INSTANCE = TyStruct().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeCtor(this, { if (it == null) TyStruct() else TyStruct(it) })
    }
}