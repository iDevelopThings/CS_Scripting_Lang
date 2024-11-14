package com.voltum.voltumscript.psi

import com.intellij.lang.ASTNode
import com.intellij.psi.PsiElement
import com.intellij.psi.stubs.*
import com.voltum.voltumscript.lang.index.IndexKeys
import com.voltum.voltumscript.lang.stubs.*
import com.voltum.voltumscript.lang.types.*

class VoltumDictionaryStub : VoltumPlaceholderStub<VoltumDictionaryValue> {
    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<*, *>,
        name: String? = "DICTIONARY",
        type: Ty? = TyKind.Object.createInstance()
    ) : super(parent, elementType, name) {
        this.prototype = type
    }

    var prototype: Ty? = null

    object Type : VoltumStubElementType<VoltumDictionaryStub, VoltumDictionaryValue>(
        "DICTIONARY"
    ) {
        override fun createPsi(stub: VoltumDictionaryStub): VoltumDictionaryValueImpl {
            return VoltumDictionaryValueImpl(stub, this)
        }

        override fun createStub(psi: VoltumDictionaryValue, parentStub: StubElement<*>?): VoltumDictionaryStub {
            return VoltumDictionaryStub(
                parentStub,
                this,
                "DICTIONARY",
                psi.tryResolveType(),
            )
        }

        override fun deserialize(dataStream: StubInputStream, parentStub: StubElement<*>?): VoltumDictionaryStub {
            return VoltumDictionaryStub(
                parentStub,
                this,
                dataStream.readName()?.string,
            ).apply {
                prototype = Ty.deserialize(dataStream)
            }
        }

        override fun serialize(stub: VoltumDictionaryStub, dataStream: StubOutputStream) =
            with(dataStream) {
                writeName(stub.name)
                serializeType(dataStream, stub.prototype)
            }

        override fun indexStub(stub: VoltumDictionaryStub, sink: IndexSink) {
            stub.name?.let { sink.occurrence(IndexKeys.VALUES, it) }
        }
    }

    override fun toString(): String = "${javaClass.simpleName}( name=$name )"
}

class VoltumDictionaryFieldStub : VoltumPlaceholderStub<VoltumDictionaryField> {

    var type: Ty? = null

    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<*, *>,
        name: String?,
        type: Ty?
    ) : super(parent, elementType, name) {
        this.type = type
    }

    object Type : VoltumStubElementType<VoltumDictionaryFieldStub, VoltumDictionaryField>(
        "DICTIONARY_FIELD"
    ) {
        override fun createPsi(stub: VoltumDictionaryFieldStub): VoltumDictionaryFieldImpl {
            return VoltumDictionaryFieldImpl(stub, this)
        }

        override fun createStub(psi: VoltumDictionaryField, parentStub: StubElement<*>?): VoltumDictionaryFieldStub {
            return VoltumDictionaryFieldStub(
                parentStub,
                this,
                psi.fieldId.name,
                psi.value.tryResolveType()
            )
        }

        override fun deserialize(dataStream: StubInputStream, parentStub: StubElement<*>?): VoltumDictionaryFieldStub {
            return VoltumDictionaryFieldStub(
                parentStub,
                this,
                dataStream.readName()?.string,
                Ty.deserialize(dataStream),
            ).apply {
            }
        }

        override fun serialize(stub: VoltumDictionaryFieldStub, dataStream: StubOutputStream) =
            with(dataStream) {
                writeName(stub.name)
                serializeType(dataStream, stub.type)
            }

        override fun indexStub(stub: VoltumDictionaryFieldStub, sink: IndexSink) {
            stub.name?.let { sink.occurrence(IndexKeys.VALUES, it) }
        }
    }

    override fun toString(): String = "${javaClass.simpleName}( name=$name )"
}


abstract class VoltumDictionaryMixin : VoltumStubbedElementImpl<VoltumDictionaryStub>, VoltumDictionaryValue {

    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumDictionaryStub, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override var prototype: Ty
        get() = greenStub?.prototype ?: tryResolveType() ?: throw Exception("No prototype found")
        set(_) {
            throw Exception("Cannot set prototype for literal number")
        }
}

fun VoltumDictionaryField.getKeyElement(): PsiElement? {
    // key can be any of:
    // - VoltumLiteralInt
    // - VoltumLiteralString
    // - ID
    val key = this.firstChild
    if (key is VoltumLiteralInt) return key
    if (key is VoltumLiteralString) return key
    if (key is VoltumFieldId) return key
    return null
}

fun VoltumDictionaryField.getKey(): String = getKeyElement()?.text ?: throw Exception("No key element found")
