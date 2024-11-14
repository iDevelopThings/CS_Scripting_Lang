package com.voltum.voltumscript.lang.index

import com.intellij.openapi.project.Project
import com.intellij.psi.PsiElement
import com.intellij.psi.search.GlobalSearchScope
import com.intellij.psi.stubs.StringStubIndexExtension
import com.voltum.voltumscript.ext.*
import com.voltum.voltumscript.lang.stubs.VoltumFileStub
import com.voltum.voltumscript.psi.VoltumNamedElement


@Suppress("CompanionObjectInExtension")
class VoltumNamedElementIndex : StringStubIndexExtension<VoltumNamedElement>() {
    override fun getVersion(): Int = VoltumFileStub.Type.stubVersion
    override fun getKey() = IndexKeys.NAMED_ELEMENTS

    companion object {
        fun findElementsByName(
            project: Project,
            target: String,
            scope: GlobalSearchScope = GlobalSearchScope.allScope(project)
        ): Collection<VoltumNamedElement> {
            checkCommitIsNotInProgress(project)
            return getElements(IndexKeys.NAMED_ELEMENTS, target, project, scope)
        }

        inline fun <reified T> elementsByNameOfType(
            project: Project,
            target: String,
            scope: GlobalSearchScope = GlobalSearchScope.allScope(project)
        ): Collection<T> where T : VoltumNamedElement, T : PsiElement {
            var elements = findElementsByName(project, target, scope)
            elements = elements.filterIsInstance<T>()
            return elements
        }

        fun getAllKeys(project: Project): Collection<String> {
            checkCommitIsNotInProgress(project)
            return getAllKeys(IndexKeys.NAMED_ELEMENTS, project)
        }

        fun getAll(project: Project): Collection<VoltumNamedElement> {
            checkCommitIsNotInProgress(project)
            return getAllValues(IndexKeys.NAMED_ELEMENTS, project)
        }

        fun getAllKeyValues(project: Project): Collection<Pair<String, VoltumNamedElement>> {
            checkCommitIsNotInProgress(project)
            return getAllKeyValues(IndexKeys.NAMED_ELEMENTS, project)
        }
    }
}

//fun <T> StubIndex.processElements(
//    key: StubIndexKey<String, VoltumNamedElement>,
//    target: String,
//    project: Project,
//    scope: GlobalSearchScope,
//    java: Class<T>,
//    processor: Processor<T>
//) {
//    this.processElements(key, target, project, scope, java, processor)
//}

