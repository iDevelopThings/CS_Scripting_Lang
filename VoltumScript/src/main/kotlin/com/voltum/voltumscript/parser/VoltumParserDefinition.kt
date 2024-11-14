package com.voltum.voltumscript.parser

import com.intellij.lang.ASTNode
import com.intellij.lang.ITokenTypeRemapper
import com.intellij.lang.ParserDefinition
import com.intellij.lang.PsiBuilder
import com.intellij.openapi.project.Project
import com.intellij.psi.FileViewProvider
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.intellij.psi.tree.IElementType
import com.intellij.psi.tree.TokenSet
import com.voltum.voltumscript.lang.stubs.VoltumFileStub
import com.voltum.voltumscript.lexer.VoltumLexerAdapter
import com.voltum.voltumscript.psi.VoltumFile
import com.voltum.voltumscript.psi.VoltumTypes

/*
class VoltumParserImpl : VoltumParser() {
    override fun parseLight(t: IElementType?, b: PsiBuilder?) {
        b!!.setTokenTypeRemapper ( object  : ITokenTypeRemapper {
            override fun filter(source: IElementType?, start: Int, end: Int, text: CharSequence?): IElementType {
                return source!!
            }
        })
        super.parseLight(t, b)
    }
}
*/

class VoltumParserDefinition : ParserDefinition {
    companion object {
//        val FILE: IFileElementType = IFileElementType(VoltumLanguage)
    }

    override fun createLexer(project: Project?) = VoltumLexerAdapter()
    override fun createParser(project: Project?) = VoltumParser()
    override fun getFileNodeType() = VoltumFileStub.Type
    override fun getCommentTokens() = COMMENTS
    override fun getStringLiteralElements() = STRING
//    override fun getWhitespaceTokens() = TokenSet.WHITE_SPACE
    override fun createElement(node: ASTNode?): PsiElement = nodeFactory(node)
    override fun createFile(viewProvider: FileViewProvider): PsiFile = VoltumFile(viewProvider)
}

fun nodeFactory(node: ASTNode?): PsiElement {
    return VoltumTypes.Factory.createElement(node)
}
//public static CompositePsiElement createElement(IElementType type) {
fun compositeNodeFactory(type: IElementType): ASTNode {
    return VoltumTypes.Factory.createElement(type)
}