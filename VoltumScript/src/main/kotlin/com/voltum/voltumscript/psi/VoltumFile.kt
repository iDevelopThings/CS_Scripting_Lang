package com.voltum.voltumscript.psi

import com.intellij.extapi.psi.PsiFileBase
import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.util.RecursionManager
import com.intellij.psi.FileViewProvider
import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.psi.util.CachedValueProvider
import com.intellij.psi.util.CachedValuesManager
import com.intellij.psi.util.PsiModificationTracker
import com.intellij.psi.util.PsiTreeUtil
import com.voltum.voltumscript.lang.VoltumFileType
import com.voltum.voltumscript.lang.VoltumLanguage


class VoltumFile(viewProvider: FileViewProvider) : PsiFileBase(viewProvider, VoltumLanguage) {

    override fun getFileType(): FileType = VoltumFileType.INSTANCE

    fun getDeclarations(): List<VoltumDeclaration> {
        val result = RecursionManager.doPreventingRecursion(this, true) {
            CachedValuesManager.getCachedValue(this) {
                val decls = PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumDeclaration::class.java)
                    .filterIsInstance<VoltumDeclaration>()

                CachedValueProvider.Result.create(decls, PsiModificationTracker.MODIFICATION_COUNT)
            }
        }

        return result!!
    }
    
    /*
    fun getObjectDeclarations(): MutableList<VoltumObjectDeclaration>? {
        return RecursionManager.doPreventingRecursion(this, true, Computable {
            CachedValuesManager.getCachedValue(this) {
                CachedValueProvider.Result.create(
                    getTopLevelDeclarations()
                        ?.mapNotNull { it.objectDeclaration }
                        ?.toMutableList(),
                    PsiModificationTracker.MODIFICATION_COUNT
                )
            }
        })
    }
    fun getFunctionDeclarations(): MutableList<VoltumFuncDeclaration>? {
        return RecursionManager.doPreventingRecursion(this, true, Computable {
            CachedValuesManager.getCachedValue(this) {
                CachedValueProvider.Result.create(
                    getTopLevelDeclarations()
                        ?.mapNotNull { it.funcDeclaration }
                        ?.toMutableList(),
                    PsiModificationTracker.MODIFICATION_COUNT
                )
            }
        })
    }
    fun getEnumDeclarations(): MutableList<VoltumEnumDeclaration>? {
        return RecursionManager.doPreventingRecursion(this, true, Computable {
            CachedValuesManager.getCachedValue(this) {
                CachedValueProvider.Result.create(
                    getTopLevelDeclarations()
                        ?.mapNotNull { it.enumDeclaration }
                        ?.toMutableList(),
                    PsiModificationTracker.MODIFICATION_COUNT
                )
            }
        })
    }*/
    
    override fun processDeclarations(processor: PsiScopeProcessor, state: ResolveState, lastParent: PsiElement?, place: PsiElement): Boolean {
        for (decl in getDeclarations()) {
            if (!processor.execute(decl, state)) {
                return false
            }
        }

        return true
    }

}