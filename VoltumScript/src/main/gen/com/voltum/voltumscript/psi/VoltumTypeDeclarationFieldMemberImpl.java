// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.intellij.navigation.ItemPresentation;
import com.intellij.psi.tree.IElementType;

public class VoltumTypeDeclarationFieldMemberImpl extends VoltumTypeDeclarationMemberMixin implements VoltumTypeDeclarationFieldMember {

  public VoltumTypeDeclarationFieldMemberImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitTypeDeclarationFieldMember(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public List<VoltumAttribute> getAttributeList() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumAttribute.class);
  }

  @Override
  @NotNull
  public VoltumTypeRef getTypeRef() {
    return PsiTreeUtil.getChildOfType(this, VoltumTypeRef.class);
  }

  @Override
  @NotNull
  public VoltumVarId getVarId() {
    return PsiTreeUtil.getChildOfType(this, VoltumVarId.class);
  }

  @Override
  @Nullable
  public PsiElement getSemicolon() {
    return findPsiChildByType(SEMICOLON);
  }

  @Override
  @Nullable
  public ItemPresentation getPresentation() {
    return VoltumPsiUtilImpl.getPresentation(this);
  }

}
