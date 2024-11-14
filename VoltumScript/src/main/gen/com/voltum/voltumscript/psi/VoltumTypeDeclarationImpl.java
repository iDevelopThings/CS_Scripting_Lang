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
import com.intellij.psi.stubs.IStubElementType;

public class VoltumTypeDeclarationImpl extends VoltumTypeDeclarationMixin implements VoltumTypeDeclaration {

  public VoltumTypeDeclarationImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumTypeDeclarationImpl(@NotNull VoltumTypeDeclarationStub stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitTypeDeclaration(this);
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
  public VoltumTypeId getTypeId() {
    return notNullChild(PsiTreeUtil.getChildOfType(this, VoltumTypeId.class));
  }

  @Override
  @Nullable
  public PsiElement getEnumKw() {
    return findChildByType(ENUM_KW);
  }

  @Override
  @Nullable
  public PsiElement getInterfaceKw() {
    return findChildByType(INTERFACE_KW);
  }

  @Override
  @Nullable
  public PsiElement getStructKw() {
    return findChildByType(STRUCT_KW);
  }

  @Override
  @NotNull
  public PsiElement getTypeKw() {
    return notNullChild(findChildByType(TYPE_KW));
  }

  @Override
  @Nullable
  public ItemPresentation getPresentation() {
    return VoltumPsiUtilImpl.getPresentation(this);
  }

  @Override
  @NotNull
  public VoltumTypeDeclarationBody getBody() {
    return notNullChild(PsiTreeUtil.getChildOfType(this, VoltumTypeDeclarationBody.class));
  }

}
