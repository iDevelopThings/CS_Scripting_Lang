@file:Suppress("UNCHECKED_CAST")

package com.voltum.voltumscript.lang.inference

import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.psi.util.parentOfType
import com.voltum.voltumscript.lang.index.VoltumNamedElementIndex
import com.voltum.voltumscript.lang.index.VoltumTypeDeclarationIndex
import com.voltum.voltumscript.lang.types.PrototypeContainer
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.inference
import com.voltum.voltumscript.psi.ext.prototype


class InferenceContext(
    val from: VoltumElement,
    val kind: InferenceFlags = InferenceFlags.all(),
    val excludeKinds: InferenceFlags = InferenceFlags.empty()
) {
    val project = from.project

    val key = when (from) {
        is VoltumIdentifier -> from.name!!
        else                                                               -> from.text
    }

    val cacheKey = InferenceCacheKey(key, kind)

    override fun toString(): String {
        return "InferenceContext(from=${from}, kind=${kind}, excludeKinds=${excludeKinds})"
    }
}

object Inference {
    private val LOG = logger<Inference>()

    fun infer(element: VoltumElement): InferenceHandler {
        return InferenceHandler(InferenceContext(element))
    }
}


data class ProcessScope(
    val name: String,
    val element: VoltumElement,
) {
    val matches = mutableListOf<VoltumElement>()
}

typealias ProcessScopeFn = (ProcessScope) -> Boolean
typealias ProcessScopeElementFn <T> = (ProcessScope, T) -> Boolean

interface IInferenceProcessor {
    fun isEnabled(handler: InferenceHandler) = true
    fun canCheckParent(): Boolean = false

    fun process(element: VoltumElement): Boolean
    fun accepts(element: VoltumElement): Boolean
    operator fun invoke(scope: ProcessScope): Boolean = process(scope.element)
}

interface ITypedInferenceProcessor<T : VoltumElement> : IInferenceProcessor {
    // override fun accepts(element: VoltumElement): Boolean = acceptsElement(element as T)
    fun acceptsElement(element: T): Boolean

    // override fun process(element: VoltumElement): Boolean = processElement(element as T)
    fun processElement(element: T): Boolean


}

open class InferenceProcessor : IInferenceProcessor {
    override fun process(element: VoltumElement): Boolean = false
    override fun accepts(element: VoltumElement): Boolean = false
}

open class TypedInferenceProcessor<T : VoltumElement> : InferenceProcessor, ITypedInferenceProcessor<T> {
    val logger = thisLogger()

    val handler: InferenceHandler
    var classType: Class<T> = VoltumElement::class.java as Class<T>

    constructor(handler: InferenceHandler, klass: Class<T>) {
        this.handler = handler
        this.classType = klass
    }

    override fun canCheckParent(): Boolean = false

    override fun process(element: VoltumElement): Boolean = processElement(element as T)
    override fun processElement(element: T): Boolean = false

    override fun accepts(element: VoltumElement): Boolean {
        return if (classType.isInstance(element)) {
            acceptsElement(element as T)
        } else {
            false
        }
    }

    override fun acceptsElement(element: T): Boolean = true

}

/**
 * This would process a full variable value; for ex:
 * - `var x = 10;`
 * - `var (x, y) = (10, 20);`
 */
class VariableInferenceProcessor : TypedInferenceProcessor<VoltumVariableDeclaration> {
    constructor(handler: InferenceHandler) : super(handler, VoltumVariableDeclaration::class.java)

    override fun canCheckParent(): Boolean = false

    override fun processElement(element: VoltumVariableDeclaration): Boolean {
        val pairs = element.varIdAndValueList.toList()
        for (pair in pairs) {
            handler.addVariable(pair.first, pair.second)

            val exprValueType = pair.second.prototype
            val exprValueInference = pair.second.inference

            logger.debug("VariableInferenceProcessor: ${pair.first} -> ${exprValueType} -> ${exprValueInference}")
        }
        return true
    }
}

/**
 * This would process a kind of reference:
 * var x = 10;
 * var y = x;
 *         ^ var ref type
 * var xyz = a.b.c;
 *           | | |
 *           ^ var ref type
 */
class VariableReferenceInferenceProcessor : TypedInferenceProcessor<VoltumVarReference> {
    constructor(handler: InferenceHandler) : super(handler, VoltumVarReference::class.java)

    override fun canCheckParent(): Boolean = false

    override fun processElement(element: VoltumVarReference): Boolean {
        var didAdd = false
        var results = VoltumNamedElementIndex.findElementsByName(handler.project, element.name!!)
        if (results.isEmpty()) {
            PrototypeContainer.typeAliases[element.name!!]?.let {
                results = VoltumNamedElementIndex.findElementsByName(element.project, it)
            }
        }
        for (result in results) {
            if (result is VoltumVariableDeclaration) {
                handler.addVariable(result)
                didAdd = true
            }
            if (result is VoltumFunction) {
                handler.addFunction(result)
                didAdd = true
            }
        }

        element.parentOfType<VoltumFunction>()?.let {
            handler.addFunction(it)
            didAdd = true
        }

        return didAdd
        /*        val pathExpr = element.pathParts.last().parentOfType<VoltumPath>()
                if (pathExpr != null) {
                    val ref = pathExpr.inference
                    if (ref != null) {
                        handler.merge(ref)
                        return true
                    }
                }
                
                return false
                
                val parts = element.pathParts.toMutableList()
                val kvResults = mutableListOf<Pair<VoltumReferenceElement, CachedInferenceResult?>>()
        
                var prevType: Ty?
                var currentType: Ty? = null
                fun resolveCurrent(): Pair<VoltumReferenceElement, CachedInferenceResult?> {
                    val current = parts.first()
                    parts.removeAt(0)
        
                    val inferenceResult = current.inference
        
                    prevType = currentType
                    currentType = current.prototype
        
                    if (prevType != null) {
                        val field = prevType?.getField((current as VoltumReferenceElement).name!!)
                        if (field != null) {
                            val result = CachedInferenceResult().apply { from = current }
                            result.prototypes.add((current as VoltumReferenceElement).name!!, field.ty!!)
                            currentType = field.ty
                            return Pair(current, result)
                        }
                    }
                    return Pair((current as VoltumReferenceElement), inferenceResult)
                }
        
                while (parts.isNotEmpty()) {
                    val (el, result) = resolveCurrent()
                    result?.prototypes?.all?.forEach { handler.prototypes.add(el.name!!, it) }
                    result?.named?.all?.forEach { handler.named.add(it) }
                    kvResults.add(Pair(el, result))
                }
        
        //        val out = kvResults.map { pair ->
        //            pair.second?.prototypes?.all?.mapNotNull {
        //                it.linkedElement as? VoltumElement
        //            } ?: emptyList()
        //        }.flatten()
        
                kvResults.forEach {
                    handler.merge(it.second)
                }
        
                return false*/
    }
}

class TypeDeclarationInferenceProcessor : TypedInferenceProcessor<VoltumIdent> {
    constructor(handler: InferenceHandler) : super(handler, VoltumIdent::class.java)

    override fun acceptsElement(element: VoltumIdent): Boolean {
        return super.acceptsElement(element) && (element is VoltumTypeId || element is VoltumTypeRef)
    }

    override fun processElement(element: VoltumIdent): Boolean {
        val results = VoltumTypeDeclarationIndex.findByName(handler.project, element.name!!)
        for (result in results) {
            handler.addTypeDeclaration(result)
        }
        return results.isNotEmpty()
    }
}

/*class FunctionParameterInferenceProcessor : TypedInferenceProcessor<VoltumFunction> {
    
    var function: VoltumFunction? = null

    constructor(handler: InferenceHandler) : super(handler, VoltumFunction::class.java)

    override fun accepts(element: VoltumElement): Boolean {
        function = element.parentOfType<VoltumFunction>()
        return function != null
    }

    override fun process(element: VoltumElement): Boolean {
        if(function == null) return false
        
        val params = function!!.getArguments()
        
        return true
    }
    override fun processElement(element: VoltumFunction): Boolean {
        val results = VoltumTypeDeclarationIndex.findByName(handler.project, element.name!!)
        for (result in results) {
            handler.addTypeDeclaration(result)
        }
        return results.isNotEmpty()
    }
}*/

fun IInferenceProcessor.toProcessorEntry(handler: InferenceHandler): ProcessorEntry {
    return ProcessorEntry({ isEnabled(handler) }) { scope -> process(scope.element) }
}

data class ProcessorEntry(
    val condition: () -> Boolean,
    val processor: ProcessScopeFn
)


