package com.voltum.voltumscript.runtime

import com.intellij.openapi.Disposable
import com.intellij.openapi.components.*
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.openapi.vfs.newvfs.BulkFileListener
import com.intellij.openapi.vfs.newvfs.events.VFileContentChangeEvent
import com.intellij.openapi.vfs.newvfs.events.VFileCreateEvent
import com.intellij.openapi.vfs.newvfs.events.VFileEvent
import com.intellij.util.messages.Topic


val Project.runtime: RuntimeManager get() = service()

interface VoltumLspRebuiltListener {
    fun lspRebuilt()
}

val VOLTUM_LSP_REBUILT_TOPIC: Topic<VoltumLspRebuiltListener> = Topic.create(
    "VOLTUM_LSP_REBUILT_TOPIC",
    VoltumLspRebuiltListener::class.java,
    Topic.BroadcastDirection.TO_PARENT
)

@Service(Service.Level.PROJECT)
class RuntimeManager(
    val project: Project
) : Disposable {
    companion object {
        val log = logger<RuntimeManager>()
    }

    val messageBus = project.messageBus.connect(this)

    fun startListener() {
        val lspFile = LocalFileSystem.getInstance().findFileByIoFile(service<RuntimeSettings>().getLspPath().toFile())
        if (lspFile == null) {
            log.warn("LSP exe path not found")
            return
        }

        messageBus.subscribe(VirtualFileManager.VFS_CHANGES, object : BulkFileListener {
            override fun after(events: MutableList<out VFileEvent>) {
                super.after(events)

                for (event in events) {
                    if (event.file?.path != lspFile.path)
                        continue

                    if (event is VFileContentChangeEvent || event is VFileCreateEvent) {
                        project.messageBus.syncPublisher(VOLTUM_LSP_REBUILT_TOPIC).lspRebuilt()
                    }
                }
            }
        })
    }

    override fun dispose() {
    }
}