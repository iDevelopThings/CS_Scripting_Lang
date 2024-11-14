package com.voltum.voltumscript

import com.intellij.codeInsight.template.impl.TemplateManagerImpl
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.roots.ModuleRootManager
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.testFramework.LightPlatformTestCase
import com.intellij.testFramework.PsiTestUtil
import com.intellij.testFramework.TestDataPath
import com.intellij.testFramework.fixtures.BasePlatformTestCase
import com.voltum.voltumscript.util.InlineFile
import com.voltum.voltumscript.util.VoltumJUnit4TestRunner
import junit.framework.AssertionFailedError
import junit.framework.TestCase
import org.intellij.lang.annotations.Language
import org.junit.runner.RunWith
import java.nio.file.Path
import java.nio.file.Paths


@RunWith(VoltumJUnit4TestRunner::class)
abstract class VoltumBasicTestCase  : LightPlatformTestCase() {}

@TestDataPath("\$CONTENT_ROOT/testData")
@RunWith(VoltumJUnit4TestRunner::class)
abstract class VoltumTestCase : BasePlatformTestCase() {
    val log = logger<VoltumTestCase>()
    
    private var tempDirRootUrl: String? = null
    private var tempDirRoot: VirtualFile? = null
    
    override fun setUp() {
        super.setUp()

        var m = ModuleRootManager.getInstance(myFixture.module)
       
        tempDirRoot = myFixture.findFileInTempDir(".")
        tempDirRootUrl = tempDirRoot?.url

        log.info("Module: ${m.module}")

    }


    protected fun openFileInEditor(path: String) {
        myFixture.configureFromExistingVirtualFile(myFixture.findFileInTempDir(path))
    }

    private fun getVirtualFileByName(path: String): VirtualFile? =
        LocalFileSystem.getInstance().findFileByPath(path)

    protected inline fun <reified X : Throwable> expect(f: () -> Unit) =  com.voltum.voltumscript.util.expect<X>(f)

    @Suppress("TestFunctionName")
    protected fun InlineFile(@Language("Voltum") code: String, name: String = "index.vlt"): InlineFile {
        val inlineFile = InlineFile(myFixture, code, name)
        return inlineFile
    }


    protected fun checkByTextWithLiveTemplate(
        @Language("Voltum") before: String,
        @Language("Voltum") after: String,
        toType: String,
        fileName: String = "main.vlt",
        action: () -> Unit
    ) {
        val actionWithTemplate = {
            action()
            assertNotNull(TemplateManagerImpl.getTemplateState(myFixture.editor))
            myFixture.type(toType)
            assertNull(TemplateManagerImpl.getTemplateState(myFixture.editor))
        }
        TemplateManagerImpl.setTemplateTesting(testRootDisposable)
        checkByText(before, after, fileName, actionWithTemplate)
    }


    protected fun checkByText(
        @Language("Voltum") before: String,
        @Language("Voltum") after: String,
        fileName: String = "main.vlt",
        action: () -> Unit
    ) {
        InlineFile(before, fileName)
        action()
        PsiTestUtil.checkPsiStructureWithCommit(myFixture.file, PsiTestUtil::checkPsiMatchesTextIgnoringNonCode)
        myFixture.checkResult(replaceCaretMarker(after))
    }
    protected fun replaceCaretMarker(text: String) = text.replace("/*caret*/", "<caret>")

    companion object {
        // XXX: hides `Assert.fail`
        fun fail(message: String): Nothing {
            throw AssertionFailedError(message)
        }
    }

}