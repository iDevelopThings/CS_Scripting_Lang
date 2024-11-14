/*
 * Use of this source code is governed by the MIT license that can be
 * found in the LICENSE file.
 */

package com.voltum.voltumscript.parser

import com.intellij.core.CoreInjectedLanguageManager
import com.intellij.lang.ASTNode
import com.intellij.lang.LanguageBraceMatching
import com.intellij.lang.injection.InjectedLanguageManager
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiElementVisitor
import com.intellij.psi.PsiErrorElement
import com.intellij.psi.PsiFile
import com.intellij.psi.tree.TokenSet
import com.intellij.testFramework.ParsingTestCase
import com.intellij.testFramework.TestDataPath
import com.intellij.testFramework.VfsTestUtil
import com.intellij.util.SystemProperties
import com.voltum.voltumscript.TestCase
import com.voltum.voltumscript.ide.VoltumBraceMatcher
import com.voltum.voltumscript.lang.VoltumLanguage
import com.voltum.voltumscript.lexer.VoltumTestCase
import com.voltum.voltumscript.util.VoltumJUnit4TestRunner
import org.jetbrains.annotations.NonNls
import org.junit.runner.RunWith
import java.io.FileNotFoundException
import java.util.*

@TestDataPath("\$CONTENT_ROOT/testData/parser/fixtures")
@RunWith(VoltumJUnit4TestRunner::class)
abstract class VoltumParsingTestCaseBase(@NonNls dataPath: String) : ParsingTestCase(
    "parser/fixtures/$dataPath",
    "vlt",
    true,
    VoltumParserDefinition()
), VoltumTestCase {

    protected var dumpAstTreeNames
        get() = SystemProperties.getBooleanProperty("dumpAstTypeNames", false)
        set(value) {
            SystemProperties.setProperty("dumpAstTypeNames", value.toString())
        }
    protected var overwriteTestData
        get() = SystemProperties.getBooleanProperty("idea.tests.overwrite.data", false)
        set(value) {
            SystemProperties.setProperty("idea.tests.overwrite.data", value.toString())
        }

    override fun setUp() {
        super.setUp()
        addExplicitExtension(LanguageBraceMatching.INSTANCE, VoltumLanguage, VoltumBraceMatcher())
        project.registerService(InjectedLanguageManager::class.java, CoreInjectedLanguageManager::class.java)
    }

    override fun loadFile(name: String): String {
        try {
            return super.loadFile(name)
        } catch (e: FileNotFoundException) {
            val nameWithoutExt = name.substringBeforeLast('.')
            val vltFilePath = "$myFullDataPath/$nameWithoutExt.$myFileExt"
            VfsTestUtil.overwriteTestData(vltFilePath, "")
            println("Created default file $vltFilePath created.")
            val psiFilePath = "$myFullDataPath/$nameWithoutExt.txt"
            VfsTestUtil.overwriteTestData(psiFilePath, "")
            
            return super.loadFile(name)
        }
    }

    override fun getTestDataPath(): String = "src/test/testData"

    override fun getTestName(lowercaseFirstLetter: Boolean): String {
        val camelCase = super.getTestName(lowercaseFirstLetter)
        return TestCase.camelOrWordsToSnake(camelCase)
    }

    data class ErrorInfo(val element: PsiElement, val description: String) {
        override fun toString(): String {
            // get a segment of the text around the error
            val text = element.containingFile.text
            val start = element.textRange.startOffset
            val end = element.textRange.endOffset
            val errorSegment = text.substring(start, end)
            // get the previous 5 elements
            val elements = mutableListOf<PsiElement>()
            var prev = element.prevSibling
            for (i in 0 until 5) {
                if (prev == null) break
                elements.add(prev)
                prev = prev.prevSibling
            }
            val prevText = elements.joinToString(" ") { it.text }

            return "Error at $start-$end: $description\n$errorSegment\n$prevText"
        }
    }

    protected fun hasError(file: PsiFile): Pair<Boolean, MutableList<ErrorInfo>> {
        val errors = mutableListOf<ErrorInfo>()
        file.accept(object : PsiElementVisitor() {
            override fun visitElement(element: PsiElement) {
                if (element is PsiErrorElement) {
                    errors.add(ErrorInfo(element, element.errorDescription))
                    return
                }
                element.acceptChildren(this)
            }
        })
        return Pair(errors.size > 0, errors)
    }

    /** Just check that the file is parsed (somehow) without checking its AST */
    protected fun checkFileParsed() {
        val name = testName
        parseFile(name, loadFile("$name.$myFileExt"));
    }

    override fun checkResult(targetDataName: String, file: PsiFile) {
        super.checkResult(targetDataName, file)
        val (hasError, errors) = hasError(file)
        check(!hasError) {
            "Error encountered in file ${file.name}: \n${errors.joinToString { it.toString() + "\n" }}"
        }
        // printAstTree()        
    }

    protected fun printAstTree() {
        val buffer = StringBuffer()
        Arrays.stream(myFile.node.getChildren(TokenSet.ANY)).forEach { it: ASTNode -> printAstTree(it, buffer, 0) }
        println("AST tree:\n$buffer")
    }

    protected fun printAstTree(node: ASTNode, buffer: StringBuffer, indent: Int) {
        var localIndent = indent
        buffer.append(" ".repeat(localIndent))
        buffer.append(node.toString()).append("\n")
        localIndent += 2
        var childNode = node.firstChildNode

        while (childNode != null) {
            printAstTree(childNode, buffer, localIndent)
            childNode = childNode.treeNext
        }
    }
}
