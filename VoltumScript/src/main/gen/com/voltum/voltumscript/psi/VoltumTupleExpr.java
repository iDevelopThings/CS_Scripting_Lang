// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumTupleExpr extends VoltumExpr {

  @NotNull
  PsiElement getLparen();

  @Nullable
  PsiElement getRparen();

  @NotNull
  List<VoltumExpr> getElements();

}
