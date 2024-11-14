// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;

public interface VoltumLiteralExpr extends VoltumExpr, VoltumValueTypeElement, StubBasedPsiElement<VoltumPlaceholderStub<?>> {

  @NotNull
  VoltumLiteralValue getLiteralValue();

}
