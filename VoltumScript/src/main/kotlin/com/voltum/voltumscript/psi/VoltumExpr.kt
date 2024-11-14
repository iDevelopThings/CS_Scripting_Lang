package com.voltum.voltumscript.psi

import com.intellij.lang.ASTNode
import com.intellij.navigation.ItemPresentation
import com.intellij.psi.PsiElement
import com.intellij.psi.stubs.IStubElementType
import com.intellij.psi.tree.IElementType
import com.intellij.psi.util.PsiTreeUtil
import com.voltum.voltumscript.lang.references.VoltumReference
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub
import com.voltum.voltumscript.lang.stubs.VoltumStubbedElementImpl
import com.voltum.voltumscript.lang.types.Ty
import com.voltum.voltumscript.lang.types.TyKind
import com.voltum.voltumscript.lang.types.TyObject
import com.voltum.voltumscript.lang.types.tryResolveType
import com.voltum.voltumscript.parser.PREFIX_AND_POSTFIX_OPERATORS
import com.voltum.voltumscript.parser.VoltumTokenSets

//interface VoltumExpr : VoltumElement {
//    val requiresTypeSubstitution: Boolean get() = false
//}


abstract class VoltumExprMixin : VoltumStubbedElementImpl<VoltumPlaceholderStub<*>>, VoltumExpr {
    //abstract class VoltumExprMixin : VoltumElementImpl, VoltumExpr {
//    constructor(el: IElementType) : super(el)
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override fun getReference(): VoltumReference? = VoltumPsiUtilImpl.getReference(this)
    override fun getReferences(): Array<VoltumReference> = VoltumPsiUtilImpl.getReferences(this)

    open val requiresTypeSubstitution: Boolean
        get() = false

    override fun toString(): String {
        return javaClass.simpleName + "(" + elementType + ")"
    }
}

abstract class VoltumBinaryExprMixin : VoltumExprImpl, VoltumBinaryExpr {
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override fun toDisplayString(): String = "(${left.text} ${operator.toDisplayString()} ${right?.text})"

    override fun toString(): String {
        return javaClass.simpleName + "(" + elementType + ") = ${toDisplayString()}"
    }
}

abstract class VoltumPrefixExprMixin : VoltumExprImpl, VoltumPrefixExpr {
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override fun getOperator(): PsiElement = notNullChild(findChildByType(PREFIX_AND_POSTFIX_OPERATORS))
    override fun toDisplayString(): String = "( ${getOperator().text} ${expr.text})"

    override fun toString(): String {
        return javaClass.simpleName + "(" + elementType + ") = ${toDisplayString()}"
    }
}

abstract class VoltumPostfixExprMixin : VoltumExprImpl, VoltumPostfixExpr {
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)
    
    override fun getOperator(): PsiElement = notNullChild(findChildByType(PREFIX_AND_POSTFIX_OPERATORS))
    override fun toDisplayString(): String = "( ${expr.text} ${getOperator().text} )"

    override fun toString(): String {
        return javaClass.simpleName + "(" + elementType + ") = ${toDisplayString()}"
    }
}

abstract class VoltumBinaryOpMixin : VoltumElementImpl, VoltumBinaryOp {
    constructor(type: IElementType) : super(type)

    override fun toDisplayString(): String = text

    override fun toString(): String {
        return javaClass.simpleName + "(" + elementType + ") = ${toDisplayString()}"
    }
}


abstract class VoltumExprListMixin : VoltumStubbedElementImpl<VoltumPlaceholderStub<*>>, VoltumElement {
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    fun getExpressions(): List<VoltumExpr> = PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumExpr::class.java)
}


/*

abstract class VoltumUnaryExprMixin : VoltumExprMixin, VoltumUnaryExpr {
    
    constructor(el: IElementType) : super(el)
    //constructor(node: ASTNode) : super(node)
    //constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)
}

abstract class VoltumBinaryExprMixin : VoltumExprMixin, VoltumBinaryExpr {
    constructor(el: IElementType) : super(el)
    //constructor(node: ASTNode) : super(node)
    //constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)
}

abstract class VoltumConditionalExprMixin : VoltumExprMixin, VoltumConditionalExpr {
    constructor(el: IElementType) : super(el)
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)
}

abstract class VoltumAssignExprMixin : VoltumExprMixin, VoltumAssignExpr {
    
    constructor(el: IElementType) : super(el)
    //constructor(node: ASTNode) : super(node)
    //constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)
}
*/

abstract class VoltumRefExprMixin : /*VoltumStubbedElementImpl<VoltumPlaceholderStub<*>>*/
    VoltumExprMixin, VoltumReferenceElement {


    //    constructor(el: IElementType) : super(el)
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override fun getNameId(): VoltumIdent? = VoltumPsiUtilImpl.getNameId(this)
    override fun getNameIdentifier(): PsiElement? = VoltumPsiUtilImpl.getNameIdentifier(this)
    override fun getPresentation(): ItemPresentation? = VoltumPsiUtilImpl.getPresentation(this)

    override fun setName(name: String): PsiElement {
        TODO("Not yet implemented")
    }

    override fun getLookupElement() = VoltumPsiUtilImpl.getLookupElement(this)


}

abstract class VoltumLiteralMixin : VoltumStubbedElementImpl<VoltumPlaceholderStub<*>>, VoltumLiteralValue {
    //    constructor(el: IElementType) : super(el)
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override var prototype: Ty
        get() = tryResolveType() ?: throw Exception("Cannot resolve type for literal")
        set(_) {
            throw Exception("Cannot set prototype for literal")
        }

    override fun getStringLiteral(): PsiElement = notNullChild(findChildByType(VoltumTypes.STRING_LITERAL));
    override fun getValueBool(): PsiElement = notNullChild(findChildByType(VoltumTypes.VALUE_BOOL));
    override fun getValueFloat(): PsiElement = notNullChild(findChildByType(VoltumTypes.VALUE_FLOAT));
    override fun getValueInteger(): PsiElement = notNullChild(findChildByType(VoltumTypes.VALUE_INTEGER));
    override fun getValueNull(): PsiElement = notNullChild(findChildByType(VoltumTypes.VALUE_NULL));


    override fun toString(): String {
        return javaClass.simpleName + "(" + elementType + ")"
    }
}

abstract class VoltumListMixin : VoltumLiteralMixin, VoltumListValue {
    //    constructor(el: IElementType) : super(el)
    constructor(node: ASTNode) : super(node)
    constructor(stub: VoltumPlaceholderStub<*>, elementType: IStubElementType<*, *>) : super(stub, elementType)

    override var prototype: Ty
        get() = TyObject.INSTANCE
        set(_) {
            throw Exception("Cannot set prototype for literal number")
        }
}


interface VoltumValueTypeElement : VoltumElement {
    var prototype: Ty
    val literalType: TyKind get() = TyKind.findType(this)
}


//interface VoltumLiteralExpr : VoltumExpr, VoltumValueTypeElement {
//}
