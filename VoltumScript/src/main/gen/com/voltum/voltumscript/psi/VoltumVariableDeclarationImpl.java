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

public class VoltumVariableDeclarationImpl extends VoltumVariableDeclarationMixin implements VoltumVariableDeclaration {

  public VoltumVariableDeclarationImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumVariableDeclarationImpl(@NotNull VoltumVariableDeclarationStub stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitVariableDeclaration(this);
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
  @NotNull
  public List<VoltumVarId> getVarIdList() {
    return PsiTreeUtil.getChildrenOfTypeAsList(this, VoltumVarId.class);
  }

  @Override
  @Nullable
  public PsiElement getEq() {
    return findChildByType(EQ);
  }

  @Override
  @NotNull
  public PsiElement getVarKw() {
    return notNullChild(findChildByType(VAR_KW));
  }

  @Override
  @Nullable
  public ItemPresentation getPresentation() {
    return VoltumPsiUtilImpl.getPresentation(this);
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
  @NotNull
  public VoltumVarId getVarName() {
    List<VoltumVarId> p1 = getVarIdList();
    return p1.get(0);
  }

  @Override
  @Nullable
  public VoltumExpr getInitializer() {
    List<VoltumExpr> p1 = getExprList();
    return p1.size() < 1 ? null : p1.get(0);
  }

  @Override
  @Nullable
  public List<VoltumExpr> getInitializers() {
    return getExprList();
  }

}
