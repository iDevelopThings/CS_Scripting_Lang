// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.StubBasedPsiElement;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;

public interface VoltumAtomExpr extends VoltumExpr, StubBasedPsiElement<VoltumPlaceholderStub<?>> {

  @Nullable
  VoltumAnonymousFunc getAnonymousFunc();

  @Nullable
  VoltumDictionaryValue getDictionaryValue();

  @Nullable
  VoltumListValue getListValue();

  @Nullable
  VoltumPath getPath();

}
