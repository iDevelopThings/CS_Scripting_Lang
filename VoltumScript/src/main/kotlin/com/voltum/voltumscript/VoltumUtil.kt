@file:Suppress("UNCHECKED_CAST")
@file:OptIn(ExperimentalTime::class)

package com.voltum.voltumscript

import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.guessProjectDir
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiManager
import com.intellij.psi.ResolveState
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.psi.search.FileTypeIndex
import com.intellij.psi.search.GlobalSearchScope
import com.intellij.util.containers.Stack
import com.voltum.voltumscript.lang.IVoltumProcessor
import com.voltum.voltumscript.lang.VoltumFileType
import com.voltum.voltumscript.psi.VoltumFile
import kotlin.time.ExperimentalTime
import kotlin.time.measureTime

object VoltumUtil {
    private val LOG = logger<VoltumUtil>()

    fun findFileByPath(project: Project, path: String): VoltumFile? {
        val dir = project.guessProjectDir() ?: return null
        val virtualFile = dir.findFileByRelativePath(path) ?: return null
        return PsiManager.getInstance(project).findFile(virtualFile) as? VoltumFile
    }

    fun forAllFiles(project: Project, unit: (file: VoltumFile) -> Unit) {
        measureTime {
            val psiManager = PsiManager.getInstance(project)

            val virtualFiles = FileTypeIndex.getFiles(VoltumFileType.INSTANCE, GlobalSearchScope.allScope(project))

//            virtualFiles.addAll(project.stdMeta.moduleLibrarySourceRoots)

            for (virtualFile in virtualFiles) {
                val file = psiManager.findFile(virtualFile) as? VoltumFile
                unit(file!!)
            }
        }.let { 
            LOG.warn("forAllFiles took $it")
        }
    }

    fun processDeclarations(
        project: Project,
        processors: List<IVoltumProcessor>,
    ): Boolean {
        val mutableProcessors = processors.toMutableList()

        val processor = object : PsiScopeProcessor {
            val toRemove = Stack<IVoltumProcessor>()

            override fun execute(element: PsiElement, state: ResolveState): Boolean {
                for (p in mutableProcessors) {
                    if (p.execute(element, state)) {
                        continue
                    }
                    toRemove.add(p)
                }

                while (toRemove.isNotEmpty()) {
                    mutableProcessors.remove(toRemove.pop())
                }

                return mutableProcessors.isNotEmpty()
            }
        }

        var didMatch = false

        forAllFiles(project) { file ->
            val result = file.processDeclarations(processor, ResolveState.initial(), null, file)
            if (!result) {
                didMatch = true
                return@forAllFiles
            }
        }

        return didMatch
    }
}

/*
    fun getAllFiles(project: Project): List<VoltumFile?> {
        val result = CachedValuesManager.getManager(project).getCachedValue(project) {
            val r = FileTypeIndex
                .getFiles(VoltumFileType.INSTANCE, GlobalSearchScope.allScope(project))
                .map {
                    PsiManager.getInstance(project).findFile(it) as? VoltumFile
                }

            CachedValueProvider.Result.create(
                r,
                PsiModificationTracker.MODIFICATION_COUNT
            )
        }

        return result
    }

    fun getAllDeclarations(project: Project): List<VoltumDeclaration> {
        val result = CachedValuesManager.getManager(project).getCachedValue(project) {
            val r = FileTypeIndex
                .getFiles(VoltumFileType.INSTANCE, GlobalSearchScope.allScope(project))
                .map {
                    PsiManager.getInstance(project).findFile(it) as? VoltumFile
                }
                .filterNotNull()
                .flatMap { it.getDeclarations() ?: emptyList() }
                .toList()

            CachedValueProvider.Result.create(
                r,
                PsiModificationTracker.MODIFICATION_COUNT
            )
        }

        return result
    }

    fun findTypeDeclarations(project: Project, name: String): List<VoltumDeclaration> {
        return getAllDeclarations(project).filter { it.name == name }
    }
        fun walkTrees(project: Project, processor: IVoltumProcessor) {
            forAllFiles(project) { file ->
                val result = PsiTreeUtil.processElements(file) { element ->
                    processor.execute(element, ResolveState.initial())
                }
                if (!result) {
                    return@forAllFiles
                }
            }
        }
    
    
        fun walkTrees(project: Project, processors: List<IVoltumProcessor>, unit: (element: PsiElement) -> Unit) {
            val processor = object : PsiScopeProcessor {
                override fun execute(element: PsiElement, state: ResolveState): Boolean {
                    processors.forEach {
                        if (!it.execute(element, state)) {
                            unit(it.getFirstResult<PsiElement>()!!)
                            return false
                        }
                    }
                    return true
                }
            }
            forAllFiles(project) { file ->
                val result = PsiTreeUtil.processElements(file) { element ->
                    processor.execute(element, ResolveState.initial())
                }
                if (!result) {
                    return@forAllFiles
                }
            }
        }
    
        fun walkTreesAll(project: Project, processors: List<IVoltumProcessor>, unit: (element: List<PsiElement>) -> Unit) {
            var mutableProcessors = processors.toMutableList()
    
            val processor = object : VoltumProcessor() {
                override fun execute(element: PsiElement, state: ResolveState): Boolean {
                    val toRemove = mutableListOf<VoltumProcessor>()
    
                    mutableProcessors.forEach {
                        // when it.execute returns false, remove this processor from mutableProcessors
                        if (!it.execute(element, state)) {
                            toRemove.add(it)
                            result.addAll(it.result)
                        }
                    }
                    toRemove.forEach {
                        mutableProcessors.remove(it)
                    }
    
                    return mutableProcessors.isNotEmpty()
                }
            }
            walkTrees(project, processor)
    
            unit(processor.result)
        }
    */