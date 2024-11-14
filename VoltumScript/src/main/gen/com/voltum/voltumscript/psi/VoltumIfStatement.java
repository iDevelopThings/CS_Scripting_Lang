// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumIfStatement extends VoltumElement {

  @NotNull
  VoltumBlockBody getBlockBody();

  @Nullable
  VoltumElseStatement getElseStatement();

  @NotNull
  VoltumExpr getExpr();

  @NotNull
  PsiElement getIfKw();

  @NotNull
  PsiElement getLparen();

  @NotNull
  PsiElement getRparen();

}
