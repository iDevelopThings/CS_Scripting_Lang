// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;
import com.intellij.navigation.ItemPresentation;

public interface VoltumVariableDeclaration extends VoltumReferenceElement, StubBasedPsiElement<VoltumVariableDeclarationStub> {

  @NotNull
  List<VoltumExpr> getExprList();

  @NotNull
  List<VoltumVarId> getVarIdList();

  @Nullable
  PsiElement getEq();

  @NotNull
  PsiElement getVarKw();

  @Nullable
  ItemPresentation getPresentation();

  @Nullable
  VoltumIdent getNameId();

  @Nullable
  PsiElement getNameIdentifier();

  @NotNull
  VoltumVarId getVarName();

  @Nullable
  VoltumExpr getInitializer();

  @Nullable
  List<VoltumExpr> getInitializers();

}
