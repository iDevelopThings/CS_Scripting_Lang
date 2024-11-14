// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;

public interface VoltumLiteralValue extends VoltumValueTypeElement, StubBasedPsiElement<VoltumPlaceholderStub<?>> {

  @Nullable
  PsiElement getStringLiteral();

  @Nullable
  PsiElement getValueBool();

  @Nullable
  PsiElement getValueFloat();

  @Nullable
  PsiElement getValueInteger();

  @Nullable
  PsiElement getValueNull();

}
