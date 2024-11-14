@file:Suppress("UNCHECKED_CAST")

package com.voltum.voltumscript.lang.types

import com.intellij.openapi.diagnostic.logger
import com.intellij.psi.PsiElement
import com.jetbrains.rd.util.ConcurrentHashMap
import java.util.*

object PrototypeContainer {
    val log = logger<PrototypeContainer>()

    public object ValuesHolder {
        val types = mutableListOf<Ty>()
        val typeById: ConcurrentHashMap<Int, Ty> = ConcurrentHashMap()
        val typeMap: ConcurrentHashMap<String, Ty> = ConcurrentHashMap()

        val defaultTypes = mutableListOf<Ty>()
        val defaultTypeMap: ConcurrentHashMap<String, Ty> = ConcurrentHashMap()

        fun addDefaults() {
            log.warn("Adding default types")

            add(TyUnknown.INSTANCE)
            add(TyBool.INSTANCE)
            add(TyInt32.INSTANCE)
            add(TyInt64.INSTANCE)
            add(TyDouble.INSTANCE)
            add(TyFloat.INSTANCE)
            add(TyString.INSTANCE)
            add(TyNull.INSTANCE)
            add(TyUnit.INSTANCE)
            add(TyObject.INSTANCE)
            add(TyStruct.INSTANCE)
            add(TyArray.INSTANCE)
//            add(TyDictionary.INSTANCE)
            add(TyFunction.INSTANCE)
        }
    }

    val types get() = ValuesHolder.types
    val typeById get() = ValuesHolder.typeById
    val typeMap get() = ValuesHolder.typeMap

    val defaultTypes get() = ValuesHolder.defaultTypes
    val defaultTypeMap get() = ValuesHolder.defaultTypeMap
    
    // Holds alias name -> type name
    val typeAliases = mutableMapOf<String, String>()

    operator fun get(id: Int): Ty? {
        return typeById[id]
    }

    operator fun get(name: String): Ty? {
        return typeMap[name]
    }

    fun init() {
        log.warn("init PrototypeContainer")

        ValuesHolder.addDefaults()
    }

    fun getOrCompute(id: Int, compute: () -> Ty): Ty = typeById.computeIfAbsent(id) {
        val computed = compute()
        types.add(computed)
        typeMap[computed.name] = computed
        computed
    }

    fun getOrCompute(id: String, compute: () -> Ty): Ty = typeMap.computeIfAbsent(id) {
        val computed = compute()
        types.add(computed)
        typeById[computed.id] = computed
        computed
    }

    fun add(type: Ty) {
        types.add(type)
        typeMap[type.name] = type
        typeById[type.id] = type

        if (type.isDefaultType) {
            defaultTypes.add(type)
            defaultTypeMap[type.name] = type
        }
    }

    fun <T> getOrCreateType(el: PsiElement): T = el.tryResolveType() as T

    fun tryGetDefaultType(name: String): Ty? {
        return defaultTypeMap[name] ?: defaultTypeMap[name.lowercase()] ?: typeMap[name] ?: typeMap[name.lowercase()] 
    }
  
    operator fun iterator(): Iterator<Ty> = types.iterator()
    
    
}