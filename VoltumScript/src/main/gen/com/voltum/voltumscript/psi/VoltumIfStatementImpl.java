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

public class VoltumIfStatementImpl extends VoltumElementImpl implements VoltumIfStatement {

  public VoltumIfStatementImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitIfStatement(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public VoltumBlockBody getBlockBody() {
    return PsiTreeUtil.getChildOfType(this, VoltumBlockBody.class);
  }

  @Override
  @Nullable
  public VoltumElseStatement getElseStatement() {
    return PsiTreeUtil.getChildOfType(this, VoltumElseStatement.class);
  }

  @Override
  @NotNull
  public VoltumExpr getExpr() {
    return PsiTreeUtil.getChildOfType(this, VoltumExpr.class);
  }

  @Override
  @NotNull
  public PsiElement getIfKw() {
    return findPsiChildByType(IF_KW);
  }

  @Override
  @NotNull
  public PsiElement getLparen() {
    return findPsiChildByType(LPAREN);
  }

  @Override
  @NotNull
  public PsiElement getRparen() {
    return findPsiChildByType(RPAREN);
  }

}
