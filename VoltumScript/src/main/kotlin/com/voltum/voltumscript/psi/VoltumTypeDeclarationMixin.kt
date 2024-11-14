package com.voltum.voltumscript.psi

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.icons.AllIcons
import com.intellij.lang.ASTNode
import com.intellij.navigation.ItemPresentation
import com.intellij.psi.stubs.*
import com.intellij.psi.tree.IElementType
import com.intellij.psi.util.parentOfType
import com.voltum.voltumscript.Icons
import com.voltum.voltumscript.lang.references.VoltumReference
import com.voltum.voltumscript.lang.stubs.*
import com.voltum.voltumscript.lang.types.*
import com.voltum.voltumscript.psi.ext.stubAncestorStrict
import javax.swing.Icon

class VoltumTypeDeclarationStub : VoltumStubWithAttributes<VoltumDeclaration>, VoltumNamedStub {
    override var name: String?

    var prototype: TyStruct? = null

    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<out StubElement<*>, *>?,
        name: String?,
        proto: TyStruct?
    ) : super(parent, elementType) {
        this.name = name
        this.prototype = proto
    }

    object Type : VoltumStubElementType<VoltumTypeDeclarationStub, VoltumDeclaration>("TYPE_DECLARATION") {
        override fun createPsi(stub: VoltumTypeDeclarationStub) =
            VoltumTypeDeclarationImpl(stub, this)

        override fun createStub(psi: VoltumDeclaration, parentStub: StubElement<*>?): VoltumTypeDeclarationStub {

            var proto = PrototypeContainer.tryGetDefaultType(psi.name!!)
            if (proto == null)
                proto = psi.tryResolveType() as? TyStruct

            return VoltumTypeDeclarationStub(
                parentStub,
                this,
                psi.name,
                proto as? TyStruct,
            )
        }

        override fun deserialize(dataStream: StubInputStream, parentStub: StubElement<*>?): VoltumTypeDeclarationStub {
            val stub = VoltumTypeDeclarationStub(
                parentStub,
                this,
                dataStream.readName()?.string,
                Ty.deserialize(dataStream) as? TyStruct
            )

            return stub
        }

        override fun serialize(stub: VoltumTypeDeclarationStub, dataStream: StubOutputStream) =
            with(dataStream) {
                writeName(stub.name)
                serializeType(dataStream, stub.prototype)
            }

        override fun indexStub(stub: VoltumTypeDeclarationStub, sink: IndexSink) =
            sink.indexTypeDeclaration(stub)
    }


    override fun toString(): String = "${javaClass.simpleName}( name=$name, flags=$flags )"
}


val VoltumTypeDeclaration.methods: List<VoltumTypeDeclarationMethodMember>
    get() = this.body.methods
val VoltumTypeDeclaration.fields: List<VoltumTypeDeclarationFieldMember>
    get() = this.body.fields
val VoltumTypeDeclaration.constructors: List<VoltumTypeDeclarationConstructor>
    get() = this.body.constructors

val VoltumTypeDeclarationFieldMember.parentTypeDeclaration: VoltumTypeDeclaration?
    get() = this.parentOfType()
val VoltumTypeDeclarationMethodMember.parentTypeDeclaration: VoltumTypeDeclaration?
    get() = this.parentOfType()

abstract class VoltumTypeDeclarationMixin :
    VoltumNamedStubbedElementImpl<VoltumTypeDeclarationStub>,
    VoltumDeclaration {

    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumTypeDeclarationStub, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override fun getLookupElement(): LookupElement {
        TODO("Not yet implemented")
    }

    override fun getName(): String? = getNameId()?.text
    override fun getNameId(): VoltumIdent? = VoltumPsiUtilImpl.getNameId(this)

    override fun getReference(): VoltumReference? = if (references.isNotEmpty()) references[0] else null
    override fun getReferences(): Array<VoltumReference> = VoltumPsiUtilImpl.getReferences(this)

    override fun getIcon(flags: Int): Icon? {
        return when (this) {
            is VoltumTypeDeclarationImpl -> Icons.STRUCT
            else                         -> null
        }
    }
}


class TypeDeclarationPresentation : ItemPresentation {
    private var decl: VoltumTypeDeclaration

    constructor(decl: VoltumTypeDeclaration) {
        this.decl = decl
    }

    constructor(declId: VoltumTypeId) {
        this.decl = declId.parent as VoltumTypeDeclaration
    }

    override fun getLocationString(): String = decl.containingFile.name
    override fun getIcon(unused: Boolean): javax.swing.Icon = AllIcons.Nodes.Class
    override fun getPresentableText(): String = decl.nameIdentifier?.text ?: "Unknown"
}

fun VoltumTypeDeclarationFieldMember.owner() = stubAncestorStrict<VoltumTypeDeclaration>()

abstract class VoltumTypeDeclarationMemberMixin : VoltumElementImpl {
    constructor(type: IElementType) : super(type)

    fun owner() = stubAncestorStrict<VoltumTypeDeclaration>()

    override fun getIcon(flags: Int): Icon? {
        return when (this) {
            is VoltumTypeDeclarationFieldMember  -> Icons.FIELD
            is VoltumTypeDeclarationMethodMember -> Icons.METHOD
            is VoltumTypeDeclarationConstructor  -> Icons.ABSTRACT_ASSOC_CONSTANT
            else                                 -> null
        }
    }
}
