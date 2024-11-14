package com.voltum.voltumscript.lang.index

import com.intellij.psi.stubs.StubIndexKey
import com.voltum.voltumscript.psi.VoltumNamedElement
import com.voltum.voltumscript.psi.VoltumTypeDeclaration
import com.voltum.voltumscript.psi.VoltumValueTypeElement

object IndexKeys {
    val NAMED_ELEMENTS = StubIndexKey.createIndexKey<String, VoltumNamedElement>("Voltum.NamedElement")
    val TYPE_DECLARATIONS = StubIndexKey.createIndexKey<String, VoltumTypeDeclaration>("Voltum.TypeDeclaration")
    val VALUES = StubIndexKey.createIndexKey<String, VoltumValueTypeElement>("Voltum.Value")


}

