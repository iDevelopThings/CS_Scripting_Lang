// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.voltum.voltumscript.psi.ext.VoltumInferenceContextOwner;
import com.intellij.psi.StubBasedPsiElement;
import com.intellij.navigation.ItemPresentation;

public interface VoltumTypeDeclaration extends VoltumDeclaration, VoltumInferenceContextOwner, StubBasedPsiElement<VoltumTypeDeclarationStub> {

  @NotNull
  List<VoltumAttribute> getAttributeList();

  @NotNull
  VoltumTypeId getTypeId();

  @Nullable
  PsiElement getEnumKw();

  @Nullable
  PsiElement getInterfaceKw();

  @Nullable
  PsiElement getStructKw();

  @NotNull
  PsiElement getTypeKw();

  @Nullable
  ItemPresentation getPresentation();

  @NotNull
  VoltumTypeDeclarationBody getBody();

}
