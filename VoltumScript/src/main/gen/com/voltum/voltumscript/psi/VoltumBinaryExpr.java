// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumBinaryExpr extends VoltumExpr {

  @NotNull
  List<VoltumExpr> getExprList();

  @NotNull
  VoltumExpr getLeft();

  @Nullable
  VoltumExpr getRight();

  @NotNull
  VoltumBinaryOp getOperator();

  @NotNull
  String toDisplayString();

}
