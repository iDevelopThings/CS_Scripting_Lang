package com.voltum.voltumscript.lang.stubs

import com.intellij.psi.stubs.IndexSink
import com.voltum.voltumscript.lang.index.IndexKeys
import com.voltum.voltumscript.psi.VoltumFunctionStub
import com.voltum.voltumscript.psi.VoltumTypeDeclarationStub
import com.voltum.voltumscript.psi.VoltumVariableDeclarationStub


private fun IndexSink.indexNamedStub(stub: VoltumNamedStub) {
    stub.name?.let { occurrence(IndexKeys.NAMED_ELEMENTS, it) }
}

fun IndexSink.indexFunction(stub: VoltumFunctionStub) {
    indexNamedStub(stub)
}

fun IndexSink.indexTypeDeclaration(stub: VoltumTypeDeclarationStub) {
    indexNamedStub(stub)
    stub.name?.let { occurrence(IndexKeys.TYPE_DECLARATIONS, it) }

    stub.prototype?.let { prototype ->
        prototype.aliasNames.forEach {
            occurrence(IndexKeys.TYPE_DECLARATIONS, it)
            occurrence(IndexKeys.NAMED_ELEMENTS, it)
        }
    }
}

fun IndexSink.indexVariableDeclaration(stub: VoltumVariableDeclarationStub) {
    indexNamedStub(stub)
}
