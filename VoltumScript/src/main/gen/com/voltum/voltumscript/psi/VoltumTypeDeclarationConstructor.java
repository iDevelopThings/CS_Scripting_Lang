// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.navigation.ItemPresentation;

public interface VoltumTypeDeclarationConstructor extends VoltumElement {

  @NotNull
  List<VoltumAttribute> getAttributeList();

  @Nullable
  VoltumBlockBody getBlockBody();

  @NotNull
  PsiElement getLparen();

  @NotNull
  PsiElement getRparen();

  @Nullable
  PsiElement getSemicolon();

  @Nullable
  PsiElement getIsDef();

  @NotNull
  VoltumFuncId getNameIdentifier();

  @NotNull
  List<VoltumIdentifierWithType> getArguments();

  @Nullable
  ItemPresentation getPresentation();

}
