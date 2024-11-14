package com.voltum.voltumscript.lang.stubs

import com.intellij.lang.ASTNode
import com.intellij.psi.stubs.*
import com.voltum.voltumscript.psi.VoltumElement

open class VoltumPlaceholderStub<PsiT : VoltumElement> : StubBase<PsiT> {
    var name: String? = null

    constructor(
        parent: StubElement<*>?,
        elementType: IStubElementType<*, *>,
        name: String?
    ) : super(parent, elementType) {
        this.name = name
    }

    open class Type<PsiT : VoltumElement>(
        override val name: String,
        private val psiCtor: (VoltumPlaceholderStub<*>, IStubElementType<*, *>) -> PsiT
    ) : VoltumStubElementType<VoltumPlaceholderStub<*>, PsiT>(name) {

        override fun shouldCreateStub(node: ASTNode): Boolean {
            val result = createStubIfParentIsStub(node)
            return result
        }

        override fun deserialize(dataStream: StubInputStream, parentStub: StubElement<*>?): VoltumPlaceholderStub<PsiT> {
            return VoltumPlaceholderStub(
                parentStub,
                this,
                dataStream.readName()?.string
            )
        }

        override fun serialize(stub: VoltumPlaceholderStub<*>, dataStream: StubOutputStream) =
            with(dataStream) {
                writeName(stub.name)
            }

        override fun createPsi(stub: VoltumPlaceholderStub<*>): PsiT = psiCtor(stub, this)

        override fun createStub(psi: PsiT, parentStub: StubElement<*>?): VoltumPlaceholderStub<PsiT> =
            VoltumPlaceholderStub(parentStub, this, name)
    }

    override fun toString() = "${javaClass.simpleName}(name=$name)"
}