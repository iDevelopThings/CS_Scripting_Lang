// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.intellij.codeInsight.lookup.LookupElement;
import com.intellij.navigation.ItemPresentation;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;
import com.intellij.psi.stubs.IStubElementType;

public class VoltumCallExprImpl extends VoltumCallExprMixin implements VoltumCallExpr {

  public VoltumCallExprImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumCallExprImpl(@NotNull VoltumPlaceholderStub<?> stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  @Override
  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitCallExpr(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public List<VoltumExpr> getExprList() {
    return PsiTreeUtil.getStubChildrenOfTypeAsList(this, VoltumExpr.class);
  }

  @Override
  @Nullable
  public VoltumPath getPath() {
    return PsiTreeUtil.getStubChildOfType(this, VoltumPath.class);
  }

  @Override
  @Nullable
  public VoltumTypeArgumentList getTypeArgumentList() {
    return PsiTreeUtil.getChildOfType(this, VoltumTypeArgumentList.class);
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
  public VoltumPath getQualifier() {
    return getPath();
  }

  @Override
  @Nullable
  public VoltumIdent getNameId() {
    return VoltumPsiUtilImpl.getNameId(this);
  }

  @Override
  @Nullable
  public PsiElement getNameIdentifier() {
    return VoltumPsiUtilImpl.getNameIdentifier(this);
  }

  @Override
  @Nullable
  public ItemPresentation getPresentation() {
    return VoltumPsiUtilImpl.getPresentation(this);
  }

  @Override
  @NotNull
  public PsiElement setName(@NotNull String name) {
    return VoltumPsiUtilImpl.setName(this, name);
  }

  @Override
  @Nullable
  public LookupElement getLookupElement() {
    return VoltumPsiUtilImpl.getLookupElement(this);
  }

}
