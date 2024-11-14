// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;

public interface VoltumDictionaryValue extends VoltumValueTypeElement, StubBasedPsiElement<VoltumDictionaryStub> {

  @NotNull
  List<VoltumDictionaryField> getDictionaryFieldList();

  @NotNull
  PsiElement getLcurly();

  @Nullable
  PsiElement getRcurly();

}
