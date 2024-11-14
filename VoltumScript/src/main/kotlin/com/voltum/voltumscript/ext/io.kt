/*
 * Use of this source code is governed by the MIT license that can be
 * found in the LICENSE file.
 */

package com.voltum.voltumscript.ext

import com.intellij.openapi.util.io.FileUtil
import com.intellij.psi.stubs.StubInputStream
import com.intellij.psi.stubs.StubOutputStream
import com.intellij.util.io.DataInputOutputUtil
import com.intellij.util.io.DataInputOutputUtil.readNullable
import com.intellij.util.io.DataInputOutputUtil.writeNullable
import java.io.*

@Throws(IOException::class)
fun DataInput.readVarInt(): Int =
    DataInputOutputUtil.readINT(this)

@Throws(IOException::class)
fun DataOutput.writeVarInt(value: Int): Unit =
    DataInputOutputUtil.writeINT(this, value)

@Throws(IOException::class)
fun OutputStream.writeStream(input: InputStream): Unit =
    FileUtil.copy(input, this)

@Throws(IOException::class)
fun <E : Enum<E>> DataOutput.writeEnum(e: E) = writeByte(e.ordinal)

@Throws(IOException::class)
inline fun <reified E : Enum<E>> DataInput.readEnum(): E = enumValues<E>()[readUnsignedByte()]

// Write collection/list of elements
@Throws(IOException::class)
fun <T> DataOutput.writeList(list: Collection<T>, writer: DataOutput.(T) -> Unit) {
    writeInt(list.size)
    for (element in list) {
        writer(element)
    }
}
@Throws(IOException::class)
fun <T> DataInput.readList(reader: DataInput.() -> T): List<T> {
    val size = readInt()
    return List(size) { reader() }
}


@Throws(IOException::class)
fun <TKey, TValue> DataOutput.writeMap(map: Map<TKey, TValue>, keyWriter: DataOutput.(TKey) -> Unit, valueWriter: DataOutput.(TValue) -> Unit) {
    writeInt(map.size)
    for ((key, value) in map) {
        keyWriter(key)
        valueWriter(value)
    }
}

@Throws(IOException::class)
fun <TKey, TValue> DataInput.readMap(keyReader: DataInput.() -> TKey, valueReader: DataInput.() -> TValue): Map<TKey, TValue> {
    val size = readInt()
    return mutableMapOf<TKey, TValue>().apply {
        repeat(size) {
            val key = keyReader()
            val value = valueReader()
            put(key, value)
        }
    }
}

fun StubInputStream.readNameAsString(): String? = readName()?.string
fun StubInputStream.readUTFFastAsNullable(): String? = readNullable(this, this::readUTFFast)
fun StubOutputStream.writeUTFFastAsNullable(value: String?) = writeNullable(this, value, this::writeUTFFast)

fun StubOutputStream.writeLongAsNullable(value: Long?) = writeNullable(this, value, this::writeLong)
fun StubInputStream.readLongAsNullable(): Long? = readNullable(this, this::readLong)

fun StubOutputStream.writeDoubleAsNullable(value: Double?) = writeNullable(this, value, this::writeDouble)
fun StubInputStream.readDoubleAsNullable(): Double? = readNullable(this, this::readDouble)
