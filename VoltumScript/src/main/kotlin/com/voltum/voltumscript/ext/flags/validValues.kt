package com.voltum.voltumscript.ext.flags

import java.util.*

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@ExperimentalUnsignedTypes
@JvmName("validValuesUByte")
inline fun <reified T> Flags<UByte, T>.validValues(): EnumSet<T>
  where T : Flag<UByte>, T : Enum<T> =
  all.filter { it.value != 0.toUByte() }.toEnumSet()

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@JvmName("validValuesByte")
inline fun <reified T> Flags<Byte, T>.validValues(): EnumSet<T>
  where T : Flag<Byte>, T : Enum<T> =
  all.filter { it.value != 0.toByte() }.toEnumSet()

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@ExperimentalUnsignedTypes
@JvmName("validValuesUShort")
inline fun <reified T> Flags<UShort, T>.validValues(): EnumSet<T>
  where T : Flag<UShort>, T : Enum<T> =
  all.filter { it.value != 0.toUShort() }.toEnumSet()

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@JvmName("validValuesShort")
inline fun <reified T> Flags<Short, T>.validValues(): EnumSet<T>
  where T : Flag<Short>, T : Enum<T> =
  all.filter { it.value != 0.toShort() }.toEnumSet()

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@ExperimentalUnsignedTypes
@JvmName("validValuesUInt")
inline fun <reified T> Flags<UInt, T>.validValues(): EnumSet<T>
  where T : Flag<UInt>, T : Enum<T> =
  all.filter { it.value != 0u }.toEnumSet()

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@JvmName("validValuesInt")
inline fun <reified T> Flags<Int, T>.validValues(): EnumSet<T>
  where T : Flag<Int>, T : Enum<T> =
  all.filter { it.value != 0 }.toEnumSet()

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@ExperimentalUnsignedTypes
@JvmName("validValuesULong")
inline fun <reified T> Flags<ULong, T>.validValues(): EnumSet<T>
  where T : Flag<ULong>, T : Enum<T> =
  all.filter { it.value != 0uL }.toEnumSet()

/**
 * Obtain all discrete valid values for the bitfield
 * @return bitfield with all valid values set
 */
@JvmName("validValuesLong")
inline fun <reified T> Flags<Long, T>.validValues(): EnumSet<T>
  where T : Flag<Long>, T : Enum<T> =
  all.filter { it.value != 0L }.toEnumSet()
