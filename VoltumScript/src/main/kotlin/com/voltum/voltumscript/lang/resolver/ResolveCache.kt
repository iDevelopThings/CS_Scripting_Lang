@file:Suppress("DEPRECATION")

package com.voltum.voltumscript.lang.resolver

import com.intellij.injected.editor.VirtualFileWindow
import com.intellij.openapi.Disposable
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.RecursionManager
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.intellij.psi.ResolveResult
import com.intellij.psi.impl.AnyPsiChangeListener
import com.intellij.psi.impl.PsiManagerImpl.ANY_PSI_CHANGE_TOPIC
import com.intellij.psi.util.CachedValue
import com.intellij.psi.util.CachedValueProvider
import com.intellij.psi.util.CachedValuesManager
import com.intellij.psi.util.PsiUtilCore
import com.intellij.util.containers.ConcurrentWeakKeySoftValueHashMap
import com.intellij.util.containers.HashingStrategy
import com.voltum.voltumscript.ext.Testmark
import com.voltum.voltumscript.ext.measureLogTime
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.ancestors
import java.lang.ref.ReferenceQueue
import java.util.concurrent.ConcurrentMap
import java.util.concurrent.atomic.AtomicReference

/**
 * The implementation is inspired by Intellij platform's [com.intellij.psi.impl.source.resolve.ResolveCache].
 * The main difference from the platform one: we invalidate the cache depends on [ResolveCacheDependency], when
 * platform cache invalidates on any PSI change.
 *
 * See [VoltumPsiManager.voltumStructureModificationTracker].
 */
@Service(Service.Level.PROJECT)
class VoltumResolveCache(project: Project) : Disposable {
    /** The cache is cleared on [VoltumPsiManager.voltumStructureModificationTracker] increment */
    private val _voltumStructureDependentCache: AtomicReference<ConcurrentMap<PsiElement, Any>?> = AtomicReference(null)

    /** The cache is cleared on [ANY_PSI_CHANGE_TOPIC] event */
    private val _anyPsiChangeDependentCache: AtomicReference<ConcurrentMap<PsiElement, Any>?> = AtomicReference(null)
    private val guard = RecursionManager.createGuard<PsiElement>("VoltumResolveCache")

    private val voltumStructureDependentCache: ConcurrentMap<PsiElement, Any>
        get() = _voltumStructureDependentCache.getOrCreateMap()

    public val anyPsiChangeDependentCache: ConcurrentMap<PsiElement, Any>
        get() = _anyPsiChangeDependentCache.getOrCreateMap()

    init {
        val voltumPsiManager = project.voltumPsiManager
        val connection = project.messageBus.connect(this)
        voltumPsiManager.subscribeVoltumStructureChange(connection, object : VoltumStructureChangeListener {
            override fun voltumStructureChanged(file: PsiFile?, changedElement: PsiElement?) =
//                measureLogTime("VoltumStructureChanged") { 
                onVoltumStructureChanged()
//        }
        })
        connection.subscribe(ANY_PSI_CHANGE_TOPIC, object : AnyPsiChangeListener {
            override fun afterPsiChanged(isPhysical: Boolean) {
//                measureLogTime("AnyPsiChanged") {
                _anyPsiChangeDependentCache.set(null)
//                }
            }

            override fun beforePsiChanged(isPhysical: Boolean) {}
        })
        voltumPsiManager.subscribeVoltumPsiChange(connection, object : VoltumPsiChangeListener {
            override fun voltumPsiChanged(file: PsiFile, element: PsiElement, isStructureModification: Boolean) =
//                measureLogTime("voltumPsiChanged") {
                onVoltumPsiChanged(element)
//                }
        })
    }

    override fun dispose() {}

    /**
     * Retrieve a cached value by [key] or compute a new value by [resolver].
     * Internally recursion-guarded by [key].
     *
     * Expected resolve results: [PsiElement], [ResolveResult] or a [List]/[Array] of [ResolveResult]
     */
    @Suppress("UNCHECKED_CAST")
    fun <K : PsiElement, V> resolveWithCaching(key: K, dep: ResolveCacheDependency, resolver: (K) -> V): V? {
        ProgressManager.checkCanceled()
        val refinedDep = refineDependency(key, dep)
        val map = getCacheFor(key, refinedDep)
        return map[key] as V? ?: run {
            val stamp = RecursionManager.markStack()
            val result = guard.doPreventingRecursion(key, true) { resolver(key) }
            ensureValidResult(result)

            if (stamp.mayCacheNow()) {
                cache(map, key, result)
            }
            result
        }
    }

    fun getCached(key: PsiElement, dep: ResolveCacheDependency): Any? {
        return getCacheFor(key, refineDependency(key, dep))[key]
    }

    private fun refineDependency(key: PsiElement, dep: ResolveCacheDependency): ResolveCacheDependency =
        when (key.containingFile.virtualFile) {
            // If virtualFile is null then event system is not enabled for this PSI file (see
            // PsiFileImpl.getVirtualFile) and we can't track PSI modifications, so depend on
            // any change. This is a case of completion, for example
            null                 -> ResolveCacheDependency.ANY_PSI_CHANGE
            // The case of injected language. Injected PSI don't have it's own event system, so can only
            // handle evens from outer PSI. For example, Voltum language is injected to Kotlin's string
            // literal. If a user change the literal, we can only be notified that the literal is changed.
            // So we have to invalidate caches for injected PSI on any PSI change
            is VirtualFileWindow -> ResolveCacheDependency.ANY_PSI_CHANGE
            else                 -> dep
        }

    private fun getCacheFor(element: PsiElement, dep: ResolveCacheDependency): ConcurrentMap<PsiElement, Any> {
        return when (dep) {
            ResolveCacheDependency.LOCAL, ResolveCacheDependency.LOCAL_AND_VOLTUM_STRUCTURE -> {
                val owner = element.findModificationTrackerOwner(strict = false)
                return if (owner != null) {
                    if (dep == ResolveCacheDependency.LOCAL) {
                        CachedValuesManager.getCachedValue(owner, LOCAL_CACHE_KEY) {
                            CachedValueProvider.Result.create(
                                createWeakMap(),
                                owner.modificationTracker
                            )
                        }

                    } else {
                        CachedValuesManager.getCachedValue(owner, LOCAL_CACHE_KEY2) {
                            CachedValueProvider.Result.create(
                                createWeakMap(),
                                owner.project.voltumStructureModificationTracker,
                                owner.modificationTracker
                            )
                        }
                    }
                } else {
                    voltumStructureDependentCache
                }

            }

            ResolveCacheDependency.VOLTUM_STRUCTURE                                         -> voltumStructureDependentCache
            ResolveCacheDependency.ANY_PSI_CHANGE                                           -> anyPsiChangeDependentCache
        }
    }

    @Suppress("UNCHECKED_CAST")
    private fun <K : PsiElement, V> cache(map: ConcurrentMap<PsiElement, Any>, element: K, result: V?) {
        // optimization: less contention
        val cached = map[element] as V?
        if (cached !== null && cached === result) return
        map[element] = result ?: NULL_RESULT as V
    }

    private fun onVoltumStructureChanged() {
        Testmarks.VoltumStructureDependentCacheCleared.hit()
        _voltumStructureDependentCache.set(null)
    }

    private fun onVoltumPsiChanged(element: PsiElement) {
        // 1. The global resolve cache invalidates only on voltum structure changes.
        // 2. If some reference element is located inside VoltumModificationTrackerOwner,
        //    the global cache will not be invalidated on its change
        // 3. PSI uses default identity-based equals/hashCode
        // 4. Intellij does incremental updates of a mutable PSI tree.
        //
        // It means that some reference element may be changed and then should be
        // re-resolved to another target. But if we use the global cache, we will
        // retrieve its previous resolve result from the cache. So if some reference
        // element is changed, we should remove it (and its ancestors) from the
        // global cache

        val referenceElement = element.parent as? VoltumReferenceElement ?: return
        val referenceNameElement = referenceElement.nameIdentifier ?: return
        if (referenceNameElement == element) {
            Testmarks.RemoveChangedElement.hit()
            referenceElement.ancestors.filter { it is VoltumReferenceElement }.forEach {
                voltumStructureDependentCache.remove(it)
            }
        }
    }

    companion object {
        fun getInstance(project: Project): VoltumResolveCache =
            ServiceManager.getService(project, VoltumResolveCache::class.java)
    }

    object Testmarks {
        object VoltumStructureDependentCacheCleared : Testmark()
        object RemoveChangedElement : Testmark()
    }
}

enum class ResolveCacheDependency {
    /**
     * Depends on the nearest [VoltumModificationTrackerOwner] and falls back to
     * [VoltumPsiManager.voltumStructureModificationTracker] if the tracker owner is not found.
     *
     * See [findModificationTrackerOwner]
     */
    LOCAL,

    /**
     * Depends on [VoltumPsiManager.voltumStructureModificationTracker]
     */
    VOLTUM_STRUCTURE,

    /**
     * Depends on both [LOCAL] and [VOLTUM_STRUCTURE]. It is not the same as "any PSI change", because,
     * for example, local changes from other functions will not invalidate the value
     */
    LOCAL_AND_VOLTUM_STRUCTURE,

    /**
     * Depends on [com.intellij.psi.util.PsiModificationTracker.MODIFICATION_COUNT]. I.e. depends on
     * any PSI change, not only in voltum files
     */
    ANY_PSI_CHANGE,
}

private fun AtomicReference<ConcurrentMap<PsiElement, Any>?>.getOrCreateMap(): ConcurrentMap<PsiElement, Any> {
    while (true) {
        get()?.let { return it }
        val map = createWeakMap<PsiElement, Any>()
        if (compareAndSet(null, map)) return map
    }
}

private fun <K : Any, V : Any> createWeakMap(): ConcurrentMap<K, V> {
    @Suppress("UnstableApiUsage")
    return object : ConcurrentWeakKeySoftValueHashMap<K, V>(
        100,
        0.75f,
        Runtime.getRuntime().availableProcessors(),
        HashingStrategy.canonical()
    ) {
        override fun createValueReference(
            value: V,
            queue: ReferenceQueue<in V>
        ): ValueReference<K, V> {
            val isTrivialValue = value === NULL_RESULT ||
                    value is Array<*> && value.size == 0 ||
                    value is List<*> && value.size == 0
            return if (isTrivialValue) {
                createStrongReference(value)
            } else {
                super.createValueReference(value, queue)
            }
        }

        override fun get(key: K): V? {
            val v = super.get(key)
            return if (v === NULL_RESULT) null else v
        }
    }
}

// BACKCOMPAT: 2020.3
@Suppress("UnstableApiUsage")
private class StrongValueReference<K, V>(
    private val value: V
) : ConcurrentWeakKeySoftValueHashMap.ValueReference<K, V> {
    override fun getKeyReference(): ConcurrentWeakKeySoftValueHashMap.KeyReference<K, V> {
        // will never GC so this method will never be called so no implementation is necessary
        throw UnsupportedOperationException()
    }

    override fun get(): V = value
}

@Suppress("UNCHECKED_CAST")
private fun <K, V> createStrongReference(value: V): StrongValueReference<K, V> {
    return when {
        value === NULL_RESULT               -> NULL_VALUE_REFERENCE as StrongValueReference<K, V>
        value === ResolveResult.EMPTY_ARRAY -> EMPTY_RESOLVE_RESULT as StrongValueReference<K, V>
        value is List<*> && value.size == 0 -> EMPTY_LIST as StrongValueReference<K, V>
        else                                -> StrongValueReference(value)
    }
}

private val NULL_RESULT = Any()
private val NULL_VALUE_REFERENCE = StrongValueReference<Any, Any>(NULL_RESULT)
private val EMPTY_RESOLVE_RESULT = StrongValueReference<Any, Array<ResolveResult>>(ResolveResult.EMPTY_ARRAY)
private val EMPTY_LIST = StrongValueReference<Any, List<Any>>(emptyList())

private val LOCAL_CACHE_KEY: Key<CachedValue<ConcurrentMap<PsiElement, Any>>> = Key.create("LOCAL_CACHE_KEY")
private val LOCAL_CACHE_KEY2: Key<CachedValue<ConcurrentMap<PsiElement, Any>>> = Key.create("LOCAL_CACHE_KEY2")

private fun ensureValidResult(result: Any?): Unit = when (result) {
    is ResolveResult -> ensureValidPsi(result)
    is Array<*>      -> ensureValidResults(result)
    is List<*>       -> ensureValidResults(result)
    is PsiElement    -> PsiUtilCore.ensureValid(result)
    else             -> Unit
}

private fun ensureValidResults(result: Array<*>) =
    result.forEach { ensureValidResult(it) }

private fun ensureValidResults(result: List<*>) =
    result.forEach { ensureValidResult(it) }

private fun ensureValidPsi(resolveResult: ResolveResult) {
    val element = resolveResult.element
    if (element != null) {
        PsiUtilCore.ensureValid(element)
    }
}
