package com.voltum.voltumscript.lang.stubs

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.extapi.psi.StubBasedPsiElementBase
import com.intellij.lang.ASTNode
import com.intellij.navigation.ItemPresentation
import com.intellij.psi.PsiElement
import com.intellij.psi.StubBasedPsiElement
import com.intellij.psi.stubs.IStubElementType
import com.intellij.psi.stubs.IndexSink
import com.intellij.psi.stubs.StubBase
import com.intellij.psi.stubs.StubElement
import com.intellij.psi.tree.IStubFileElementType
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.ext.flags.*
import com.voltum.voltumscript.lang.VoltumLanguage
import com.voltum.voltumscript.lang.references.VoltumReference
import com.voltum.voltumscript.psi.*


enum class StubAttributes(override val value: Int) : Flag<Int> {
    None(0),
    HasAttrs(1 shl 0),
    IsAsync(1 shl 1),
    ;

    companion object : Flags<Int, StubAttributes> {
        override val all: Set<StubAttributes> = values().toEnumSet()

        fun value(value: Int = 0) = EnumFlagValue(value, StubAttributes::class)
    }
}


interface IVoltumStubElement<StubT : PsiElement> : StubElement<StubT>

abstract class VoltumStubBase<StubT : PsiElement> : StubBase<StubT>, IVoltumStubElement<StubT> {
    protected val flags = EnumFlagValue(0, StubAttributes::class)
    
    val hasAttrs by EnumFlagValueProxy(flags, StubAttributes.HasAttrs)
    
    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<out StubElement<*>, *>?,
    ) : super(parent, elementType)
    
    override fun toString(): String {
        return "${super.toString()}(${flags})"
    }

}

abstract class VoltumStubWithAttributes<StubT : PsiElement> : VoltumStubBase<StubT> {
    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<out StubElement<*>, *>?,
    ) : super(parent, elementType)

    override fun toString(): String {
        return "${javaClass.simpleName}(${flags})"
    }

}

interface VoltumStubBasedPsiElement<StubT : IVoltumStubElement<*>> : PsiElement
interface VoltumNamedStubbedElement<StubT> : VoltumStubBasedPsiElement<StubT>, VoltumNamedElement
        where StubT : VoltumNamedStub,
              StubT : IVoltumStubElement<*> {
    override fun getNameIdentifier(): PsiElement?
    override fun getName(): String?
    override fun setName(name: String): PsiElement?
    override fun getLookupElement(): LookupElement?
}

abstract class VoltumStubbedElementImpl<StubT : StubElement<*>> : StubBasedPsiElementBase<StubT>, StubBasedPsiElement<StubT>, VoltumElement {
    constructor(node: ASTNode) : super(node)
    constructor(stub: StubT, nodeType: IStubElementType<*, *>) : super(stub, nodeType)

    override fun getReference(): VoltumReference? = VoltumPsiUtilImpl.getReference(this)
    override fun getReferences(): Array<VoltumReference> = VoltumPsiUtilImpl.getReferences(this)
    
    override fun getPresentation(): ItemPresentation? = VoltumPsiUtilImpl.getPresentation(this)
}

abstract class VoltumNamedStubbedElementImpl<StubT> :
    VoltumStubbedElementImpl<StubT>,
    VoltumNamedElement
        where StubT : VoltumNamedStub,
              StubT : StubElement<*> {

    constructor(node: ASTNode) : super(node)
    constructor(stub: StubT, nodeType: IStubElementType<*, *>) : super(stub, nodeType)


    override fun getNameIdentifier(): PsiElement? = findChildByType(VoltumTypes.ID)

    override fun getPresentation(): ItemPresentation? = VoltumPsiUtilImpl.getPresentation(this)
    
    override fun getName(): String? {
        val stub = greenStub
        return if (stub !== null) stub.name else nameIdentifier?.text
    }

    override fun setName(name: String): PsiElement? {
        nameIdentifier?.replace(VoltumElementFactory.createIdentifier(project, name))
        return this
    }

    override fun getLookupElement(): LookupElement? {
        TODO("Not yet implemented")
    }


}

abstract class VoltumValueStubbedElement<StubT> :
    VoltumStubbedElementImpl<StubT>
        where StubT : StubElement<*> {

    constructor(node: ASTNode) : super(node)
    constructor(stub: StubT, nodeType: IStubElementType<*, *>) : super(stub, nodeType)

}

interface VoltumNamedStub {
    var name: String?
}

abstract class VoltumStubElementType<StubT : StubElement<*>, PsiT : PsiElement>(
    open val name: String
) : IStubElementType<StubT, PsiT>(name, VoltumLanguage) {

    final override fun getExternalId(): String = "${Constants.NAME.lowercase()}.${super.toString()}"

    override fun indexStub(stub: StubT, sink: IndexSink) {}
}

fun createStubIfParentIsStub(node: ASTNode): Boolean {
    val parent = node.treeParent
    val parentType = parent.elementType
    return (parentType is IStubElementType<*, *> && parentType.shouldCreateStub(parent)) ||
            parentType is IStubFileElementType<*>
}


