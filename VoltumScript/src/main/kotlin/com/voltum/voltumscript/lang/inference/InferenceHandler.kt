package com.voltum.voltumscript.lang.inference

import com.voltum.voltumscript.ext.measureLogTime
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.ext.voltumParent

class InferenceHandler(val ctx: InferenceContext) : CachedInferenceResult() {
    val project get() = ctx.project
    override var from: VoltumElement? = ctx.from
    val key get() = ctx.key
    val kind get() = ctx.kind
    val excludeKinds get() = ctx.excludeKinds
    
    val inferenceProcessors = mutableListOf<IInferenceProcessor>(
        VariableInferenceProcessor(this),
        VariableReferenceInferenceProcessor(this),
        TypeDeclarationInferenceProcessor(this),
    )
    
    val enabledProcessors get() = inferenceProcessors.filter { it.isEnabled(this) }

    fun resolve(): CachedInferenceResult {
        measureLogTime(ctx.cacheKey.toString()) {
            for (entry in enabledProcessors) {
                val scope = ProcessScope(key, from!!)
                if (entry.accepts(ctx.from)) {
                    entry.invoke(scope)
                    continue
                }
                if (entry.canCheckParent()) {
                    val parent = from!!.voltumParent
                    if (parent != null && entry.accepts(parent)) {
                        entry.invoke(ProcessScope(key, parent))
                        continue
                    }
                }
            }
        }

        return this
    }

}

fun InferenceHandler.withKindFlags(kind: InferenceFlags): InferenceHandler {
    ctx.kind.flags.or(kind.flags)
    return this
}
