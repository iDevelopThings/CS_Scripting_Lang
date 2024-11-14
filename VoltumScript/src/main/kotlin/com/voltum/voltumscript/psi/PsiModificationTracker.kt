package com.voltum.voltumscript.psi


import com.intellij.openapi.util.ModificationTracker
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.psi.ext.stubParent
import kotlin.reflect.KClass

/**
 * A PSI element that holds modification tracker for some reason.
 * This is mostly used to invalidate cached type inference results.
 */
interface VoltumModificationTrackerOwner : VoltumElement {
    val modificationTracker: ModificationTracker

    /**
     * Increments local modification counter if needed.
     *
     * If and only if false returned,
     * [VoltumPsiManager.VoltumStructureModificationTracker]
     * will be incremented.
     *
     * @param element the changed psi element
     * @see com.voltum.voltumscript.psi.VoltumPsiManagerImpl.updateModificationCount
     */
    fun incModificationCount(element: PsiElement): Boolean
}

fun PsiElement.findModificationTrackerOwner(strict: Boolean): VoltumModificationTrackerOwner? {
    return findContextOfTypeWithoutIndexAccess(
        strict,
        VoltumElement::class,
    ) as? VoltumModificationTrackerOwner
}

// We have to process contexts without index access because accessing indices during PSI event processing is slow.
private val PsiElement.contextWithoutIndexAccess: PsiElement?
    get() = stubParent

@Suppress("UNCHECKED_CAST")
private fun <T : PsiElement> PsiElement.findContextOfTypeWithoutIndexAccess(strict: Boolean, vararg classes: KClass<out T>): T? {
    var element = if (strict) contextWithoutIndexAccess else this

    while (element != null && !classes.any { it.isInstance(element) }) {
        element = element.contextWithoutIndexAccess
    }

    return element as T?
}

