@file:Suppress("UNCHECKED_CAST")

package com.voltum.voltumscript.lang.resolver

import com.intellij.codeInsight.completion.CompletionResultSet
import com.intellij.openapi.project.Project
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.psi.search.GlobalSearchScope
import com.intellij.psi.stubs.StubIndex
import com.intellij.psi.stubs.StubIndexKey
import com.intellij.util.SmartList
import com.voltum.voltumscript.lang.completion.VoltumCompletionContext
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumNamedElement

/**
 * ScopeEntry is some PsiElement visible in some code scope.
 *
 * [ScopeEntry] handles the two case:
 *   * aliases (that's why we need a [name] property)
 *   * lazy resolving of actual elements (that's why [element] can return `null`)
 */
interface ScopeEntry {
    val name: String
    val element: VoltumElement
    
    fun cloneWithNewElement(element: VoltumElement): ScopeEntry
}

typealias VoltumProcessor<T> = (T) -> Boolean

interface VoltumResolveProcessorBase<in T : ScopeEntry> {
    /**
     * Return `true` to stop further processing,
     * return `false` to continue search
     */
    fun process(entry: T): Boolean

    /**
     * Indicates that processor is interested only in [ScopeEntry]s with specified [names].
     * Improves performance for Resolve2.
     * `null` in completion
     */
    val names: Set<String>?

    fun acceptsName(name: String): Boolean {
        val names = names
        return names == null || name in names
    }
}

typealias VoltumResolveProcessor = VoltumResolveProcessorBase<ScopeEntry>

fun createStoppableProcessor(processor: (ScopeEntry) -> Boolean): VoltumResolveProcessor {
    return object : VoltumResolveProcessorBase<ScopeEntry> {
        override fun process(entry: ScopeEntry): Boolean = processor(entry)
        override val names: Set<String>? get() = null
    }
}

fun createProcessor(processor: (ScopeEntry) -> Unit): VoltumResolveProcessor {
    return object : VoltumResolveProcessorBase<ScopeEntry> {
        override fun process(entry: ScopeEntry): Boolean {
            processor(entry)
            return false
        }

        override val names: Set<String>? get() = null
    }
}

fun collectResolveVariants(referenceName: String?, f: (VoltumResolveProcessor) -> Unit): List<VoltumElement> {
    if (referenceName == null) return emptyList()
    val processor = ResolveVariantsCollector(referenceName)
    f(processor)
    return processor.result
}

private class ResolveVariantsCollector(
    private val referenceName: String,
    val result: MutableList<VoltumElement> = SmartList(),
) : VoltumResolveProcessorBase<ScopeEntry> {
    override val names: Set<String> = setOf(referenceName)

    override fun process(entry: ScopeEntry): Boolean {
        if (entry.name == referenceName) {
            val element = entry.element

            result += element

        }
        return false
    }
}

fun pickFirstResolveVariant(referenceName: String?, f: (VoltumResolveProcessor) -> Unit): VoltumElement? =
    pickFirstResolveEntry(referenceName, f)?.element

fun pickFirstResolveEntry(referenceName: String?, f: (VoltumResolveProcessor) -> Unit): ScopeEntry? {
    if (referenceName == null) return null
    val processor = PickFirstScopeEntryCollector(referenceName)
    f(processor)
    return processor.result
}

private class PickFirstScopeEntryCollector(
    private val referenceName: String,
    var result: ScopeEntry? = null,
) : VoltumResolveProcessorBase<ScopeEntry> {
    override val names: Set<String> = setOf(referenceName)

    override fun process(entry: ScopeEntry): Boolean {
        if (entry.name == referenceName) {
            entry.element
            result = entry
            return true
        }
        return false
    }
}

fun collectCompletionVariants(
    result: CompletionResultSet,
    context: VoltumCompletionContext,
    f: (VoltumResolveProcessor) -> Unit
) {
    val processor = CompletionVariantsCollector(result, context)
    f(processor)
}

private class CompletionVariantsCollector(
    private val result: CompletionResultSet,
    private val context: VoltumCompletionContext,
) : VoltumResolveProcessorBase<ScopeEntry> {
    override val names: Set<String>? get() = null

    override fun process(entry: ScopeEntry): Boolean {
        // addEnumVariantsIfNeeded(entry)
        // addAssociatedItemsIfNeeded(entry)

        TODO("not implemented")
        /*result.addElement(
            createLookupElement(
                scopeEntry = entry,
                context = context
            )
        )
        return false
         */
    }
    /*
        private fun addEnumVariantsIfNeeded(entry: ScopeEntry) {
            val element = entry.element as? VoltumEnumItem ?: return
    
            val expectedType = (context.expectedTy?.ty?.stripReferences() as? TyAdt)?.item
            val actualType = (element.declaredType as? TyAdt)?.item
    
            val parent = context.context
            val contextPat = if (parent is VoltumPath) parent.context else parent
            val contextIsPat = contextPat is VoltumPatBinding || contextPat is VoltumPatStruct || contextPat is VoltumPatTupleStruct
    
            if (expectedType == actualType || contextIsPat) {
                val variants = collectVariantsForEnumCompletion(element, context, entry.subst)
                val filtered = when (contextPat) {
                    is VoltumPatStruct -> variants.filter { (it.psiElement as? VoltumEnumVariant)?.blockFields != null }
                    is VoltumPatTupleStruct -> variants.filter { (it.psiElement as? VoltumEnumVariant)?.tupleFields != null }
                    else -> variants
                }
                result.addAllElements(filtered)
            }
        }
    
        private fun addAssociatedItemsIfNeeded(entry: ScopeEntry) {
            if (entry.name != "Self") return
            val entryTrait = when (val traitOrImpl = entry.element as? VoltumTraitOrImpl) {
                is VoltumTraitItem -> traitOrImpl as? VoltumTraitItem ?: return
                is VoltumImplItem -> traitOrImpl.traitRef?.path?.reference?.resolve() as? VoltumTraitItem ?: return
                else -> return
            }
    
            val associatedTypes = entryTrait
                .associatedTypesTransitively
                .mapNotNull { type ->
                    val name = type.name ?: return@mapNotNull null
                    val typeAlias = type.superItem ?: type
                    createLookupElement(
                        SimpleScopeEntry("Self::$name", typeAlias, TYPES),
                        context,
                    )
                }
            result.addAllElements(associatedTypes)
        }*/
}

fun collectNames(f: (VoltumResolveProcessor) -> Unit): Set<String> {
    val processor = NamesCollector()
    f(processor)
    return processor.result
}

private class NamesCollector(
    val result: MutableSet<String> = mutableSetOf(),
) : VoltumResolveProcessorBase<ScopeEntry> {
    override val names: Set<String>? get() = null

    override fun process(entry: ScopeEntry): Boolean {
        if (entry.name != "_") {
            result += entry.name
        }
        return false
    }
}


data class SimpleScopeEntry(
    override val name: String,
    override val element: VoltumElement,
) : ScopeEntry {
    override fun cloneWithNewElement(element: VoltumElement): ScopeEntry =
        copy(element = element)
}

fun VoltumResolveProcessor.process(name: String, e: VoltumElement): Boolean =
    process(SimpleScopeEntry(name, e))

inline fun VoltumResolveProcessor.lazy(name: String, e: () -> VoltumElement?): Boolean {
    if (!acceptsName(name)) return false
    val element = e() ?: return false
    return process(name, element)
}

fun VoltumResolveProcessor.process(e: VoltumNamedElement): Boolean {
    val name = e.name ?: return false
    return process(name, e)
}

fun VoltumResolveProcessor.processAll(elements: List<VoltumNamedElement>): Boolean {
    return elements.any { process(it) }
}
fun VoltumResolveProcessor.processAll(elements: Collection<VoltumNamedElement>): Boolean {
    return elements.any { process(it) }
}

fun VoltumResolveProcessor.asPsiScopeProcessor(name: String): PsiScopeProcessor {
    return PsiScopeProcessor { element, _ ->
        val scope = SimpleScopeEntry(name, element as VoltumElement)

        val result = process(scope)

        result
    }
}

fun processAllScopeEntries(elements: List<ScopeEntry>, processor: VoltumResolveProcessor): Boolean {
    return elements.any { processor.process(it) }
}


fun <T : ScopeEntry, U : ScopeEntry> VoltumResolveProcessorBase<T>.wrapWithMapper(
    mapper: (U) -> T
): VoltumResolveProcessorBase<U> {
    return MappingProcessor(this, mapper)
}

private class MappingProcessor<in T : ScopeEntry, in U : ScopeEntry>(
    private val originalProcessor: VoltumResolveProcessorBase<T>,
    private val mapper: (U) -> T,
) : VoltumResolveProcessorBase<U> {
    override val names: Set<String>? = originalProcessor.names
    override fun process(entry: U): Boolean {
        val mapped = mapper(entry)
        return originalProcessor.process(mapped)
    }

    override fun toString(): String = "MappingProcessor($originalProcessor, mapper = $mapper)"
}
/*
inline fun <Key : Any, reified Psi : PsiElement> getElements(
    indexKey: StubIndexKey<Key, Psi>,
    key: Key,
    project: Project,
    scope: GlobalSearchScope?
): Collection<Psi> =
    StubIndex.getElements(indexKey, key, project, scope, Psi::class.java)
*/

inline fun <Key : Any, reified Psi : VoltumElement> indexProcessor(
    processor: VoltumResolveProcessor,
    indexKey: StubIndexKey<Key, Psi>,
    project: Project,
    scope: GlobalSearchScope? = GlobalSearchScope.allScope(project)
): VoltumResolveProcessorBase<ScopeEntry> {
    return object  : VoltumResolveProcessorBase<ScopeEntry> {
        override val names: Set<String>? get() = processor.names

        override fun process(entry: ScopeEntry): Boolean {
            return StubIndex.getInstance().processElements(indexKey, entry.name as Key, project, scope, Psi::class.java) { psi ->
                return@processElements processor.process(entry.cloneWithNewElement(psi))
            }
        }
    }
}
