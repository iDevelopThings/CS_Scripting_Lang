// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumForLoopStatement extends VoltumElement {

  @Nullable
  VoltumBlockBody getBlockBody();

  @NotNull
  List<VoltumExpr> getExprList();

  @Nullable
  VoltumVariableDeclaration getVariableDeclaration();

  @NotNull
  PsiElement getForKw();

  @Nullable
  PsiElement getLparen();

  @Nullable
  PsiElement getRparen();

}
