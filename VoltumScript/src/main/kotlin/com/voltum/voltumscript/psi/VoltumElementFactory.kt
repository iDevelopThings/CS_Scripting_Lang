package com.voltum.voltumscript.psi

import com.intellij.lang.ASTNode
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiFileFactory
import com.intellij.psi.util.childrenOfType
import com.voltum.voltumscript.lang.VoltumFileType
import com.voltum.voltumscript.psi.ext.childOfType

object VoltumElementFactory {

    fun createFile(project: Project?, text: String?, name: String = "dummy.simple"): VoltumFile {
        return text?.let {
            PsiFileFactory.getInstance(project).createFileFromText(name, VoltumFileType.INSTANCE, it)
        } as VoltumFile
    }

    fun createIdentifier(project: Project?, name: String?): VoltumNamedElement {
        val file = createFile(project, "var $name = 0;")
        return file.firstChild.childrenOfType<VoltumNamedElement>().first()
    }

    fun createId(project: Project?, name: String?): ASTNode {
        val ident = createIdentifier(project, name)
        return ident.node.findChildByType(VoltumTypes.ID)!!
    }

    fun createStructDeclaration(project: Project?, name: String?): VoltumTypeDeclaration? {
        val file = createFile(project, "type $name struct {}")
        return file.childOfType<VoltumTypeDeclaration>()
    }
    
    fun createStructDeclaration(project: Project?, name: String?, bodyWriter: StringBuilder.() -> Unit): VoltumTypeDeclaration? {
        val body = StringBuilder().apply(bodyWriter).toString()
        val file = createFile(project, "type $name struct {\n$body\n}")
        
        return file.childOfType<VoltumTypeDeclaration>()
    }

    fun createStructMemberProperty(project: Project?, name: String, type: String, documentation: String? = null): VoltumTypeDeclarationFieldMember? {
        return createStructMemberProperty(project, "$name $type", documentation)
    }

    fun createStructMemberProperty(project: Project?, def: String, documentation: String? = null): VoltumTypeDeclarationFieldMember? {
        val struct = createStructDeclaration(project, "Foo", bodyWriter = {
            documentation?.let { append("/**\n$it\n*/\n") }
            append(def)
        })
        return struct?.fields?.firstOrNull()
    }

    fun createStructMemberMethodDef(
        project: Project?,
        name: String,
        params: List<Pair<String, String>>,
        returnType: String?,
        documentation: String? = null
    ): VoltumTypeDeclarationMethodMember? {
        return createStructMemberMethodDef(
            project,
            "def $name(${params.joinToString(", ") { (name, type) -> "$type $name" }}) ${returnType ?: "void"}",
            documentation
        )
    }

    fun createStructMemberMethodDef(
        project: Project?,
        def: String,
        documentation: String? = null
    ): VoltumTypeDeclarationMethodMember? {
        val struct = createStructDeclaration(project, "Foo", bodyWriter = {
            documentation?.let { append("/**\n$it\n*/\n") }
            append(def)
        })
        return struct?.methods?.firstOrNull()
    }

    fun createStructConstructorDef(
        project: Project?,
        def: String,
        documentation: String? = null
    ): VoltumTypeDeclarationConstructor? {
        val struct = createStructDeclaration(project, "Foo", bodyWriter = {
            documentation?.let { append("/**\n$it\n*/\n") }
            append(def)
        })
        return struct?.constructors?.firstOrNull()
    }

}