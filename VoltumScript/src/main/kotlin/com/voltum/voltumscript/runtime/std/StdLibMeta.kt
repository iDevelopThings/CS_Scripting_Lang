package com.voltum.voltumscript.runtime.std

import com.intellij.openapi.project.Project
import com.voltum.voltumscript.ext.Printer
import com.voltum.voltumscript.ext.ValueHolderString
import com.voltum.voltumscript.ext.onAdded

enum class StdTypeMetaKind(val type: String) {
    Class("Class"),
    Module("Module"),
    Prototype("Prototype")
}

enum class StdTypeMemberKind {
    Property,
    Method,
    Constructor
}

/*open class StdTypeMemberMeta {
    var name: String = ""
    var definition: String? = ""
    var documentation: StdTypeDocumentation? = null
    var isInstanceGetterProperty: Boolean = false
    var isGetter: Boolean = false
    var isSetter: Boolean = false

    @SerializedName("Type")
    var typeHint: StdTypeTypeHint? = null

    var kind: StdTypeMemberKind = StdTypeMemberKind.Property

    var parameters: List<StdTypeParameter> = listOf()
    val returnType: StdTypeTypeHint? = null

    override fun toString(): String {
        return "StdTypeMemberMeta(name='$name', definition=$definition, documentation=$documentation, isInstanceGetterProperty=$isInstanceGetterProperty, isGetter=$isGetter, isSetter=$isSetter, kind=$kind)"
    }
}*/

/*open class StdTypeMeta {
    companion object {
        val log = logger<StdTypeMeta>()

        fun classFromModule(fromMeta: StdModuleMeta): StdTypeMeta {
            val m = StdTypeMeta()
            m.kind = StdTypeMetaKind.Class
            m.name = fromMeta.name
            m.module = fromMeta.name
            m.namespace = fromMeta.namespace
            m.definition = fromMeta.definition
            return m
        }
    }

//    lateinit var virtualFile: TempVirtualFile
    var psiFile: PsiFile? = null

    var kind: StdTypeMetaKind = StdTypeMetaKind.Class
    
    var relativePath: String = ""

    var name: String = ""
    var module: String? = ""

    var namespace: String? = ""
    val namespacedName get() = "${module?.let { "$it." } ?: ""}$name"
    
    var definition: String? = ""

    val properties: MutableList<StdTypeMemberMeta> = mutableListOf()
    val propertiesByKey: MutableMap<String, StdTypeMemberMeta> = mutableMapOf()

    val methods: MutableList<StdTypeMemberMeta> = mutableListOf()
    val methodsByKey: MutableMap<String, StdTypeMemberMeta> = mutableMapOf()

    val constructors: MutableList<StdTypeMemberMeta> = mutableListOf()
    val constructorsByKey: MutableMap<String, StdTypeMemberMeta> = mutableMapOf()

    val all get() = properties + methods + constructors

    open fun init(metaService: StdLibMetaService, project: Project) {
//        rootPath = RuntimeSettings.instance.getStdLibPath()
//        if (kind == StdTypeMetaKind.Class || kind == StdTypeMetaKind.Prototype) {
//            createPsiFile(project)
//        }
    }

    fun fixTypes() {
        properties.forEach {
            it.kind = StdTypeMemberKind.Property
            propertiesByKey[it.name] = it
        }
        methods.forEach {
            it.kind = StdTypeMemberKind.Method
            methodsByKey[it.name] = it
        }
        constructors.forEach {
            it.kind = StdTypeMemberKind.Constructor
            constructorsByKey[it.name] = it
        }
    }

    override fun toString(): String {
        return "StdTypeMeta(kind=$kind, name='$name', namespace='$namespace', properties=${properties.count()}, methods=${methods.count()}, constructors=${constructors.count()})"
    }

    *//*fun createPsiFile(project: Project) {
        virtualFile = TempVirtualFile(
            rootPath!!,
            (subPath.isNotEmpty()).let { "$subPath/" } + namespacedName + "." + Constants.FILE_EXTENSION,
            VoltumFileType.INSTANCE,
            definition!!
        )
        virtualFile.isWritable = false

        psiFile = virtualFile.toPsiFile(project)
    }*//*

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


}*/

/*class StdModuleMeta() : StdTypeMeta() {

    override fun init(metaService: StdLibMetaService, project: Project) {
        super.init(metaService, project)

        *//*if (methods.isNotEmpty() || constructors.isNotEmpty()) {
            val indexClass = classFromModule(this)
            indexClass.name = "Index"
            indexClass.methods.addAll(methods)
            indexClass.constructors.addAll(constructors)
            metaService.add(indexClass)
            indexClass.init(metaService, project)
        }*//*
    }

    val classes: ValueHolderString<StdTypeMeta> = ValueHolderString<StdTypeMeta>().onAdded {
//        moduleLibrary.sourceRoots.add(it.virtualFile)
//        log.warn("Added class(${it.name}) to module(${name})")
    }

//    val moduleLibrary: StdModuleLibrary = StdModuleLibrary(this)

    override fun debugString(w: Printer, extra: () -> Unit) {
        super.debugString(w) {
            // w.verticalList(moduleLibrary.sourceRoots.toList(), "SourceRoots:") {
            //     w.a(it.path)
            // }
        }
    }
}*/


