package com.voltum.voltumscript.runtime.std.types

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFileManager
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.ext.Printer
import com.voltum.voltumscript.ext.toPsiFile
import com.voltum.voltumscript.lang.types.Ty
import com.voltum.voltumscript.runtime.runtimeSettings
import com.voltum.voltumscript.runtime.std.StdTypeMemberKind
import com.voltum.voltumscript.runtime.std.StdTypeMetaKind

abstract class TypeMeta : Cloneable {
//    var psiFile: PsiFile? = null

    var kind: StdTypeMetaKind = StdTypeMetaKind.Class

    var relativePath: String = ""
    val relativePathWithExtension get() = "$relativePath.${Constants.FILE_EXTENSION}"

    fun getVirtualFile(project: Project) = VirtualFileManager.getInstance()
        .findFileByNioPath(
            runtimeSettings
                .getStdLibPath()
                .resolve(relativePathWithExtension)
        )

    fun getPsiFile(project: Project) = getVirtualFile(project)?.toPsiFile(project)

    var name: String = ""
    var namespace: String? = ""
    var module: String? = ""
    var definition: String? = ""

    val namespacedName get() = "${module?.let { "$it." } ?: ""}$name"

    val properties: MutableList<TypeMemberMeta> = mutableListOf()
    val methods: MutableList<TypeMemberMeta> = mutableListOf()
    val constructors: MutableList<TypeMemberMeta> = mutableListOf()

    var superType: String? = null
    var superTypeMeta: TypeMeta? = null

    var type: Ty? = null
    var isAlias: Boolean = false
//    var aliasTypes = mutableListOf<TypeMeta>()

    fun fixTypes() {
        properties.forEach {
            it.kind = StdTypeMemberKind.Property
        }
        methods.forEach {
            it.kind = StdTypeMemberKind.Method
        }
        constructors.forEach {
            it.kind = StdTypeMemberKind.Constructor
        }
    }

    public override fun clone(): Any {
        return super.clone()
    }

    open fun debugString(w: Printer, extra: () -> Unit = {}) {
        w.ln("Name=$namespacedName Module=$module")

        w.i {
            w.verticalList(properties, "Properties:") {
                w.a(it.name)
            }
            w.verticalList(constructors, "Constructors:") {
                w.a(it.name)
            }
            w.verticalList(methods, "Methods:") {
                w.a(it.name)
            }

            extra.invoke()
        }
    }


}

