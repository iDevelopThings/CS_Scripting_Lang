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

public class VoltumAnonymousFuncImpl extends VoltumFunctionMixin implements VoltumAnonymousFunc {

  public VoltumAnonymousFuncImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumAnonymousFuncImpl(@NotNull VoltumFunctionStub stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitAnonymousFunc(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public VoltumBlockBody getBlockBody() {
    return PsiTreeUtil.getChildOfType(this, VoltumBlockBody.class);
  }

  @Override
  @Nullable
  public VoltumStatement getStatement() {
    return PsiTreeUtil.getChildOfType(this, VoltumStatement.class);
  }

  @Override
  @Nullable
  public PsiElement getAsyncKw() {
    return findChildByType(ASYNC_KW);
  }

  @Override
  @Nullable
  public PsiElement getColon() {
    return findChildByType(COLON);
  }

  @Override
  @Nullable
  public PsiElement getCoroutineKw() {
    return findChildByType(COROUTINE_KW);
  }

  @Override
  @Nullable
  public PsiElement getFatArrow() {
    return findChildByType(FAT_ARROW);
  }

  @Override
  @Nullable
  public PsiElement getFuncKw() {
    return findChildByType(FUNC_KW);
  }

  @Override
  @NotNull
  public PsiElement getLparen() {
    return notNullChild(findChildByType(LPAREN));
  }

  @Override
  @NotNull
  public PsiElement getRparen() {
    return notNullChild(findChildByType(RPAREN));
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

}
