package com.voltum.voltumscript.lang.completion

import com.voltum.voltumscript.psi.VoltumElement


data class VoltumCompletionContext(
    val context: VoltumElement? = null,
    val expectedTy: Any? = null,
    val isSimplePath: Boolean = false
) {
//    val lookup: ImplLookup? = context?.implLookup
}