package com.voltum.voltumscript.lang.index

import com.intellij.openapi.project.Project
import com.intellij.psi.search.GlobalSearchScope
import com.intellij.psi.stubs.StringStubIndexExtension
import com.intellij.psi.stubs.StubIndexKey
import com.voltum.voltumscript.ext.checkCommitIsNotInProgress
import com.voltum.voltumscript.ext.getAllValues
import com.voltum.voltumscript.ext.getElements
import com.voltum.voltumscript.lang.stubs.VoltumFileStub
import com.voltum.voltumscript.psi.VoltumTypeDeclaration

class VoltumTypeDeclarationIndex : StringStubIndexExtension<VoltumTypeDeclaration>() {
    override fun getVersion(): Int = VoltumFileStub.Type.stubVersion
    override fun getKey() = IndexKeys.TYPE_DECLARATIONS

    companion object {
        fun findByName(
            project: Project,
            target: String,
            scope: GlobalSearchScope = GlobalSearchScope.allScope(project)
        ): Collection<VoltumTypeDeclaration> {
            checkCommitIsNotInProgress(project)
            return getElements(IndexKeys.TYPE_DECLARATIONS, target, project, scope)
        }
        fun getAllKeys(project: Project): Collection<String> {
            checkCommitIsNotInProgress(project)
            return com.voltum.voltumscript.ext.getAllKeys(IndexKeys.TYPE_DECLARATIONS, project)
        }
        fun getAll(project: Project): Collection<VoltumTypeDeclaration> {
            checkCommitIsNotInProgress(project)
            return getAllValues(IndexKeys.TYPE_DECLARATIONS, project)
        }
        fun getAllKeyValues(project: Project): Collection<Pair<String, VoltumTypeDeclaration>> {
            checkCommitIsNotInProgress(project)
            return com.voltum.voltumscript.ext.getAllKeyValues(IndexKeys.TYPE_DECLARATIONS, project)
        }
    }
}