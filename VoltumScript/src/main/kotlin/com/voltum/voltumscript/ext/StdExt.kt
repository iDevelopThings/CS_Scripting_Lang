@file:OptIn(ExperimentalContracts::class)

package com.voltum.voltumscript.ext

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.util.NlsActions
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.BitUtil
import org.apache.commons.lang3.RandomStringUtils
import java.nio.file.Files
import java.nio.file.InvalidPathException
import java.nio.file.Path
import java.nio.file.Paths
import kotlin.contracts.ExperimentalContracts
import kotlin.contracts.InvocationKind
import kotlin.contracts.contract
import kotlin.streams.asSequence
import kotlin.time.TimeSource
import kotlin.time.measureTime
import kotlin.time.measureTimedValue


private val LOG = Logger.getInstance("#com.voltum")

inline fun measureLogTime(block: () -> Unit) {
    contract {
        callsInPlace(block, InvocationKind.EXACTLY_ONCE)
    }
    val dur = TimeSource.Monotonic.measureTime(block)
    Logger.getInstance("#com.voltum").warn("Time: $dur")
}

inline fun measureLogTime(tagString: String, block: () -> Unit) {
    contract {
        callsInPlace(block, InvocationKind.EXACTLY_ONCE)
    }
    val dur = TimeSource.Monotonic.measureTime(block)
    Logger.getInstance("#com.voltum").warn("Time[$tagString]: $dur")
}
inline fun measureLogTimeWithDebugCtx(tagString: String, debugString: String, block: () -> Unit) {
    contract {
        callsInPlace(block, InvocationKind.EXACTLY_ONCE)
    }
    val dur = TimeSource.Monotonic.measureTime(block)
    Logger.getInstance("#com.voltum").warn("Time[$tagString]: $dur")
    Logger.getInstance("#com.voltum").debug(debugString)
}

inline fun <T> measureLogTimeValue(block: () -> T): T {
    contract {
        callsInPlace(block, InvocationKind.EXACTLY_ONCE)
    }

    val result = TimeSource.Monotonic.measureTimedValue(block)
    Logger.getInstance("#com.voltum").warn("Time: ${result.duration}")
    return result.value
}


inline fun <T> VirtualFile.applyWithSymlink(f: (VirtualFile) -> T?): T? {
    return f(this) ?: f(canonicalFile ?: return null)
}

fun String.toPath(): Path = Paths.get(this)

fun String.toPathOrNull(): Path? = pathOrNull(this::toPath)

fun Path.resolveOrNull(other: String): Path? = pathOrNull { resolve(other) }

private inline fun pathOrNull(block: () -> Path): Path? {
    return try {
        block()
    } catch (e: InvalidPathException) {
        LOG.warn(e)
        null
    }
}

fun Path.isExecutable(): Boolean = Files.isExecutable(this)

fun Path.list(): Sequence<Path> = Files.list(this).asSequence()

fun String.pluralize(): String = StringUtil.pluralize(this)

@NlsActions.ActionText
fun String.capitalized(): String = StringUtil.capitalize(this)

fun randomLowercaseAlphabetic(length: Int): String =
    RandomStringUtils.random(length, "0123456789abcdefghijklmnopqrstuvwxyz")

fun numberSuffix(number: Int): String {
    if ((number % 100) in 11..13) {
        return "th"
    }
    return when (number % 10) {
        1 -> "st"
        2 -> "nd"
        3 -> "rd"
        else -> "th"
    }
}

fun Long.isPowerOfTwo(): Boolean {
    return this > 0 && (this.and(this - 1)) == 0L
}

fun Boolean.toggle(): Boolean {
    return this xor true
}