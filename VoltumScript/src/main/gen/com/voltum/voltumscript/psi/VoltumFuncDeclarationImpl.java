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

public class VoltumFuncDeclarationImpl extends VoltumFunctionMixin implements VoltumFuncDeclaration {

  public VoltumFuncDeclarationImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumFuncDeclarationImpl(@NotNull VoltumFunctionStub stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitFuncDeclaration(this);
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
  public VoltumFuncId getFuncId() {
    return notNullChild(PsiTreeUtil.getChildOfType(this, VoltumFuncId.class));
  }

  @Override
  @Nullable
  public PsiElement getAsyncKw() {
    return findChildByType(ASYNC_KW);
  }

  @Override
  @Nullable
  public PsiElement getCoroutineKw() {
    return findChildByType(COROUTINE_KW);
  }

  @Override
  @Nullable
  public PsiElement getDefKw() {
    return findChildByType(DEF_KW);
  }

  @Override
  @NotNull
  public PsiElement getFuncKw() {
    return notNullChild(findChildByType(FUNC_KW));
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
  @Nullable
  public ItemPresentation getPresentation() {
    return VoltumPsiUtilImpl.getPresentation(this);
  }

  @Override
  @Nullable
  public VoltumTypeRef getReturnType() {
    return PsiTreeUtil.getChildOfType(this, VoltumTypeRef.class);
  }

  @Override
  @NotNull
  public List<VoltumIdentifierWithType> getArguments() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumIdentifierWithType.class);
  }

  @Override
  @Nullable
  public VoltumTypeArgumentList getTypeArguments() {
    return PsiTreeUtil.getChildOfType(this, VoltumTypeArgumentList.class);
  }

}
