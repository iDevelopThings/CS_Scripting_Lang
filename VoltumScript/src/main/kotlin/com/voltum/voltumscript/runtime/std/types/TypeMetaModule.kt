package com.voltum.voltumscript.runtime.std.types

import com.voltum.voltumscript.runtime.std.StdTypeMetaKind

class TypeMetaModule : TypeMeta() {
    val classes: MutableList<TypeMetaClass> = mutableListOf()
    val prototypes: MutableList<TypeMetaPrototype> = mutableListOf()

    val objects get() = classes + prototypes

    init {
        kind = StdTypeMetaKind.Module
    }
}