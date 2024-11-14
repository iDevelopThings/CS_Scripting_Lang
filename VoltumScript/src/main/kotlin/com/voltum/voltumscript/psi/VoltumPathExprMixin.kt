package com.voltum.voltumscript.psi

import com.intellij.lang.ASTNode
import com.intellij.navigation.ItemPresentation
import com.intellij.psi.PsiElement
import com.intellij.psi.stubs.IStubElementType
import com.intellij.psi.util.PsiTreeUtil
import com.voltum.voltumscript.ext.VoltumQualifiedName
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub
import com.voltum.voltumscript.lang.stubs.VoltumStubbedElementImpl

interface VoltumQualifiedReferenceElement : VoltumReferenceElement {
    val qualifier: PsiElement?
    val qualifiedPath: VoltumQualifiedName
}

abstract class VoltumPathExprMixin : VoltumStubbedElementImpl<VoltumPlaceholderStub<*>>, VoltumPath {
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    val previewLeftMostQualifier get() = leftMostQualifier()?.text
    val previewTopMostPathParent get() = getTopMostPathParent()?.text
    val previewPathParts get() = getPathParts().map { it.text }.joinToString(".")

    override fun getLastVarReference(): VoltumVarReference? {
        var currentPath = this as VoltumPath?

        while (currentPath != null) {
            val vr = PsiTreeUtil.getStubChildOfType(currentPath, VoltumVarReference::class.java)
            if(vr != null)
                return vr
            currentPath = currentPath.path
        }
        
        return null
    }
    override val qualifier get(): VoltumPath? = findChildByType(VoltumTypes.PATH)
//    override fun getNameId(): VoltumIdent? = findChildByType(VoltumTypes.VAR_REFERENCE)
    override fun getNameId(): VoltumIdent? = getLastVarReference()
    override fun getNameIdentifier(): PsiElement? = getNameId()?.getId()
    override fun getPresentation(): ItemPresentation? = VoltumPsiUtilImpl.getPresentation(this)
    override fun getName() = nameIdentifier?.text ?: throw IllegalStateException("Name identifier is null")
    override fun setName(name: String): PsiElement = VoltumPsiUtilImpl.setName(this, name)
    override fun getLookupElement() = VoltumPsiUtilImpl.getLookupElement(this)

    override val qualifiedPath: VoltumQualifiedName
        get() {
            val qualifiedName = VoltumQualifiedName.fromDottedString(text)
            return qualifiedName
        }

    override fun leftMostQualifier(): PsiElement? {
        var result: PsiElement = this
        while (result is VoltumQualifiedReferenceElement) {
            val child = result.qualifier
            if (child != null) {
                result = child
            } else {
                return result
            }
        }
        return result
    }

    override fun getTopMostPathParent(): PsiElement? {
        return PsiTreeUtil.skipParentsOfType(this, VoltumPath::class.java)
    }

    override fun getQualifiers() = PsiTreeUtil.getChildrenOfType(this, VoltumPath::class.java)

    override fun getPathParts(): List<VoltumElement> {
        val parts = mutableListOf<VoltumElement>()
        var current: PsiElement = this
        while (current is VoltumPath) {
            val nameId = current.getNameId()
            val qualifier = current.qualifier
            if (nameId != null) {
                parts.add(nameId as VoltumElement)
            }
            current = qualifier ?: break
        }
        return parts.reversed()
    }


    override fun toString(): String {
        var str = "PathExpr("

        if (qualifier != null)
            str += "q: '$qualifier', "

        str += "r: '${nameIdentifier?.text}', "

        getPathParts().map { it.text }.joinToString(".").takeIf {
            it.isNotEmpty() && it != nameIdentifier?.text
        }.let {
            if (it != null) {
                str += "p: '$it', "
            }
        }

        typeArgumentList?.takeIf { it.typeRefList.isNotEmpty() }?.let {
            str += "t: <${it.typeRefList.joinToString(", ") { it.text }}>"
        }

        return "$str)"
    }
}

//fun VoltumCallExpr.getNameId(): VoltumIdent? = findChildByType(VoltumTypes.VAR_REFERENCE)

abstract class VoltumCallExprMixin : VoltumExprImpl, VoltumCallExpr {
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override val requiresTypeSubstitution: Boolean
        get() = true // typeArguments.isNotEmpty() == true

    override fun getNameId(): VoltumIdent? = findChildByType(VoltumTypes.VAR_REFERENCE)
    override fun getNameIdentifier(): PsiElement? = getNameId()?.getId()

    override fun getArguments(): List<VoltumExpr> = exprList

    override fun getTypeArguments(): List<VoltumTypeRef> {
        val args = mutableListOf<VoltumTypeRef>()

        if (path != null) {
            args.addAll(path!!.typeArgumentList?.typeRefList ?: emptyList())
        }

        val directTypeArgs = PsiTreeUtil.getChildOfType(this, VoltumTypeArgumentList::class.java);
        if (directTypeArgs != null) {
            args.addAll(directTypeArgs.typeRefList)
        }

        return args
    }

    override fun toString(): String {
        var str = "CallExpr = "
        str += qualifier?.text
//        if (typeArguments.isNotEmpty())
//            str += "<${typeArguments.joinToString(",") { it.text }}>"

        str += "("

        str += arguments.joinToString(", ") { it.text } 

        str += ")"

        return str
    }
}

