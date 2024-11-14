// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;

public interface VoltumDictionaryField extends VoltumElement, StubBasedPsiElement<VoltumDictionaryFieldStub> {

  @NotNull
  VoltumFieldId getFieldId();

  @NotNull
  PsiElement getColon();

  @NotNull
  VoltumExpr getValue();

}
