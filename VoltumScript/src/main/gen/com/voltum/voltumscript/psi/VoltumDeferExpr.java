// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumDeferExpr extends VoltumExpr {

  @Nullable
  VoltumAnonymousFunc getAnonymousFunc();

  @Nullable
  VoltumBlockBody getBlockBody();

  @NotNull
  PsiElement getDeferKw();

}
