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

public class VoltumBlockBodyImpl extends VoltumElementImpl implements VoltumBlockBody {

  public VoltumBlockBodyImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitBlockBody(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public List<VoltumExpr> getExprList() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumExpr.class);
  }

  @Override
  @NotNull
  public List<VoltumStatement> getStatementList() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumStatement.class);
  }

  @Override
  @NotNull
  public PsiElement getLcurly() {
    return findPsiChildByType(LCURLY);
  }

  @Override
  @Nullable
  public PsiElement getRcurly() {
    return findPsiChildByType(RCURLY);
  }

}
