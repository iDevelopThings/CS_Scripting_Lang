package com.voltum.voltumscript.lang.stubs

import com.intellij.lang.ASTNode
import com.intellij.psi.PsiFile
import com.intellij.psi.StubBuilder
import com.intellij.psi.impl.source.tree.TreeUtil
import com.intellij.psi.stubs.*
import com.intellij.psi.tree.IStubFileElementType
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.lang.VoltumLanguage
import com.voltum.voltumscript.psi.VoltumFile


class VoltumFileStub(
    file: VoltumFile?,
    private val flags: Int,
) : PsiFileStubImpl<VoltumFile>(file) {

    override fun getType() = Type

    object Type : IStubFileElementType<VoltumFileStub>(VoltumLanguage) {
        override fun getStubVersion(): Int = Constants.STUB_VERSION

        override fun getBuilder(): StubBuilder = object : DefaultStubBuilder() {
            override fun createStubForFile(file: PsiFile): StubElement<*> {
                TreeUtil.ensureParsed(file.node) // profiler hint

                check(file is VoltumFile)
                val flags = 0
                
                return VoltumFileStub(file, flags)
            }

            /*override fun skipChildProcessingWhenBuildingStubs(parent: ASTNode, child: ASTNode): Boolean {
                val elementType = child.elementType
                return elementType == MACRO_ARGUMENT || elementType == MACRO_BODY ||
                        elementType in RS_DOC_COMMENTS ||
                        elementType == BLOCK && parent.elementType == FUNCTION && skipChildForFunctionBody(child)
            }*/

        }

        override fun serialize(stub: VoltumFileStub, dataStream: StubOutputStream) {
            dataStream.writeByte(stub.flags)
        }

        override fun deserialize(dataStream: StubInputStream, parentStub: StubElement<*>?): VoltumFileStub =
            VoltumFileStub(null, dataStream.readUnsignedByte())

        override fun getExternalId(): String = Constants.NAME + ".File"

//        Uncomment to find out what causes switch to the AST
//
//        private val PARESED = com.intellij.util.containers.ContainerUtil.newConcurrentSet<String>()
//        override fun doParseContents(chameleon: ASTNode, psi: com.intellij.psi.PsiElement): ASTNode? {
//            val path = psi.containingFile?.virtualFile?.path
//            if (path != null && PARESED.add(path)) {
//                println("Parsing (${PARESED.size}) $path")
//                val trace = java.io.StringWriter().also { writer ->
//                    Exception().printStackTrace(java.io.PrintWriter(writer))
//                    writer.toString()
//                }
//                println(trace)
//                println()
//            }
//            return super.doParseContents(chameleon, psi)
//        }
    }
}
