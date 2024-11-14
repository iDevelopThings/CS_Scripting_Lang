// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.voltum.voltumscript.psi.ext.VoltumInferenceContextOwner;
import com.intellij.psi.StubBasedPsiElement;
import com.intellij.navigation.ItemPresentation;

public interface VoltumFuncDeclaration extends VoltumFunction, VoltumInferenceContextOwner, StubBasedPsiElement<VoltumFunctionStub> {

  @NotNull
  List<VoltumAttribute> getAttributeList();

  @Nullable
  VoltumBlockBody getBlockBody();

  @NotNull
  VoltumFuncId getFuncId();

  @Nullable
  PsiElement getAsyncKw();

  @Nullable
  PsiElement getCoroutineKw();

  @Nullable
  PsiElement getDefKw();

  @NotNull
  PsiElement getFuncKw();

  @Nullable
  PsiElement getLparen();

  @Nullable
  PsiElement getRparen();

  @Nullable
  ItemPresentation getPresentation();

  @Nullable
  VoltumTypeRef getReturnType();

  @NotNull
  List<VoltumIdentifierWithType> getArguments();

  @Nullable
  VoltumTypeArgumentList getTypeArguments();

}
