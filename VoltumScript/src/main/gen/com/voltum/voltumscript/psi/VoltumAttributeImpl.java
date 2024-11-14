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

public class VoltumAttributeImpl extends VoltumElementImpl implements VoltumAttribute {

  public VoltumAttributeImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitAttribute(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public VoltumCallExpr getCallExpr() {
    return PsiTreeUtil.getChildOfType(this, VoltumCallExpr.class);
  }

  @Override
  @Nullable
  public PsiElement getId() {
    return findPsiChildByType(ID);
  }

  @Override
  @NotNull
  public PsiElement getLbrack() {
    return findPsiChildByType(LBRACK);
  }

  @Override
  @NotNull
  public PsiElement getRbrack() {
    return findPsiChildByType(RBRACK);
  }

}
