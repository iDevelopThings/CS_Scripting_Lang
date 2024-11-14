package com.voltum.voltumscript.lang.inference

import com.intellij.openapi.diagnostic.logger
import com.voltum.voltumscript.ext.ProjectCache
import com.voltum.voltumscript.ext.recursionGuard
import kotlin.time.ExperimentalTime
import kotlin.time.measureTimedValue

object OldInferenceImplementation : ProjectCache<InferenceCacheKey, CachedInferenceResult>("InferenceCache") {
    private val LOG = logger<Inference>()

    @OptIn(ExperimentalTime::class)
    fun resolve(ctx: InferenceContext): CachedInferenceResult {

        return getOrPut(ctx.project, ctx.cacheKey) {
            recursionGuard(ctx.from, {
                measureTimedValue {
                    infer()
                }.let {
                    LOG.warn("Inference for ${ctx.key} took ${it.duration}")
                    it.value
                }
            }) ?: throw IllegalStateException("Recursion detected")
        }
    }

    private fun infer(): CachedInferenceResult {
        /*val result = CachedInferenceResult(ctx)
        
        result.processors.add(FunctionDeclarationReferenceProcessor(ctx.key, ctx.from).onMatch {
            result.functions.add(it)
        })

        result.processors.add(VariableReferenceProcessor(ctx.key, ctx.from).onMatch {
            val varDecl = it.parentOfType<VoltumVariableDeclaration>()
            if (varDecl != null) {
                result.addVariable(varDecl)
            } else {
                LOG.warn("Failed to find declaration for ${ctx.key}")
            }
        })

        result.processors.add(TypeDeclarationProcessor(ctx.key, ctx.from).onMatch {
            result.types.add(it)
        })

        VoltumUtil.processDeclarations(ctx.project, result.processors)

        return result*/
        return null!!
    }


}