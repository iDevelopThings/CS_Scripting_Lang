// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.voltum.voltumscript.lang.stubs.VoltumStubbedElementImpl;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;
import com.intellij.psi.stubs.IStubElementType;

public class VoltumSignalDeclarationImpl extends VoltumStubbedElementImpl<VoltumPlaceholderStub<?>> implements VoltumSignalDeclaration {

  public VoltumSignalDeclarationImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumSignalDeclarationImpl(@NotNull VoltumPlaceholderStub<?> stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitSignalDeclaration(this);
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
  public List<VoltumIdentifierWithType> getIdentifierWithTypeList() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumIdentifierWithType.class);
  }

  @Override
  @Nullable
  public PsiElement getId() {
    return findChildByType(ID);
  }

  @Override
  @Nullable
  public PsiElement getLparen() {
    return findChildByType(LPAREN);
  }

  @Override
  @Nullable
  public PsiElement getRparen() {
    return findChildByType(RPAREN);
  }

  @Override
  @NotNull
  public PsiElement getSignalKw() {
    return notNullChild(findChildByType(SIGNAL_KW));
  }

}
