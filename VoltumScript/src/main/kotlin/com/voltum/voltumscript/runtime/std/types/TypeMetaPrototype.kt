package com.voltum.voltumscript.runtime.std.types

import com.voltum.voltumscript.runtime.std.StdTypeMetaKind

class TypeMetaPrototype : TypeMeta() {
    val aliases = mutableListOf<String>()

    init {
        kind = StdTypeMetaKind.Prototype
    }

//    fun createAlias(alias: String): TypeMetaPrototype {
//        val aliasObj = clone() as TypeMetaPrototype
//        aliasObj.name = alias
//        aliasObj.isAlias = true
//
//        aliasTypes.add(aliasObj)
//        
//        return aliasObj
//    }
}