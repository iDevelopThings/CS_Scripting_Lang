// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumPostfixExpr extends VoltumExpr {

  @NotNull
  VoltumExpr getExpr();

  @Nullable
  PsiElement getMinusminus();

  @Nullable
  PsiElement getPlusplus();

  @NotNull
  String toDisplayString();

  @NotNull
  PsiElement getOperator();

}
