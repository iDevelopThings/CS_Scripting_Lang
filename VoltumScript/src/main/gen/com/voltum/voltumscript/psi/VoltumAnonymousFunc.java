// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.navigation.ItemPresentation;

public interface VoltumAnonymousFunc extends VoltumFunction {

  @Nullable
  VoltumBlockBody getBlockBody();

  @Nullable
  VoltumStatement getStatement();

  @Nullable
  PsiElement getAsyncKw();

  @Nullable
  PsiElement getColon();

  @Nullable
  PsiElement getCoroutineKw();

  @Nullable
  PsiElement getFatArrow();

  @Nullable
  PsiElement getFuncKw();

  @NotNull
  PsiElement getLparen();

  @NotNull
  PsiElement getRparen();

  @Nullable
  ItemPresentation getPresentation();

  @Nullable
  VoltumTypeRef getReturnType();

  @NotNull
  List<VoltumIdentifierWithType> getArguments();

}
