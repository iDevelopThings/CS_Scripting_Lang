@file:Suppress("UNUSED_PARAMETER")

package com.voltum.voltumscript.lang.types

import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.util.Key
import com.intellij.psi.PsiElement
import com.intellij.psi.stubs.StubInputStream
import com.intellij.psi.stubs.StubOutputStream
import com.intellij.psi.util.CachedValue
import com.intellij.psi.util.CachedValuesManager
import com.intellij.psi.util.elementType
import com.jetbrains.rd.util.AtomicInteger
import com.voltum.voltumscript.ext.*
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.createCachedResult
import com.voltum.voltumscript.runtime.std.types.TypeMeta
import java.lang.ref.SoftReference
import kotlin.properties.ReadWriteProperty
import kotlin.reflect.KProperty
import kotlin.reflect.full.primaryConstructor

val PROTOTYPE_TYPE_KEY: Key<CachedValue<Ty>> = Key.create("PROTOTYPE_TYPE_KEY")
val PROTOTYPE_VALUE_KEY: Key<SoftReference<Ty>> = Key.create("PROTOTYPE_VALUE_KEY")
val PROTOTYPE_FIELD_REF_KEY: Key<SoftReference<TyField>> = Key.create("PROTOTYPE_FIELD_REF_KEY")

typealias PrototypeFlags = Int

object PrototypeFlag : BitFlagInstanceBuilder<PrototypeFlags>(Limit.INT) {
    val IS_DEFAULT_PROTOTYPE = next("IS_DEFAULT_PROTOTYPE")
    val IS_PROTOTYPE_LOCKED = next("IS_PROTOTYPE_LOCKED")
    val HAS_TYPE_PARAMETER = next("HAS_TYPE_PARAMETER")
}

class TypeFlagsDelegate(val flagType: PrototypeFlags) : ReadWriteProperty<Ty, Boolean> {
    override fun getValue(thisRef: Ty, property: KProperty<*>): Boolean = thisRef.flags.isSet(flagType)
    override fun setValue(thisRef: Ty, property: KProperty<*>, value: Boolean) {
        thisRef.flags = thisRef.flags.setFlag(flagType, value)
    }
}

interface KindFlags {
    val flags: PrototypeFlags
}


fun PsiElement.tryResolveType(): Ty? {
//    if (this is VoltumLiteralExpr) {
//        return tryResolveType()
//    }
    val result = recursionGuard(this, {
        CachedValuesManager.getCachedValue(this, PROTOTYPE_TYPE_KEY) {
            val proto: Ty? = when (this) {
                is VoltumTypeDeclaration             -> TyKind.Struct.createInstance(this, this.name)
                is VoltumTypeDeclarationFieldMember  -> Ty.addOrGetField(this)?.ty
                is VoltumTypeDeclarationMethodMember -> Ty.addOrGetMethod(this)?.ty
                
                is VoltumFuncDeclaration             -> TyKind.Function.createInstance(this)

                is VoltumLiteralString               -> TyKind.String.createInstance(this)

                is VoltumLiteralInt,
                is VoltumLiteralFloat                -> TyNumber.typeConstructor.createTyped(this)

                is VoltumLiteralBool                 -> TyKind.Bool.createInstance(this)
                is VoltumLiteralNull                 -> TyKind.Null.createInstance(this)
                is VoltumDictionaryValue             -> TyKind.Object.createInstance(this)
                is VoltumListValue                   -> TyKind.Array.createInstance(this)

                is VoltumTypeRef                     -> this.tryResolveType()?.resolve(this)

                else                                 -> TyKind.Unknown.createInstance(this)
            }
            createCachedResult(proto)
        }
    }, false)
    return result
}

fun PsiElement.tryResolveTypeLazy(): Lazy<Ty?> = lazy {
    val result = tryResolveType()

    result
}


fun VoltumLiteralExpr.tryResolveType(): Ty? {
    if (firstChild is VoltumLiteralExpr) {
        return firstChild!!.tryResolveType()
    }

    if (firstChild is VoltumVarReference) {
        throw NotImplementedError("tryResolveType[VarReference] not implemented")
    }
    if (firstChild is VoltumIdentifier) {
        throw NotImplementedError("tryResolveType[Identifier] not implemented")
    }

    throw NotImplementedError("tryResolveType[${this.elementType} - ${this.firstChild?.elementType}] not implemented")
}

private var idCounter: AtomicInteger = AtomicInteger(0)

interface TyCompanion<T : Ty> {
    val INSTANCE: T
    val typeConstructor: TypeConstructorTyped<T>
}

interface TypeConstructor {
    fun create(el: PsiElement?): Ty?
}

interface TypeConstructorTyped<T : Ty> : TypeConstructor {
    override fun create(el: PsiElement?): Ty? = createTyped(el)
    fun createTyped(el: PsiElement?): T?
}

class DefaultTypeConstructor<T : Ty>(var ctorFn: (() -> T)? = null) : TypeConstructorTyped<T> {
    override fun createTyped(el: PsiElement?): T? = ctorFn?.invoke() ?: throw Exception("No constructor function provided")
}

class DefaultTypeCtor<T : Ty>(
    var companion: TyCompanion<T>,
    val ctorFn: (PsiElement?) -> T?
) : TypeConstructorTyped<T> {
    override fun createTyped(el: PsiElement?): T? {
        val inst = ctorFn.invoke(el) ?: throw Exception("No constructor function provided")
        val proto = companion.INSTANCE
        inst.addToPrototype(proto)
        return inst
    }
}

class TypeConstructorFromPrototype<T : Ty>(
    var proto: () -> T?,
    val ctorFn: (PsiElement?) -> T?
) : TypeConstructorTyped<T> {
    override fun createTyped(el: PsiElement?): T? {
        val inst = ctorFn.invoke(el) ?: throw Exception("No constructor function provided")
        val proto = proto.invoke() ?: throw Exception("No prototype provided")
        inst.addToPrototype(proto)
        return inst
    }
}


abstract class Ty : KindFlags {
    private var _name: String = ""
    open var name: String
        get() = _name
        set(value) {
            _name = value
        }
    
    var aliasNames = mutableListOf<String>()

    override var flags: PrototypeFlags = 0

    var linkedElement: PsiElement? = null
    var linkedToField: TyField? = null

    var isDefaultType: Boolean by TypeFlagsDelegate(PrototypeFlag.IS_DEFAULT_PROTOTYPE)
    var isTypeLocked: Boolean by TypeFlagsDelegate(PrototypeFlag.IS_PROTOTYPE_LOCKED)
    var hasTypeParameter: Boolean by TypeFlagsDelegate(PrototypeFlag.HAS_TYPE_PARAMETER)

    var prototype: Ty? = null

    var id: Int = 0
    val kind: TyKind get() = TyKind.fromClass(this::class)

    val members: MutableMap<String, TyField> = mutableMapOf()

    val fields get() = members.values.filter { it.kind == TyFieldKind.FIELD }
    val methods get() = members.values.filter { it.kind == TyFieldKind.METHOD }

    constructor() {
        id = idCounter.getAndIncrement()
    }

    constructor(el: PsiElement) {
        id = idCounter.getAndIncrement()
        linkedElement = el
        cacheReference(el)
    }

    protected fun cacheReference(el: PsiElement) {
        el.putUserData(PROTOTYPE_VALUE_KEY, SoftReference(this))

        logger.debug("[${this}] - Caching reference for el ${el}")

        if (el is VoltumLiteralExpr && el.parent is VoltumLiteralExpr) {
            el.parent.putUserData(PROTOTYPE_VALUE_KEY, SoftReference(this))
            logger.debug("[${this}] - Caching reference for el ${el.parent}")
        }
    }

    open fun serialize(dataStream: StubOutputStream) {
        dataStream.writeEnum(kind)
        dataStream.writeInt(id)
        dataStream.writeUTFFast(name)
        dataStream.writeInt(flags)

//        dataStream.writeMap(
//            fields,
//            { dataStream.writeUTF(it) },
//            { it.serialize(dataStream) }
//        )
    }

    open fun deserialize(dataStream: StubInputStream) {

    }

    override fun toString(): String {
        return "${this::class.simpleName}(id=$id, name=$name, flags=${PrototypeFlag.dump(flags)})"
    }

    open fun configure(meta: TypeMeta, el: PsiElement) {
        modifyUnlocked {
            name = meta.name

            if (meta.superTypeMeta != null && meta.superTypeMeta?.type != null) {
                addToPrototype(meta.superTypeMeta?.type)
            }
            if (el is VoltumTypeDeclaration) {
                addTypeDeclarationData(el)
            }
        }
    }

    fun modifyUnlocked(block: Ty.() -> Unit) {
        val wasLocked = isTypeLocked
        isTypeLocked = false
        block.invoke(this)
        isTypeLocked = wasLocked
    }

    fun addToPrototype(proto: Ty?) {
        if (isTypeLocked) {
            logger.error("Type[${this}] is locked, cannot set prototype")
            return
        }
        if (prototype == proto) {
            logger.warn("Type[${this}] already has prototype[${proto}]")
            return
        }

        prototype = proto
    }

    fun setAsDefault(value: Boolean = true) {
        isDefaultType = value
        setLocked(value)
    }

    fun setLocked(value: Boolean = true) {
        isTypeLocked = value
    }

    fun prototypeChain(): Sequence<Ty> = sequence {
        val seen = mutableSetOf<Ty>()
        seen.add(this@Ty)

        yield(this@Ty)

        var p = prototype
        while (p != null) {
            yield(p)
            if (seen.contains(p)) {
                break
            }
            seen.add(p)

            p = p.prototype
        }
    }

    fun addField(name: String, ty: Lazy<Ty?>, kind: TyFieldKind = TyFieldKind.FIELD): TyField {
        val field = TyField(this, name, ty, kind, { field, t ->
             if (t?.linkedToField == null)
                t?.linkedToField = field
        })

        if (isTypeLocked) {
            logger.error("Type[${this}] is locked, cannot add field")
            return field
        }

        members[name] = field

        return field
    }

    fun addField(name: String, ty: Ty?, kind: TyFieldKind = TyFieldKind.FIELD): TyField = addField(name, lazy { ty }, kind)

    fun addMethod(name: String, ty: Ty): TyField = addField(name, ty, TyFieldKind.METHOD)
    fun addMethod(name: String, ty: Lazy<Ty?>): TyField = addField(name, ty, TyFieldKind.METHOD)

    private fun setFieldLinked(field: TyField?, el: PsiElement) {
        if (field != null && field.linkedElement == null)
            field.linkedElement = el as? VoltumElement
        if (field != null) {
            el.putUserData(PROTOTYPE_FIELD_REF_KEY, SoftReference(field))
            logger.debug("[${this}] Caching field reference for ${field.name} -> el ${el}")
        }
    }

    fun addField(field: TyField) = addField(field.name, field.lazyTy, field.kind)

    fun addField(field: VoltumTypeDeclarationFieldMember): TyField {
        val tr = field.typeRef
        val ty = tr.tryResolveTypeLazy()
        
        return addField(field.varId.name!!, ty).apply {
            setFieldLinked(this, field)
        }
    }

    fun addField(field: VoltumDictionaryField) = addField(field.getKey(), field.value.tryResolveTypeLazy()).apply {
        setFieldLinked(this, field)
    }

    fun addMethod(field: VoltumTypeDeclarationMethodMember) = addMethod(field.nameIdentifier.name!!, lazy {
        val fn = TyFunction.typeConstructor.createTyped(field)
        fn
    }).apply {
        setFieldLinked(this, field)
    }


    fun getFieldType(name: String): Ty? = getField(name)?.ty

    operator fun get(name: String): Ty? = getFieldType(name)
    operator fun set(name: String, ty: Ty) {
        val f = getField(name)
        if (f != null) {
            f.ty = ty
        } else {
            addField(name, ty)
        }
    }

    fun getField(name: String): TyField? {
        prototypeChain().forEach {
            if (it.members.containsKey(name)) {
                return it.members[name]
            }
        }
        return null
    }

    fun hasField(name: String): Boolean = prototypeChain().any { it.members.containsKey(name) }

    open fun dump(writer: Printer? = null) {
        val w = writer ?: Printer()

        w.ln()

        w.ln(this.toString())

        w.verticalList(members.values.toList(), "Members:") {
            w.a("${it.name} -> ${it.kind.name} -> ${it.ty?.name}")
        }

        w.verticalList(prototypeChain().toList(), "Prototype Chain:") {
            if (it == this) {
                w.a("(self) ")
            }
            w.a(it.name)
            w.a("(${it.id})")
        }
    }

    open fun getCorrectFoldElement(el: PsiElement): PsiElement {
        return el
    }
    open fun substituteType(el: PsiElement): FoldedTypeResult? {
        TODO("Not yet implemented: ${this::class.simpleName} -> element: ${el}")
    }

    companion object {
        val logger = thisLogger()

        fun deserialize(dataStream: StubInputStream): Ty? {
            if (!dataStream.readBoolean()) {
                return null
            }

            val kind = dataStream.readEnum<TyKind>()
            val id = dataStream.readInt()
            val name = dataStream.readUTFFast()
            val flags = dataStream.readInt()

            val inst = kind.classType.primaryConstructor?.let {
                val t = it.call()
                t.id = id
                t.name = name
                t.flags = flags
                t.deserialize(dataStream)
                return t
            }

            return inst
        }

        fun getFromElement(el: PsiElement): Ty? {
            if (el is VoltumLiteralExpr && el.parent is VoltumLiteralExpr) {
                return el.parent?.getUserData(PROTOTYPE_VALUE_KEY)?.get()
            }
            return el.getUserData(PROTOTYPE_VALUE_KEY)?.get()
        }

        fun addOrGetField(field: VoltumTypeDeclarationFieldMember): TyField? {
            val f = field.getUserData(PROTOTYPE_FIELD_REF_KEY)?.get()
            if (f != null) {
                return f
            }
            val type = getFromElement(field.parentTypeDeclaration!!)
            if (type != null) {
                return type.addField(field)
            }
            return null
        }

        fun addOrGetMethod(field: VoltumTypeDeclarationMethodMember): TyField? {
            val f = field.getUserData(PROTOTYPE_FIELD_REF_KEY)?.get()
            if (f != null) {
                return f
            }
            val type = getFromElement(field.parentTypeDeclaration!!)
            if (type != null) {
                return type.addMethod(field)
            }
            return null
        }
    }

}

fun Ty.isUnknown(): Boolean = this is TyUnknown

class TyUnknown : Ty() {
    override var name: String = "unknown"

    companion object : TyCompanion<TyUnknown> {
        override val INSTANCE = TyUnknown().apply { setAsDefault(true) }
        override val typeConstructor get() = DefaultTypeConstructor(::TyUnknown)
    }
}

fun serializeType(dataStream: StubOutputStream, ty: Ty?) {
    dataStream.writeBoolean(ty != null)
    ty?.serialize(dataStream)
}