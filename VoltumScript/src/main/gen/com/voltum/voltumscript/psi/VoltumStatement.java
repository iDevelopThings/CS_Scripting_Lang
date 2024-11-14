// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.intellij.psi.ResolveState;
import com.intellij.psi.scope.PsiScopeProcessor;

public interface VoltumStatement extends VoltumBaseStatement {

  @Nullable
  VoltumForLoopStatement getForLoopStatement();

  @Nullable
  VoltumIfStatement getIfStatement();

  @Nullable
  VoltumReturnExpr getReturnExpr();

  @Nullable
  VoltumVariableDeclaration getVariableDeclaration();

  boolean processDeclarations(@NotNull PsiScopeProcessor processor, @NotNull ResolveState state, @NotNull PsiElement place);

}
