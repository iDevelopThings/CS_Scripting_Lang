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

public class VoltumTypeDeclarationBodyImpl extends VoltumElementImpl implements VoltumTypeDeclarationBody {

  public VoltumTypeDeclarationBodyImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitTypeDeclarationBody(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
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

  @Override
  @NotNull
  public List<VoltumTypeDeclarationFieldMember> getFields() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumTypeDeclarationFieldMember.class);
  }

  @Override
  @NotNull
  public List<VoltumTypeDeclarationMethodMember> getMethods() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumTypeDeclarationMethodMember.class);
  }

  @Override
  @NotNull
  public List<VoltumTypeDeclarationConstructor> getConstructors() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumTypeDeclarationConstructor.class);
  }

}
