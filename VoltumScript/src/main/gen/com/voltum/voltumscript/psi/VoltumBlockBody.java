// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumBlockBody extends VoltumElement {

  @NotNull
  List<VoltumExpr> getExprList();

  @NotNull
  List<VoltumStatement> getStatementList();

  @NotNull
  PsiElement getLcurly();

  @Nullable
  PsiElement getRcurly();

}
