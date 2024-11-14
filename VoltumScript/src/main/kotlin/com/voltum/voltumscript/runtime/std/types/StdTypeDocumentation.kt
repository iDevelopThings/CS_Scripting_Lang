package com.voltum.voltumscript.runtime.std.types

data class StdTypeDocumentation(
    var summary: String = "",
    var codeExample: String? = null,
) {
    fun toDocString(): String {
        return """
            |/**
            | * ${summary.split("\n").joinToString("\n") { " * $it" }}
            | *
            | * ${codeExample?.let { "```voltum\n$it\n```" }}
            | */
        """.trimMargin()
    }
}