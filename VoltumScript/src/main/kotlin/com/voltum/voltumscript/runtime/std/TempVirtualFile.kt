package com.voltum.voltumscript.runtime.std

import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.testFramework.LightVirtualFile
import com.voltum.voltumscript.lang.VoltumFileType
import java.nio.file.Path

open class TempVirtualFile(
    val rootPath: Path,
    name: String,
    fileType: VoltumFileType,
    content: String
) : LightVirtualFile(name, fileType, content) {
    init {
        isWritable = false
    }

    override fun getPresentableName(): String {
        return super.getPresentableName()
    }
    override fun getUrl(): String {
        return VirtualFileManager.constructUrl(fileSystem.protocol, path)
    }
    override fun getPath(): String {
        var p = name
        if(parent?.path != null) {
            p = parent.path + "/" + name
        }
        
        var np = rootPath.resolve("libraries").resolve(p)
        return np.toString()
    }
}