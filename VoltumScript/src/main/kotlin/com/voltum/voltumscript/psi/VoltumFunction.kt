package com.voltum.voltumscript.psi

import com.intellij.lang.ASTNode
import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.intellij.psi.StubBasedPsiElement
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.psi.stubs.*
import com.voltum.voltumscript.ext.flags.EnumFlagValueProxy
import com.voltum.voltumscript.lang.stubs.*
import com.voltum.voltumscript.lang.types.Ty
import com.voltum.voltumscript.lang.types.TyFunction
import com.voltum.voltumscript.lang.types.serializeType
import com.voltum.voltumscript.lang.types.tryResolveType
import com.voltum.voltumscript.psi.ext.greenStub
import com.voltum.voltumscript.psi.ext.hasChildOfType
import com.voltum.voltumscript.psi.ext.prototypeNullable

interface VoltumFunction : VoltumDeclaration, StubBasedPsiElement<VoltumFunctionStub> {

    override fun getNameIdentifier(): VoltumFuncId?
    override fun getName(): String
    fun getArguments(): List<VoltumIdentifierWithType>
    fun getReturnType(): VoltumTypeRef?

    fun getBlockBody(): VoltumBlockBody?
    fun getStatement(): VoltumStatement?
    fun getStatements(): List<VoltumStatement> = getBlockBody()?.statementList ?: listOf(getStatement()!!)

    fun hasArgumentWithName(name: String): Boolean
}

fun VoltumFunction.isAsync() = greenStub?.isAsync ?: hasChildOfType(VoltumTypes.ASYNC_KW)

class VoltumFunctionStub : VoltumStubWithAttributes<VoltumFunction>, VoltumNamedStub {
    override var name: String?

    var isAsync by EnumFlagValueProxy(flags, StubAttributes.IsAsync)
//    var prototype: Lazy<TyFunction?>? = null

    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<out StubElement<*>, *>?,
        name: String?,
        isAsync: Boolean,
//        prototype: Lazy<TyFunction?>?
    ) : super(parent, elementType) {
        this.name = name
        this.isAsync = isAsync
//        this.prototype = prototype ?: lazy { null }
    }

    object Type : VoltumStubElementType<VoltumFunctionStub, VoltumFunction>("DECLARATION") {
        override fun createPsi(stub: VoltumFunctionStub) =
            VoltumFuncDeclarationImpl(stub, this)

        override fun createStub(psi: VoltumFunction, parentStub: StubElement<*>?): VoltumFunctionStub {
            return VoltumFunctionStub(
                parentStub,
                this,
                psi.name,
                psi.isAsync(),
//                lazy { (psi.prototypeNullable ?: psi.tryResolveType()) as? TyFunction }
            )
        }

        override fun deserialize(dataStream: StubInputStream, parentStub: StubElement<*>?): VoltumFunctionStub {
            return VoltumFunctionStub(
                parentStub,
                this,
                dataStream.readName()?.string,
                dataStream.readBoolean(),
//                (Ty.deserialize(dataStream) as? TyFunction)?.let { lazy { it } }
            )
        }

        override fun serialize(stub: VoltumFunctionStub, dataStream: StubOutputStream) =
            with(dataStream) {
                writeName(stub.name)
                writeBoolean(stub.isAsync)
//                serializeType(dataStream, stub.prototype?.value)
            }

        override fun indexStub(stub: VoltumFunctionStub, sink: IndexSink) =
            sink.indexFunction(stub)
    }


    override fun toString(): String = "${javaClass.simpleName}( name=$name, flags=$flags )"
}

abstract class VoltumFunctionMixin :
    VoltumNamedStubbedElementImpl<VoltumFunctionStub>,
    VoltumFunction {

    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumFunctionStub, nodeType: IStubElementType<*, *>) : super(stub, nodeType)

    override fun getStatement(): VoltumStatement? = findChildByType(VoltumTypes.STATEMENT)

    override fun hasArgumentWithName(name: String): Boolean =
        getArguments().any { it.nameIdentifier.text == name }


    override fun getNameId(): VoltumIdent? = findChildByType(VoltumTypes.FUNC_ID)
    override fun getNameIdentifier(): VoltumFuncId? = findChildByType(VoltumTypes.FUNC_ID)

    override fun getName(): String = nameIdentifier?.text ?: "Unknown name?"

    override fun processDeclarations(processor: PsiScopeProcessor, state: ResolveState, lastParent: PsiElement?, place: PsiElement): Boolean {
        getArguments().forEach {
            if (!processor.execute(it, state)) {
                return false
            }
        }

        getStatements().forEach {
            if (!VoltumPsiUtilImpl.processDeclarations(it, processor, state, lastParent!!)) {
                return false
            }
        }

        return true
    }


    override fun toString(): String {
        var str = "Function = "

        str += nameIdentifier?.text ?: "Unknown name"
        str += getArguments().asPresentationString()

        return str
    }
}

fun List<VoltumIdentifierWithType>.asPresentationString(): String {
    return joinToString(", ", "(", ")") {
        it.nameIdentifier.text + " " + it.type.text
    }
}