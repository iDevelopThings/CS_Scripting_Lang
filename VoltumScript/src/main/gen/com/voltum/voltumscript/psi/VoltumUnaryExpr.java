// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumUnaryExpr extends VoltumExpr {

  @Nullable
  VoltumExpr getExpr();

  @Nullable
  PsiElement getAnd();

  @Nullable
  PsiElement getExcl();

  @Nullable
  PsiElement getMinus();

  @Nullable
  PsiElement getMul();

  @Nullable
  PsiElement getPlus();

}
