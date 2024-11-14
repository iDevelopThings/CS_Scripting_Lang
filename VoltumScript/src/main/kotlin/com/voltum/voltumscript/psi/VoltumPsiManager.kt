@file:Suppress("UNUSED_PARAMETER")

package com.voltum.voltumscript.psi

import com.intellij.injected.editor.VirtualFileWindow
import com.intellij.openapi.Disposable
import com.intellij.openapi.components.service
import com.intellij.openapi.project.DumbService
import com.intellij.openapi.project.Project
import com.intellij.openapi.roots.ModuleRootEvent
import com.intellij.openapi.roots.ModuleRootListener
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.ModificationTracker
import com.intellij.openapi.util.SimpleModificationTracker
import com.intellij.psi.*
import com.intellij.psi.util.PsiModificationTracker
import com.intellij.util.messages.MessageBusConnection
import com.intellij.util.messages.Topic
import com.voltum.voltumscript.ext.measureLogTime
import com.voltum.voltumscript.ext.measureLogTimeWithDebugCtx
import com.voltum.voltumscript.lang.VoltumFileType
import com.voltum.voltumscript.psi.VoltumPsiManager.Companion.isIgnorePsiEvents
import com.voltum.voltumscript.psi.VoltumPsiTreeChangeEvent.*
import com.voltum.voltumscript.psi.ext.containingVoltumFileSkippingCodeFragments

/** Don't subscribe directly or via plugin.xml lazy listeners. Use [VoltumPsiManager.subscribeVoltumStructureChange] */
private val VOLTUM_STRUCTURE_CHANGE_TOPIC: Topic<VoltumStructureChangeListener> = Topic.create(
    "VOLTUM_STRUCTURE_CHANGE_TOPIC",
    VoltumStructureChangeListener::class.java,
    Topic.BroadcastDirection.TO_PARENT
)

/** Don't subscribe directly or via plugin.xml lazy listeners. Use [VoltumPsiManager.subscribeVoltumPsiChange] */
private val VOLTUM_PSI_CHANGE_TOPIC: Topic<VoltumPsiChangeListener> = Topic.create(
    "VOLTUM_PSI_CHANGE_TOPIC",
    VoltumPsiChangeListener::class.java,
    Topic.BroadcastDirection.TO_PARENT
)

interface VoltumPsiManager {
    /**
     * A project-global modification tracker that increments on each PSI change that can affect
     * name resolution or type inference. It will be incremented with a change of most types of
     * PSI element excluding function bodies (expressions and statements)
     */
    val voltumStructureModificationTracker: ModificationTracker

    /**
     * Similar to [voltumStructureModificationTracker], but it is not incremented by changes in
     * workspace voltum files.
     *
     * @see PackageOrigin.WORKSPACE
     */
    val voltumStructureModificationTrackerInDependencies: SimpleModificationTracker

    fun incVoltumStructureModificationCount()

    /** This is an instance method because [VoltumPsiManager] should be created prior to event subscription */
    fun subscribeVoltumStructureChange(connection: MessageBusConnection, listener: VoltumStructureChangeListener) {
        connection.subscribe(VOLTUM_STRUCTURE_CHANGE_TOPIC, listener)
    }

    /** This is an instance method because [VoltumPsiManager] should be created prior to event subscription */
    fun subscribeVoltumPsiChange(connection: MessageBusConnection, listener: VoltumPsiChangeListener) {
        connection.subscribe(VOLTUM_PSI_CHANGE_TOPIC, listener)
    }

    companion object {
        private val IGNORE_PSI_EVENTS: Key<Boolean> = Key.create("IGNORE_PSI_EVENTS")

        fun <T> withIgnoredPsiEvents(psi: PsiFile, f: () -> T): T {
            setIgnorePsiEvents(psi, true)
            try {
                return f()
            } finally {
                setIgnorePsiEvents(psi, false)
            }
        }

        fun isIgnorePsiEvents(psi: PsiFile): Boolean =
            psi.getUserData(IGNORE_PSI_EVENTS) == true

        private fun setIgnorePsiEvents(psi: PsiFile, ignore: Boolean) {
            psi.putUserData(IGNORE_PSI_EVENTS, if (ignore) true else null)
        }
    }
}

interface VoltumStructureChangeListener {
    fun voltumStructureChanged(file: PsiFile?, changedElement: PsiElement?)
}

interface VoltumPsiChangeListener {
    fun voltumPsiChanged(file: PsiFile, element: PsiElement, isStructureModification: Boolean)
}

class VoltumPsiManagerImpl(val project: Project) : VoltumPsiManager, Disposable {

    override val voltumStructureModificationTracker = SimpleModificationTracker()
    override val voltumStructureModificationTrackerInDependencies = SimpleModificationTracker()

    init {
        PsiManager.getInstance(project).addPsiTreeChangeListener(CacheInvalidator(), this)
        project.messageBus.connect().subscribe(ModuleRootListener.TOPIC, object : ModuleRootListener {
            override fun rootsChanged(event: ModuleRootEvent) {
                incVoltumStructureModificationCount()
            }
        })
        // project.messageBus.connect().subscribe(CargoProjectsService.CARGO_PROJECTS_TOPIC, CargoProjectsListener { _, _ ->
        //     incVoltumStructureModificationCount()
        // })
    }

    override fun dispose() {}

    inner class CacheInvalidator : VoltumPsiTreeChangeAdapter() {
        
        override fun handleEvent(event: VoltumPsiTreeChangeEvent) {
//            measureLogTimeWithDebugCtx("CacheInvalidator::handleEvent -> ${event.toStringSimple()}", event.toString()) {
                internalHandleEvent(event)
//            }
        }
        
        fun internalHandleEvent(event: VoltumPsiTreeChangeEvent) {
            val element = when (event) {
                is ChildRemoval.Before -> event.child
                is ChildRemoval.After -> event.parent
                is ChildReplacement.Before -> event.oldChild
                is ChildReplacement.After -> event.newChild
                is ChildAddition.After -> event.child
                is ChildMovement.After -> event.child
                is ChildrenChange.After -> if (!event.isGenericChange) event.parent else return
                is PropertyChange.After -> {
                    when (event.propertyName) {
                        PsiTreeChangeEvent.PROP_UNLOADED_PSI, PsiTreeChangeEvent.PROP_FILE_TYPES -> {
                            incVoltumStructureModificationCount()
                            return
                        }

                        PsiTreeChangeEvent.PROP_WRITABLE -> return
                        else -> event.element ?: return
                    }
                }

                else -> return
            }

            val file = event.file

            // if file is null, this is an event about VFS changes
            if (file == null) {
                val isStructureModification = element is VoltumFile && !isIgnorePsiEvents(element)
                        || element is PsiDirectory /*&& project.cargoProjects.findPackageForFile(element.virtualFile) != null*/
                if (isStructureModification) {
                    incVoltumStructureModificationCount(element as? VoltumFile, element as? VoltumFile)
                }
            } else {
                if (file.fileType != VoltumFileType.INSTANCE) return
                if (isIgnorePsiEvents(file)) return

                val isWhitespaceOrComment = element is PsiComment || element is PsiWhiteSpace
                if (isWhitespaceOrComment /*&& !isMacroExpansionModeNew*/) {
                    // Whitespace/comment changes are meaningful if new macro expansion engine is used
                    return
                }

                // Most of events means that some element *itself* is changed, but ChildrenChange means
                // that changed some of element's children, not the element itself. In this case
                // we should look up for ModificationTrackerOwner a bit differently
                val isChildrenChange = event is ChildrenChange || event is ChildRemoval.After

                updateModificationCount(file, element, isChildrenChange, isWhitespaceOrComment)
            }
        }

    }

    private fun updateModificationCount(
        file: PsiFile,
        psi: PsiElement,
        isChildrenChange: Boolean,
        isWhitespaceOrComment: Boolean
    ) {
        // We find the nearest parent item or macro call (because macro call can produce items)
        // If found item implements VoltumModificationTrackerOwner, we increment its own
        // modification counter. Otherwise, we increment global modification counter.
        //
        // So, if something is changed inside a function except an item, we will only
        // increment the function local modification counter.
        //
        // It may not be intuitive that if we change an item inside a function,
        // like this struct: `fn foo() { struct Bar; }`, we will increment the
        // global modification counter instead of function-local. We do not care
        // about it because it is a rare case and implementing it differently
        // is much more difficult.

        val owner = if (DumbService.isDumb(project)) null else psi.findModificationTrackerOwner(!isChildrenChange)


        val isStructureModification = owner == null || !owner.incModificationCount(psi)

        if (isStructureModification) {
            incVoltumStructureModificationCount(file, psi)
        }
        project.messageBus.syncPublisher(VOLTUM_PSI_CHANGE_TOPIC).voltumPsiChanged(file, psi, isStructureModification)
    }

    override fun incVoltumStructureModificationCount() =
        incVoltumStructureModificationCount(null, null)

    private fun incVoltumStructureModificationCount(file: PsiFile? = null, psi: PsiElement? = null) {
        voltumStructureModificationTracker.incModificationCount()
        if (!isWorkspaceFile(file)) {
            voltumStructureModificationTrackerInDependencies.incModificationCount()
        }
        project.messageBus.syncPublisher(VOLTUM_STRUCTURE_CHANGE_TOPIC).voltumStructureChanged(file, psi)
    }

    private fun isWorkspaceFile(file: PsiFile?): Boolean {
        if (file !is VoltumFile)
            return false
        
        return file.virtualFile != null
    }
}

val Project.voltumPsiManager: VoltumPsiManager get() = service()

/** @see VoltumPsiManager.voltumStructureModificationTracker */
val Project.voltumStructureModificationTracker: ModificationTracker
    get() = voltumPsiManager.voltumStructureModificationTracker


/**
 * Returns [VoltumPsiManager.voltumStructureModificationTracker] or [PsiModificationTracker.MODIFICATION_COUNT]
 * if `this` element is inside language injection
 */
val VoltumElement.voltumStructureOrAnyPsiModificationTracker: ModificationTracker
    get() {
        val containingFile = containingFile
        return when {
            // The case of injected language. Injected PSI doesn't have its own event system, so can only
            // handle evens from outer PSI. For example, Voltum language is injected to Kotlin's string
            // literal. If a user change the literal, we can only be notified that the literal is changed.
            // So we have to invalidate the cached value on any PSI change
            containingFile.virtualFile is VirtualFileWindow ->
                PsiManager.getInstance(containingFile.project).modificationTracker

            containingFile.containingVoltumFileSkippingCodeFragments != null ->
                containingFile.project.voltumStructureModificationTracker

            else -> containingFile.project.voltumPsiManager.voltumStructureModificationTrackerInDependencies
        }
    }