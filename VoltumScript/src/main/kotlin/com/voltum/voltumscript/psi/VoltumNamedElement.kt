@file:Suppress("UNCHECKED_CAST")

package com.voltum.voltumscript.psi

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.navigation.ItemPresentation
import com.intellij.navigation.NavigationItem
import com.intellij.openapi.project.guessProjectDir
import com.intellij.openapi.util.TextRange
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.workspace.storage.impl.url.toVirtualFileUrl
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiNameIdentifierOwner
import com.intellij.psi.tree.IElementType
import com.intellij.psi.util.parentOfType
import com.voltum.voltumscript.lang.references.VoltumReference
import com.voltum.voltumscript.lang.types.TyKind
import com.voltum.voltumscript.lang.types.TyReference
import com.voltum.voltumscript.psi.ext.prototype
import com.voltum.voltumscript.psi.ext.startOffset
import java.net.URI

class UnknownNameElementException(element: PsiElement?, message: String? = null) : Exception(
    "Name identifier is null for element: $element" + if (message != null) " ($message)" else ""
)

interface VoltumNamedElement :
    VoltumElement,
    PsiNameIdentifierOwner,
    NavigationItem,
    PsiElementWithLookup {

    fun getNameId(): VoltumIdent?

    override fun getNameIdentifier(): PsiElement?

    /*fun getNameIdentifierRange(): TextRange? {
        if (nameIdentifier == null) return null
        val startOffset: Int = nameIdentifier!!.startOffset - textRange.startOffset
        return TextRange(startOffset, startOffset + nameIdentifier!!.textLength)
    }*/

    override fun getName(): String?
    override fun getPresentation(): ItemPresentation? = VoltumPsiUtilImpl.getPresentation(this)

    override fun getReference(): VoltumReference?
    override fun getReferences(): Array<VoltumReference>

    fun getNameStrict(): String = name ?: throw UnknownNameElementException(this)
}

abstract class VoltumNamedElementMixin : VoltumElementImpl, VoltumNamedElement {

    constructor(type: IElementType) : super(type)


    override fun getLookupElement(): LookupElement {
        TODO("Not yet implemented")
    }

    /*override fun getPresentation(): ItemPresentation? {
        return object : ItemPresentation {
            override fun getPresentableText() = name
            override fun getLocationString() = containingFile.name
            override fun getIcon(unused: Boolean): Icon {
                return when (this@VoltumNamedElementImpl) {
                    is ArcObjectId -> AllIcons.Nodes.Class
                    is ArcFuncId -> AllIcons.Nodes.Function
                    is ArcVarId -> AllIcons.Nodes.Variable
                    else -> AllIcons.Nodes.Type
                }
            }
        }
    }*/

    override fun getNameIdentifier() = VoltumPsiUtilImpl.getNameIdentifier(this)

    override fun getPresentation(): ItemPresentation? = VoltumPsiUtilImpl.getPresentation(this)
    
    override fun getName() = nameIdentifier?.text ?: throw IllegalStateException("Name identifier is null")
    override fun setName(name: String): PsiElement = VoltumPsiUtilImpl.setName(this, name)

    override fun getReference(): VoltumReference? = VoltumPsiUtilImpl.getReference(this)
    override fun getReferences(): Array<VoltumReference> = VoltumPsiUtilImpl.getReferences(this)
}

interface VoltumIdent : VoltumNamedElement {
    fun getId(): PsiElement?
    override fun getNameId(): VoltumIdent?
    override fun getNameIdentifier(): PsiElement? = this.node.findChildByType(VoltumTypes.ID)?.psi
        ?: throw UnknownNameElementException(this)

    override fun getPresentation(): ItemPresentation?
    
    val documentationUrl: String? get() {
        val projectDir = containingFile.project.guessProjectDir() ?: return null
        val file = containingFile.virtualFile ?: return null
        val offset = startOffset
        
//        val relativePath = file.path.substring(projectDir.path.length)
        
        val path = file.toNioPath().normalize()
//            .toVirtualFileUrl(
//            WorkspaceModel.getInstance(containingFile.project).getVirtualFileUrlManager()
//        )
        
        return "file/${path}#$offset"
    }
}

abstract class VoltumIdentifierMixin : VoltumNamedElementMixin, VoltumIdent {

    constructor(type: IElementType) : super(type)


    override fun getNameId(): VoltumIdent? = VoltumPsiUtilImpl.getNameId(this)
    override fun getNameIdentifier(): PsiElement? = this
    /*this.node.findChildByType(VoltumTypes.ID)?.psi
        ?: throw UnknownNameElementException(this)*/

}

/*
fun VoltumIdentifierReference.getId(): PsiElement = this.node.findChildByType(VoltumTypes.ID)?.psi ?: throw UnknownNameElementException(this)
val VoltumIdentifierReferenceImpl.id: PsiElement
    get() = this.node.findChildByType(VoltumTypes.ID)?.psi ?: throw UnknownNameElementException(this)
*/

interface VoltumReferenceElement : VoltumNamedElement {
    override fun getReference(): VoltumReference?
}

//val VoltumAccessExpr.pathParts: List<VoltumElement>
//    //    get() = PsiTreeUtil.findChildrenOfAnyType(this, VoltumIndexer::class.java, VoltumVarReference::class.java)
//    get() {
//        val parts = mutableListOf<VoltumElement>()
//        var current: PsiElement? = firstChild
//        while (current != null) {
//            if (current is VoltumReferenceElement || current is VoltumIndexer) {
//                parts.add(current as VoltumElement)
//            }
//            current = current.nextSibling
//        }
//        return parts
//    }
//
//val VoltumVarReference.pathParts: List<VoltumElement>
//    get() {
//        val parts = mutableListOf<VoltumElement>()
//        if (parent is VoltumPath) {
//            val p = (parent as VoltumPath).pathParts
//            for (seg in p) {
//                if (seg is VoltumReferenceElement || seg is VoltumIndexer)
//                    parts.add(seg)
//                if (seg == this)
//                    break
//            }
//        } else if (parent is VoltumAccessExpr) {
//            var current: PsiElement? = parent.firstChild
//            while (current != null) {
//                if (current is VoltumReferenceElement || current is VoltumIndexer) {
//                    parts.add(current as VoltumElement)
//                }
//                if (current == this)
//                    break
//
//                current = current.nextSibling
//            }
//        } else {
//            parts.add(this)
//        }
//
//        return parts
//    }

/** For `Foo::bar::baz::quux` path returns `Foo` */
//tailrec fun <T : VoltumReferenceElement> T.basePath(): T {
//    if (this is VoltumAccessExpr) {
//        val p = pathParts.firstOrNull() as T?
//        return if (p === null) this else p.basePath()
//    }
//    return this
//}

/** For `Foo::bar` in `Foo::bar::baz::quux` returns `Foo::bar::baz::quux` */
//fun VoltumReferenceElement.rootPath(): VoltumAccessExpr {
//    if (this is VoltumAccessExpr) {
//        return this
//    }
//
//    if (this is VoltumVarReference) {
//        return this.parentOfType<VoltumAccessExpr>()!!
//    }
//
//    throw IllegalStateException("Unhandled reference type in rootPath: $this")
//}

abstract class VoltumTypeRefMixin : VoltumIdentifierMixin, VoltumReferenceElement, VoltumTypeRef {
    constructor(type: IElementType) : super(type)

    override fun tryResolveType(): TyReference? {
        val idName = this.getId()?.text ?: return null
        val tyKind = TyKind.findByName(idName)
        if (tyKind != TyKind.Unknown) {
            return TyReference(tyKind)
        }

        this.reference?.resolve()?.let {
            return TyReference(it.prototype)
        }

        return null
    }
    // getId
}

