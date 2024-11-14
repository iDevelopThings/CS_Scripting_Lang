package com.voltum.voltumscript.ext.flags

import java.util.*

/**
 * Conversion from collection of flags to a bitfield
 * @return bitfield
 */
inline fun <reified T : Enum<T>> Array<out T>.toEnumSet(): EnumSet<T> =
  EnumSet.noneOf(T::class.java).apply {
    addAll(this@toEnumSet)
  }

/**
 * Conversion from collection of flags to a bitfield
 * @return bitfield
 */
inline fun <reified T : Enum<T>> Collection<T>.toEnumSet(): EnumSet<T> =
  EnumSet.noneOf(T::class.java).apply {
    addAll(this@toEnumSet)
  }
