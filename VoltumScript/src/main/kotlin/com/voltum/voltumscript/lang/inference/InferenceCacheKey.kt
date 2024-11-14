package com.voltum.voltumscript.lang.inference

data class InferenceCacheKey(val key: String, val kind: InferenceFlags) {
    override fun toString(): String {
        return "$key:$kind"
    }

    override fun hashCode(): Int {
        return key.hashCode() + kind.flags
    }

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is InferenceCacheKey) return false

        return key == other.key && kind.flags == other.kind.flags
    }
}