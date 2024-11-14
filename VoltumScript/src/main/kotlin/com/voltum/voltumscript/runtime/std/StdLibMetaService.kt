package com.voltum.voltumscript.runtime.std

import com.google.gson.FieldNamingPolicy
import com.google.gson.GsonBuilder
import com.intellij.openapi.Disposable
import com.intellij.openapi.application.EDT
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.editor.Document
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.modules
import com.intellij.openapi.roots.libraries.Library
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.psi.PsiDocumentListener
import com.intellij.psi.PsiFile
import com.voltum.voltumscript.ext.*
import com.voltum.voltumscript.lang.types.PrototypeContainer
import com.voltum.voltumscript.lang.types.tryResolveType
import com.voltum.voltumscript.psi.VoltumTypeDeclaration
import com.voltum.voltumscript.psi.VoltumTypes
import com.voltum.voltumscript.psi.ext.stubChildOfElementType
import com.voltum.voltumscript.runtime.runtimeSettings
import com.voltum.voltumscript.runtime.std.types.TypeMeta
import com.voltum.voltumscript.runtime.std.types.TypeMetaClass
import com.voltum.voltumscript.runtime.std.types.TypeMetaModule
import com.voltum.voltumscript.runtime.std.types.TypeMetaPrototype
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

val Project.stdMeta: StdLibMetaService get() = service()

@Service(Service.Level.PROJECT)
class StdLibMetaService(
    val project: Project,
    private val cs: CoroutineScope
) : Disposable {
    companion object {
        val log = logger<StdLibMetaService>()
    }

    private lateinit var stdLibrary: Library
    private lateinit var stdLibDir: VirtualFile

    val meta: ValueHolderString<TypeMeta> = ValueHolderString()

    val classes: ValueHolderString<TypeMeta> = ValueHolderString()
    val modules: ValueHolderString<TypeMetaModule> = ValueHolderString()
    val prototypes: ValueHolderString<TypeMetaPrototype> = ValueHolderString()
    val moduleLibraries: MutableList<StdModuleLibrary> = mutableListOf()

    val objects get() = (classes.all + prototypes.all).filter { it.isAlias.not() }

    fun reset() {
        meta.clear()
        classes.clear()
        modules.clear()
        prototypes.clear()
        moduleLibraries.clear()
    }

    fun add(metaType: TypeMeta) {
        meta.add(metaType.namespacedName, metaType)
        when (metaType.kind) {
            StdTypeMetaKind.Class     -> classes.add(metaType.namespacedName, metaType)
            StdTypeMetaKind.Prototype -> prototypes.add(metaType.namespacedName, metaType as TypeMetaPrototype)

            StdTypeMetaKind.Module    -> {
                if (metaType !is TypeMetaModule)
                    throw Exception("TypeMetaModule is null")

                modules.add(metaType.namespacedName, metaType)
                metaType.classes.forEach { classes.add(it.namespacedName, it) }
                metaType.prototypes.forEach {
                    prototypes.add(it.namespacedName, it)

//                    it.aliases.forEach { alias ->
//                        val aliasObj = it.createAlias(alias)
//                        prototypes.add(alias, aliasObj)
//                    }
                }
            }

        }

        metaType.fixTypes()
    }

    init {

        val connection = project.messageBus.connect(this)
        connection.subscribe(PsiDocumentListener.TOPIC, object : PsiDocumentListener {
            override fun documentCreated(document: Document, psiFile: PsiFile?, project: Project) {
//                log.warn("Document created: ${psiFile?.name} ${document.text}")
            }

            override fun fileCreated(file: PsiFile, document: Document) {
                super.fileCreated(file, document)

//                log.warn("File created: ${file.name} ${document.text}")
            }
        })

        load()


    }

    override fun dispose() {
        reset()
    }

    fun load() {
        reset()

        PrototypeContainer.init()

        val metaString = runtimeSettings.getMetaJson()

        stdLibDir = LocalFileSystem.getInstance()
            .findFileByPath(runtimeSettings.getStdLibPath().toString())!!

        try {
            val gson = GsonBuilder()
                .serializeNulls()
                .setLenient()
                .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE)
                .create()

            val metaTypesList = JsonUtils.tryParseJsonArray(metaString)

            metaTypesList!!.forEach {
                val obj = it.asJsonObject
                val kind = obj.get("Kind").asString
                var type: TypeMeta? = null
                if (kind == "Class") {
                    type = gson.fromJson(it, TypeMetaClass::class.java)
                } else if (kind == "Prototype") {
                    type = gson.fromJson(it, TypeMetaPrototype::class.java)
                } else if (kind == "Module") {
                    type = gson.fromJson(it, TypeMetaModule::class.java)
                }
                if (type != null) {
                    add(type)
                }
            }

            project.modules.firstOrNull()?.let {
                val lib = StdModuleLibrary("StdLib")
                for (moduleMeta in modules.all) {
                    stdLibDir.findChild(moduleMeta.relativePath)?.let { mf ->
                        lib.sourceRoots.add(mf)
                    }
                }
                moduleLibraries.add(lib)
            }

        } catch (e: Exception) {
            log.error("Failed to load meta", e)
        }

        try {

            invokeLaterOnWriteThread {
                cs.launch {
                    withContext(Dispatchers.EDT) {
                        VirtualFileManager.getInstance().asyncRefresh {
                            objects.forEach { obj ->
                                // FileBasedIndex.getInstance().requestReindex(obj.virtualFile!!)

                                val psiFile = obj.getPsiFile(project)
                                psiFile?.stubChildOfElementType(VoltumTypes.TYPE_DECLARATION)?.let { stub ->
                                    if (stub is VoltumTypeDeclaration) {
                                        obj.type = PrototypeContainer.tryGetDefaultType(obj.name) ?: stub.tryResolveType()
                                        if(obj is TypeMetaPrototype) {
                                            obj.type!!.aliasNames = obj.aliases
                                        }
                                        // obj.aliasTypes.forEach { alias ->
                                        //     alias.type = TyKind.Struct.createInstance(null, alias.name)
                                        // }
                                    }
                                }

                                /*if (obj.type == null) {
                                    obj.type = PrototypeContainer.tryGetDefaultType(obj.name)
                                    obj.aliasTypes.forEach { alias ->
                                        alias.type = obj.type
                                    }
                                }*/

                                if (!obj.superType.isNullOrEmpty()) {
                                    obj.superTypeMeta = objects.firstOrNull { it.name == obj.superType }
                                    // obj.aliasTypes.forEach { alias ->
                                    //     alias.superTypeMeta = obj
                                    // }
                                }

                            }

                            val configuredObjects = HashSet<TypeMeta>()
                            objects.forEach { obj ->
                                val psiFile = obj.getPsiFile(project)
                                
                                psiFile?.stubChildOfElementType(VoltumTypes.TYPE_DECLARATION)?.let { stub ->
                                    if (stub is VoltumTypeDeclaration) {
                                        if (obj.type != null) {
                                            if (configuredObjects.contains(obj)) {
                                                return@forEach
                                            }
                                            configuredObjects.add(obj)
                                            obj.type?.configure(obj, stub)
                                            
                                            if(obj is TypeMetaPrototype) {
                                                obj.aliases.forEach { alias ->
                                                    PrototypeContainer.typeAliases[alias] = stub.name!!
                                                }
                                            }

                                            // obj.aliasTypes.forEach { alias ->
                                            //     alias.type?.configure(alias, stub)
                                            // }
                                        }
                                    }
                                }
                            }

                            // val writer = Printer()
                            // for (type in PrototypeContainer) {
                            //     type.dump(writer)
                            // }
                            // log.warn(writer.toString())
                        }
                    }
                }


            }

            /*cs.launch {
                writeAction {
                    var module = ModuleManager.getInstance(project).newModule(stdLibDir.path, "StdLib")

                    stdLibrary = project.libraryTable.getOrCreateLibrary("StdLib")

                    stdLibrary.apply {
                        modifiableModel.apply {
                            for (moduleMeta in modules.all) {
                                stdLibDir.findChild(moduleMeta.relativePath)?.let { mf ->
                                    addRoot(mf, OrderRootType.CLASSES)
                                }
                            }
                        }.commit()
                    }

                    ModuleRootManager.getInstance(module).modifiableModel.apply {
                        addLibraryEntries(listOf(stdLibrary), DependencyScope.COMPILE, true)

                        addContentEntry(stdLibDir).apply {
                            addSourceFolder(stdLibDir, false)
                        }
                    }.commit()

                    ModuleRootModificationUtil.addDependency(module, stdLibrary)


                }
            }*/

        } catch (e: Exception) {
            log.error("Failed to debug meta", e)
        }


    }

    fun debugString(w: Printer) {
        w.ln()

        w.verticalList(modules.all.toList(), "Modules:") {
            it.debugString(w)
        }

        w.verticalList(classes.all.toList(), "Classes:") {
            it.debugString(w)
        }

        w.verticalList(prototypes.all.toList(), "Prototypes:") {
            it.debugString(w)
        }

    }

}