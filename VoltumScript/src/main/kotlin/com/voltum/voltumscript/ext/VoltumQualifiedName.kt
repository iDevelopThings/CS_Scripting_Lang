package com.voltum.voltumscript.ext

import com.intellij.openapi.util.text.StringUtil
import com.intellij.psi.stubs.StubInputStream
import com.intellij.psi.stubs.StubOutputStream
import java.util.*
import kotlin.collections.ArrayList

class VoltumQualifiedName : Comparable<VoltumQualifiedName?> {
    private constructor(count: Int) {
        this.myComponents = ArrayList(count)
    }

    private val myComponents: MutableList<String?>

    fun append(name: String?): VoltumQualifiedName {
        val result = VoltumQualifiedName(myComponents.size + 1)
        result.myComponents.addAll(myComponents)
        result.myComponents.add(name)
        return result
    }

    fun append(qName: VoltumQualifiedName): VoltumQualifiedName {
        val result = VoltumQualifiedName(myComponents.size + qName.componentCount)
        result.myComponents.addAll(myComponents)
        result.myComponents.addAll(qName.components)
        return result
    }

    fun removeLastComponent(): VoltumQualifiedName {
        return removeTail(1)
    }

    fun removeTail(count: Int): VoltumQualifiedName {
        val size = myComponents.size
        val result = VoltumQualifiedName(size)
        result.myComponents.addAll(myComponents)
        var i = 0
        while (i < count && !result.myComponents.isEmpty()) {
            result.myComponents.removeAt(result.myComponents.size - 1)
            i++
        }
        return result
    }

    fun removeHead(count: Int): VoltumQualifiedName {
        val size = myComponents.size
        val result = VoltumQualifiedName(size)
        result.myComponents.addAll(myComponents)
        var i = 0
        while (i < count && !result.myComponents.isEmpty()) {
            result.myComponents.removeAt(0)
            i++
        }
        return result
    }

    val components: List<String?>
        get() = Collections.unmodifiableList(myComponents)

    val componentCount: Int
        get() = myComponents.size

    fun matches(vararg components: String): Boolean {
        if (myComponents.size != components.size) {
            return false
        }
        for (i in myComponents.indices) {
            if (myComponents[i] != components[i]) {
                return false
            }
        }
        return true
    }

    fun matchesPrefix(prefix: VoltumQualifiedName): Boolean {
        if (componentCount < prefix.componentCount) {
            return false
        }
        for (i in 0 until prefix.componentCount) {
            val component = components[i]
            if (component == null || component != prefix.components[i]) {
                return false
            }
        }
        return true
    }

    fun endsWith(suffix: String): Boolean {
        return suffix == lastComponent
    }

    val firstComponent: String?
        get() {
            if (myComponents.isEmpty()) {
                return null
            }
            return myComponents[0]
        }

    val lastComponent: String?
        get() {
            if (myComponents.isEmpty()) {
                return null
            }
            return myComponents[myComponents.size - 1]
        }

    override fun toString(): String {
        return join(".")
    }

    fun join(separator: String?): String {
        return StringUtil.join(myComponents, separator!!)
    }

    override fun equals(o: Any?): Boolean {
        if (this === o) return true
        if (o == null || javaClass != o.javaClass) return false
        val that = o as VoltumQualifiedName
        return myComponents == that.myComponents
    }

    override fun hashCode(): Int {
        return myComponents.hashCode()
    }

    fun subVoltumQualifiedName(fromIndex: Int, toIndex: Int): VoltumQualifiedName {
        return fromComponents(myComponents.subList(fromIndex, toIndex))
    }

    override fun compareTo(other: VoltumQualifiedName?): Int {
        return toString().compareTo(other.toString())
    }

    companion object {
        fun fromComponents(components: Collection<String?>): VoltumQualifiedName {
            for (component in components) {
                assertNoDots(component!!)
            }
            val qName = VoltumQualifiedName(components.size)
            qName.myComponents.addAll(components)
            return qName
        }

        fun fromComponents(vararg components: String): VoltumQualifiedName {
            for (component in components) {
                assertNoDots(component)
            }
            val result = VoltumQualifiedName(components.size)
            Collections.addAll(result.myComponents, *components)
            return result
        }

        fun serialize(qName: VoltumQualifiedName?, dataStream: StubOutputStream) {
            if (qName == null) {
                dataStream.writeVarInt(0)
            } else {
                dataStream.writeVarInt(qName.componentCount)
                for (s in qName.myComponents) {
                    dataStream.writeName(s)
                }
            }
        }

        fun deserialize(dataStream: StubInputStream): VoltumQualifiedName? {
            val qName: VoltumQualifiedName?
            val size = dataStream.readVarInt()
            if (size == 0) {
                qName = null
            } else {
                qName = VoltumQualifiedName(size)
                for (i in 0 until size) {
                    qName.myComponents.add(dataStream.readNameString())
                }
            }
            return qName
        }

        fun fromDottedString(refName: String): VoltumQualifiedName {
            return fromComponents(*refName.split("\\.".toRegex()).dropLastWhile { it.isEmpty() }.toTypedArray())
        }

        private fun assertNoDots(component: String) {
            require(!component.contains(".")) { "Components of VoltumQualifiedName cannot contain dots inside them, but got: $component" }
        }
    }
}
