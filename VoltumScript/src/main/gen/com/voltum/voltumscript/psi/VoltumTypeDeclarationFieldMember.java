// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.navigation.ItemPresentation;

public interface VoltumTypeDeclarationFieldMember extends VoltumElement {

  @NotNull
  List<VoltumAttribute> getAttributeList();

  @NotNull
  VoltumTypeRef getTypeRef();

  @NotNull
  VoltumVarId getVarId();

  @Nullable
  PsiElement getSemicolon();

  @Nullable
  ItemPresentation getPresentation();

}
