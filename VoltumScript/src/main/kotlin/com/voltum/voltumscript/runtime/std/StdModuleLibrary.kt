package com.voltum.voltumscript.runtime.std

import com.intellij.navigation.ItemPresentation
import com.intellij.openapi.roots.SyntheticLibrary
import com.intellij.openapi.vfs.VirtualFile
import com.voltum.voltumscript.Icons
import com.voltum.voltumscript.runtime.std.types.TypeMetaModule
import javax.swing.Icon

class StdModuleLibrary : SyntheticLibrary, ItemPresentation {

    var moduleMeta: TypeMetaModule? = null
    val mySourceRoots: MutableCollection<VirtualFile>
    
    var nameOverride: String? = null
    val name get() = moduleMeta?.name ?: nameOverride ?: "Unknown"

    constructor(moduleMeta: TypeMetaModule, mySourceRoots: MutableCollection<VirtualFile> = mutableListOf()) : super() {
        this.moduleMeta = moduleMeta
        this.mySourceRoots = mySourceRoots
    }

    constructor(name: String, mySourceRoots: MutableCollection<VirtualFile> = mutableListOf()) : super() {
        this.mySourceRoots = mySourceRoots
        this.nameOverride = name
    }

    override fun equals(other: Any?): Boolean = other is StdModuleLibrary && other.sourceRoots == mySourceRoots
    override fun hashCode(): Int = mySourceRoots.hashCode()

    override fun getSourceRoots(): MutableCollection<VirtualFile> = mySourceRoots

    override fun getPresentableText(): String = name
    override fun getIcon(unused: Boolean): Icon = Icons.ModuleFolder

}


