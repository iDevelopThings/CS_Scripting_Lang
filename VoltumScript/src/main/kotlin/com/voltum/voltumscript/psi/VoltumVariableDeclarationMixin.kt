package com.voltum.voltumscript.psi

import com.intellij.icons.AllIcons
import com.intellij.lang.ASTNode
import com.intellij.navigation.ItemPresentation
import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.psi.stubs.*
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.ext.readEnum
import com.voltum.voltumscript.ext.writeEnum
import com.voltum.voltumscript.lang.references.VariableDeclarationValueReference
import com.voltum.voltumscript.lang.references.VoltumReference
import com.voltum.voltumscript.lang.stubs.*

class VoltumVariableDeclarationStub : VoltumStubWithAttributes<VoltumVariableDeclaration>, VoltumNamedStub {
    override var name: String?

//    var kind: VariableDeclarationKind = VariableDeclarationKind.Single

    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<out StubElement<*>, *>?,
        name: String?
    ) : super(parent, elementType) {
        this.name = name
    }

    object Type : VoltumStubElementType<VoltumVariableDeclarationStub, VoltumVariableDeclaration>(
        "VARIABLE_DECLARATION"
    ) {
        override fun createPsi(stub: VoltumVariableDeclarationStub): VoltumVariableDeclarationImpl {
            return VoltumVariableDeclarationImpl(stub, this)
//            return when (stub.kind) {
//                VariableDeclarationKind.Single -> VoltumSingleVarDeclarationImpl(stub, this)
//                VariableDeclarationKind.TupleBased -> VoltumTupleVarDeclarationImpl(stub, this)
//            }
        }

        override fun createStub(psi: VoltumVariableDeclaration, parentStub: StubElement<*>?): VoltumVariableDeclarationStub {
            return VoltumVariableDeclarationStub(
                parentStub,
                this,
                psi.name
            ).apply {
//                kind = psi.kind
            }
        }

        override fun deserialize(dataStream: StubInputStream, parentStub: StubElement<*>?) =
            VoltumVariableDeclarationStub(
                parentStub,
                this,
                dataStream.readName()?.string,
            ).apply {
//                kind = dataStream.readEnum()
            }

        override fun serialize(stub: VoltumVariableDeclarationStub, dataStream: StubOutputStream) =
            with(dataStream) {
                writeName(stub.name)
//                writeEnum(stub.kind)
            }

        override fun indexStub(stub: VoltumVariableDeclarationStub, sink: IndexSink) =
            sink.indexVariableDeclaration(stub)
    }

    override fun toString(): String = "${javaClass.simpleName}( name=$name, flags=$flags )"
}

enum class VariableDeclarationKind {
    Single,
    TupleBased
}

//val VoltumVariableDeclaration.kind
//    get() = when (this) {
//        is VoltumSingleVarDeclaration -> VariableDeclarationKind.Single
//        is VoltumTupleVarDeclaration  -> VariableDeclarationKind.TupleBased
//        else                          -> error("Unknown variable declaration type")
//    }

//val VoltumVariableDeclaration.name: String get() = nameIdentifier?.text ?: error("Variable declaration without name")

val VoltumVariableDeclaration.varIdAndValueList: Sequence<Pair<VoltumVarId, VoltumExpr>> get() =
    sequence {
        varIdList.forEachIndexed { index, varId ->
            val value = initializers?.getOrNull(index) ?: return@forEachIndexed
            yield(varId to value)
        }        
    }

abstract class VoltumVariableDeclarationMixin : VoltumNamedStubbedElementImpl<VoltumVariableDeclarationStub>, VoltumVariableDeclaration {

    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumVariableDeclarationStub, elementType: IStubElementType<*, *>) : super(stub, elementType)


    override fun getNameId(): VoltumIdent? = varName
    override fun getNameIdentifier(): PsiElement? = varName

    override fun processDeclarations(processor: PsiScopeProcessor, state: ResolveState, lastParent: PsiElement?, place: PsiElement): Boolean {
        varIdList.forEach {
            if (!processor.execute(it, state)) {
                return false
            }
        }
        return true
    }
    
    override fun toString(): String {
        var str = "VarDecl = "

        if(varIdList.size == 1) {
            str += varIdList[0].text
            str += " = "
            
            str += initializers?.map { it.text }?.joinToString(", ") ?: ""
        } else {
            str += "(${varIdList.joinToString(", ") { it.text }})"
            str += " = "
            str += "(${initializers?.map { it.text }?.joinToString(", ") ?: ""})"
        }
        return str
    }

    override fun getReference(): VoltumReference? {
        return VariableDeclarationValueReference(this, varIdList[0])
    }
}

abstract class VoltumVarIdMixin : VoltumIdentifierMixin, VoltumVarId {
    constructor(type: IElementType) : super(type)

    override fun getId(): PsiElement? = node.findChildByType(VoltumTypes.ID)?.psi
    override fun getReference(): VoltumReference? = VoltumPsiUtilImpl.getReference(this)
//        return VariableReference(parentOfType<VoltumVariableDeclaration>()!!)
    
}

//val VoltumVarIdImpl.reference: VoltumReference? = VariableReference(this)


class VariablePresentation : ItemPresentation {
    private var v: VoltumVariableDeclaration

    constructor(fn: VoltumVariableDeclaration) {
        this.v = fn
    }

    constructor(fnId: VoltumVarId) {
        this.v = fnId.parent as VoltumVariableDeclaration
    }

    override fun getLocationString(): String = v.containingFile.name
    override fun getIcon(unused: Boolean): javax.swing.Icon = AllIcons.Nodes.Variable
    override fun getPresentableText(): String = v.varName?.text ?: "Unknown"
}
