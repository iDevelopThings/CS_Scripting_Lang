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

public class VoltumTypeDeclarationConstructorImpl extends VoltumTypeDeclarationMemberMixin implements VoltumTypeDeclarationConstructor {

  public VoltumTypeDeclarationConstructorImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitTypeDeclarationConstructor(this);
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
  @Nullable
  public VoltumBlockBody getBlockBody() {
    return PsiTreeUtil.getChildOfType(this, VoltumBlockBody.class);
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

  @Override
  @Nullable
  public PsiElement getSemicolon() {
    return findPsiChildByType(SEMICOLON);
  }

  @Override
  @Nullable
  public PsiElement getIsDef() {
    return findPsiChildByType(DEF_KW);
  }

  @Override
  @NotNull
  public VoltumFuncId getNameIdentifier() {
    return PsiTreeUtil.getChildOfType(this, VoltumFuncId.class);
  }

  @Override
  @NotNull
  public List<VoltumIdentifierWithType> getArguments() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumIdentifierWithType.class);
  }

  @Override
  @Nullable
  public ItemPresentation getPresentation() {
    return VoltumPsiUtilImpl.getPresentation(this);
  }

}
