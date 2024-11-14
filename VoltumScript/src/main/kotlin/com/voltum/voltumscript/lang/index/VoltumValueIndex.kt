package com.voltum.voltumscript.lang.index

import com.intellij.openapi.project.Project
import com.intellij.psi.search.GlobalSearchScope
import com.intellij.psi.stubs.StringStubIndexExtension
import com.intellij.psi.stubs.StubIndexKey
import com.voltum.voltumscript.ext.checkCommitIsNotInProgress
import com.voltum.voltumscript.ext.getAllValues
import com.voltum.voltumscript.ext.getElements
import com.voltum.voltumscript.lang.stubs.VoltumFileStub
import com.voltum.voltumscript.psi.VoltumValueTypeElement

class VoltumValueIndex : StringStubIndexExtension<VoltumValueTypeElement>() {
    override fun getVersion(): Int = VoltumFileStub.Type.stubVersion
    override fun getKey() = IndexKeys.VALUES

    companion object {
        fun findByName(
            project: Project,
            target: String,
            scope: GlobalSearchScope = GlobalSearchScope.allScope(project)
        ): Collection<VoltumValueTypeElement> {
            checkCommitIsNotInProgress(project)
            return getElements(IndexKeys.VALUES, target, project, scope)
        }
        fun getAllKeys(project: Project): Collection<String> {
            checkCommitIsNotInProgress(project)
            return com.voltum.voltumscript.ext.getAllKeys(IndexKeys.VALUES, project)
        }
        fun getAll(project: Project): Collection<VoltumValueTypeElement> {
            checkCommitIsNotInProgress(project)
            return getAllValues(IndexKeys.VALUES, project)
        }
        fun getAllKeyValues(project: Project): Collection<Pair<String, VoltumValueTypeElement>> {
            checkCommitIsNotInProgress(project)
            return com.voltum.voltumscript.ext.getAllKeyValues(IndexKeys.VALUES, project)
        }
    }
}