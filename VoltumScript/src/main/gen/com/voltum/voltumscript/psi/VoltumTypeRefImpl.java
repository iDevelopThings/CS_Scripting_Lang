// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.voltum.voltumscript.lang.references.VoltumReference;
import com.voltum.voltumscript.lang.types.TyReference;
import com.intellij.psi.tree.IElementType;

public class VoltumTypeRefImpl extends VoltumTypeRefMixin implements VoltumTypeRef {

  public VoltumTypeRefImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitTypeRef(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public PsiElement getDotdotdot() {
    return findPsiChildByType(DOTDOTDOT);
  }

  @Override
  @Nullable
  public PsiElement getId() {
    return findPsiChildByType(ID);
  }

  @Override
  @Nullable
  public VoltumReference getReference() {
    return VoltumPsiUtilImpl.getReference(this);
  }

  @Override
  @Nullable
  public PsiElement getArray() {
    return findPsiChildByType(BRACKET_PAIR);
  }

  @Override
  @Nullable
  public VoltumTypeArgumentList getTypeArguments() {
    return PsiTreeUtil.getChildOfType(this, VoltumTypeArgumentList.class);
  }

}
