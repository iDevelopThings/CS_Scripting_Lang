// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.intellij.psi.tree.IElementType;

public class VoltumForLoopStatementImpl extends VoltumElementImpl implements VoltumForLoopStatement {

  public VoltumForLoopStatementImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitForLoopStatement(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public VoltumBlockBody getBlockBody() {
    return PsiTreeUtil.getChildOfType(this, VoltumBlockBody.class);
  }

  @Override
  @NotNull
  public List<VoltumExpr> getExprList() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumExpr.class);
  }

  @Override
  @Nullable
  public VoltumVariableDeclaration getVariableDeclaration() {
    return PsiTreeUtil.getChildOfType(this, VoltumVariableDeclaration.class);
  }

  @Override
  @NotNull
  public PsiElement getForKw() {
    return findPsiChildByType(FOR_KW);
  }

  @Override
  @Nullable
  public PsiElement getLparen() {
    return findPsiChildByType(LPAREN);
  }

  @Override
  @Nullable
  public PsiElement getRparen() {
    return findPsiChildByType(RPAREN);
  }

}
