// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;
import com.intellij.codeInsight.lookup.LookupElement;
import com.intellij.navigation.ItemPresentation;

public interface VoltumCallExpr extends VoltumExpr, VoltumReferenceElement, StubBasedPsiElement<VoltumPlaceholderStub<?>> {

  @NotNull
  List<VoltumExpr> getExprList();

  @Nullable
  VoltumPath getPath();

  @Nullable
  VoltumTypeArgumentList getTypeArgumentList();

  @NotNull
  PsiElement getLparen();

  @NotNull
  PsiElement getRparen();

  @Nullable
  VoltumPath getQualifier();

  @NotNull
  List<VoltumExpr> getArguments();

  @NotNull
  List<VoltumTypeRef> getTypeArguments();

  @Nullable
  VoltumIdent getNameId();

  @Nullable
  PsiElement getNameIdentifier();

  @Nullable
  ItemPresentation getPresentation();

  @NotNull
  PsiElement setName(@NotNull String name);

  @Nullable
  LookupElement getLookupElement();

}
