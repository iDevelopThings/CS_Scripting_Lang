/*
 * Use of this source code is governed by the MIT license that can be
 * found in the LICENSE file.
 */

package com.voltum.voltumscript.ide.completion

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.impl.PsiManagerEx
import com.intellij.testFramework.fixtures.CodeInsightTestFixture
import com.voltum.voltumscript.util.InlineFile

open class VoltumCompletionTestFixture(
    fixture: CodeInsightTestFixture,
    private val defaultFileName: String = "main.voltum"
) : VoltumCompletionTestFixtureBase<String>(fixture) {

    override fun prepare(code: String) {
        InlineFile(myFixture, code.trimIndent(), defaultFileName).withCaret()
    }

    protected open fun checkAstNotLoaded(fileFilter: (VirtualFile) -> Boolean) {
        PsiManagerEx.getInstanceEx(project).setAssertOnFileLoadingFilter(fileFilter, testRootDisposable)
    }
}
