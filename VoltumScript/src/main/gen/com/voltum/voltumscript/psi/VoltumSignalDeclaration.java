// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.voltum.voltumscript.psi.ext.VoltumInferenceContextOwner;
import com.intellij.psi.StubBasedPsiElement;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;

public interface VoltumSignalDeclaration extends VoltumInferenceContextOwner, StubBasedPsiElement<VoltumPlaceholderStub<?>> {

  @NotNull
  List<VoltumAttribute> getAttributeList();

  @NotNull
  List<VoltumIdentifierWithType> getIdentifierWithTypeList();

  @Nullable
  PsiElement getId();

  @Nullable
  PsiElement getLparen();

  @Nullable
  PsiElement getRparen();

  @NotNull
  PsiElement getSignalKw();

}
