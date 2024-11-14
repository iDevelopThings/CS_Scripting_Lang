package com.voltum.voltumscript.runtime.std.types

import com.google.gson.annotations.SerializedName
import com.voltum.voltumscript.runtime.std.StdTypeMemberKind

open class TypeMemberMeta {
    var name: String = ""
    var definition: String? = ""
    var documentation: StdTypeDocumentation? = null
    var isInstanceGetterProperty: Boolean = false
    var isGetter: Boolean = false
    var isSetter: Boolean = false

    @SerializedName("Type")
    var typeHint: StdTypeTypeHint? = null

    var kind: StdTypeMemberKind = StdTypeMemberKind.Property

    var parameters: List<StdTypeParameter> = listOf()
    val returnType: StdTypeTypeHint? = null

    override fun toString(): String {
        return "StdTypeMemberMeta(name='$name', definition=$definition, documentation=$documentation, isInstanceGetterProperty=$isInstanceGetterProperty, isGetter=$isGetter, isSetter=$isSetter, kind=$kind)"
    }
}
