// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.intellij.psi.ResolveState;
import com.intellij.psi.scope.PsiScopeProcessor;
import com.intellij.psi.tree.IElementType;

public class VoltumStatementImpl extends VoltumBaseStatementImpl implements VoltumStatement {

  public VoltumStatementImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitStatement(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public VoltumForLoopStatement getForLoopStatement() {
    return PsiTreeUtil.getChildOfType(this, VoltumForLoopStatement.class);
  }

  @Override
  @Nullable
  public VoltumIfStatement getIfStatement() {
    return PsiTreeUtil.getChildOfType(this, VoltumIfStatement.class);
  }

  @Override
  @Nullable
  public VoltumReturnExpr getReturnExpr() {
    return PsiTreeUtil.getChildOfType(this, VoltumReturnExpr.class);
  }

  @Override
  @Nullable
  public VoltumVariableDeclaration getVariableDeclaration() {
    return PsiTreeUtil.getChildOfType(this, VoltumVariableDeclaration.class);
  }

  @Override
  public boolean processDeclarations(@NotNull PsiScopeProcessor processor, @NotNull ResolveState state, @NotNull PsiElement place) {
    return VoltumPsiUtilImpl.processDeclarations(this, processor, state, place);
  }

}
