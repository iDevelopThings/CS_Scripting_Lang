package com.voltum.voltumscript.runtime.std.types

import com.voltum.voltumscript.runtime.std.StdTypeMetaKind

class TypeMetaClass : TypeMeta() {
    init {
        kind = StdTypeMetaKind.Class
    }
}