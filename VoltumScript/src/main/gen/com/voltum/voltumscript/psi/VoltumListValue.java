// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;

public interface VoltumListValue extends VoltumValueTypeElement, StubBasedPsiElement<VoltumPlaceholderStub<?>> {

  @NotNull
  List<VoltumExpr> getExprList();

  @NotNull
  PsiElement getLbrack();

  @Nullable
  PsiElement getRbrack();

}
