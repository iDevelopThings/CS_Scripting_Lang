@file:Suppress("RecursivePropertyAccessor")

package com.voltum.voltumscript.psi.ext

import com.intellij.injected.editor.VirtualFileWindow
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.util.Key
import com.intellij.psi.PsiElement
import com.intellij.psi.util.*
import com.voltum.voltumscript.ext.recursionGuard
import com.voltum.voltumscript.ext.withNext
import com.voltum.voltumscript.lang.inference.CachedInferenceResult
import com.voltum.voltumscript.lang.inference.Inference
import com.voltum.voltumscript.lang.inference.InferenceHandler
import com.voltum.voltumscript.lang.resolver.VoltumResolveCache
import com.voltum.voltumscript.lang.types.*
import com.voltum.voltumscript.psi.*
import kotlin.reflect.KProperty


val TYPE_INFERENCE_KEY: Key<CachedValue<CachedInferenceResult>> = Key.create("TYPE_INFERENCE_KEY")
val TYPE_INFERENCE_HANDLER_KEY: Key<CachedValue<InferenceHandler?>> = Key.create("TYPE_INFERENCE_HANDLER_KEY")

interface VoltumInferenceContextOwner : VoltumElement

fun <T> VoltumInferenceContextOwner.createCachedResult(value: T): CachedValueProvider.Result<T> {
    val structureModificationTracker = project.voltumStructureModificationTracker

    return when {
        // The case of injected language. Injected PSI don't have its own event system, so can only
        // handle evens from outer PSI. For example, Rust language is injected to Kotlin's string
        // literal. If a user change the literal, we can only be notified that the literal is changed.
        // So we have to invalidate the cached value on any PSI change
        containingFile.virtualFile is VirtualFileWindow -> {
            CachedValueProvider.Result.create(value, PsiModificationTracker.MODIFICATION_COUNT)
        }

        // CachedValueProvider.Result can accept a ModificationTracker as a dependency, so the
        // cached value will be invalidated if the modification counter is incremented.
        else                                            -> {
            val modificationTracker = contextOrSelf<VoltumModificationTrackerOwner>()?.modificationTracker
            CachedValueProvider.Result.create(value, listOfNotNull(structureModificationTracker, modificationTracker))
        }
    }
}

fun <T> PsiElement.createCachedResult(value: T): CachedValueProvider.Result<T> {
    return when (this) {
        is VoltumInferenceContextOwner -> this.createCachedResult(value)
        else                           -> {
            val modificationTracker = contextOrSelf<VoltumModificationTrackerOwner>()?.modificationTracker
            CachedValueProvider.Result.create(value, listOfNotNull(project.voltumStructureModificationTracker, modificationTracker))
//            CachedValueProvider.Result.create(value, VoltumResolveCache.getInstance(project).anyPsiChangeDependentCache)
        }
    }
}


val VoltumInferenceContextOwner.inferenceHandler: InferenceHandler?
    get() = CachedValuesManager.getCachedValue(this, TYPE_INFERENCE_HANDLER_KEY) {
        val value = recursionGuard(this, {
            val handler = Inference.infer(this)
            return@recursionGuard handler
        })

        createCachedResult(value)
    }

val VoltumInferenceContextOwner.selfInferenceResult: CachedInferenceResult?
    get() = CachedValuesManager.getCachedValue(this, TYPE_INFERENCE_KEY) {
        val value = recursionGuard(this, {
            val handler = this.inferenceHandler
            if (handler != null) {
                val res = handler.resolve()
                return@recursionGuard res
            }
            return@recursionGuard null
        }, false)
        createCachedResult(value)
    }


val PsiElement.inferenceContextOwner: VoltumInferenceContextOwner?
    get() = contexts
        .withNext()
        .find { (it, next) ->
            if (it !is VoltumInferenceContextOwner) return@find false
            next != null
        }?.first as? VoltumInferenceContextOwner

val PsiElement.inference: CachedInferenceResult?
    get() = inferenceContextOwner?.selfInferenceResult

val PsiElement.prototype: Ty
    get() {
        return when (this) {
            is VoltumValueTypeElement -> this.prototype
            is VoltumTypeDeclaration  -> {
                this.greenStub?.prototype ?: this.tryResolveType() ?: TyUnknown.INSTANCE
            }

            is VoltumFuncId           -> (
                    this.parentOfType<VoltumFunction>()
                        ?: this.parentOfType<VoltumTypeDeclarationMethodMember>()
                    )?.prototype ?: TyUnknown.INSTANCE

            is VoltumFunction         -> {
                this.tryResolveType() ?: TyUnknown.INSTANCE
            }
            is VoltumTypeDeclarationMethodMember -> {
                this.tryResolveType() ?: TyUnknown.INSTANCE
            }

            is VoltumCallExpr         -> {
                val func = this.reference?.resolve()
                func?.prototype ?: TyUnknown.INSTANCE
            }

            is VoltumTypeRef          -> {
                this.inference?.prototypes?.get(this.name!!) ?: TyUnknown.INSTANCE
            }

            is VoltumPath             -> {
                val ref = this.inference?.prototypes?.get(this.name!!)
                if (ref != null) {
                    return ref
                }
                return TyUnknown.INSTANCE
            }

            is VoltumVarReference     -> {
                val proto = inference?.prototypes?.get(this.name!!)
                if (proto != null) {
                    return proto
                }
                if (this.parent is VoltumPath) {
                    val type = (this.parent as VoltumPath).reference?.resolve()
                    return type?.prototype ?: TyUnknown.INSTANCE
                }
//                thisLogger().error("No prototype for element: $this -> `${this.text}`")
                return TyUnknown.INSTANCE

            }

            is VoltumVarId            -> {
                if (this.parent is VoltumTypeDeclarationFieldMember) {
                    val type = (this.parent as VoltumTypeDeclarationFieldMember).typeRef
                    return type.prototype
                }
                return TyUnknown.INSTANCE
            }

            else                      -> TyUnknown.INSTANCE
        }
    }
/*

val VoltumExpr.requiresTypeSubstitution: Boolean
    get() = when (this) {
        is VoltumExpr -> this.requiresTypeSubstitution
        else             -> false
    }
*/


fun PsiElement.tryFoldType(type: Ty?): FoldedTypeResult? {
    val el = type?.getCorrectFoldElement(this) ?: return null
    if (el !is VoltumExprMixin) {
        throw IllegalStateException("Only VoltumExprMixin can be folded el = '$el'")
    }
    if (!el.requiresTypeSubstitution)
        throw IllegalStateException("This element does not require type substitution el = '$el'")

    return type.substituteType(el)
}

val PsiElement.prototypeNullable: Ty?
    get() {
        val proto = prototype
        if (proto is TyUnknown)
            return null
        return proto
    }