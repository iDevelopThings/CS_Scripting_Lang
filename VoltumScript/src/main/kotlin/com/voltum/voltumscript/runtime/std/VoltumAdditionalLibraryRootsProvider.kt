package com.voltum.voltumscript.runtime.std

import com.intellij.openapi.project.Project
import com.intellij.openapi.roots.AdditionalLibraryRootsProvider
import com.intellij.openapi.roots.SyntheticLibrary
import com.intellij.openapi.vfs.VirtualFile

class VoltumAdditionalLibraryRootsProvider : AdditionalLibraryRootsProvider() {
    companion object {
        val instance = VoltumAdditionalLibraryRootsProvider()
    }

    override fun getAdditionalProjectLibraries(project: Project): Collection<SyntheticLibrary> =
        project.stdMeta.moduleLibraries

    override fun getRootsToWatch(project: Project): Collection<VirtualFile> =
        getAdditionalProjectLibraries(project).flatMap { it.sourceRoots }
}