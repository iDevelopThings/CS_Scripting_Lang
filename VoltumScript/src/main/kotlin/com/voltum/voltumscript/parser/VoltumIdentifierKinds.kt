package com.voltum.voltumscript.parser

import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.psi.VoltumElementType
import com.voltum.voltumscript.psi.VoltumTypes


enum class VoltumIdentifierKinds(
    val kind: String,
    val elementType: IElementType
) {
    UntypedIdentifier("untyped_identifier", VoltumTypes.ID),
    VarId("var_id", VoltumTypes.VAR_ID),
    TypeId("type_id", VoltumTypes.TYPE_ID),
    ArgumentId("argument_id", VoltumTypes.ARGUMENT_ID),
    FuncId("func_id", VoltumTypes.FUNC_ID),
    FieldId("field_id", VoltumTypes.FIELD_ID),
    VarReference("var_reference", VoltumTypes.VAR_REFERENCE),
    ;
    
    companion object {
        fun findByKind(kind: String): VoltumIdentifierKinds? {
            return values().find { it.kind == kind }
        }
        fun findByElementType(elementType: IElementType): VoltumIdentifierKinds? {
            return values().find { it.elementType == elementType }
        }
    }

}