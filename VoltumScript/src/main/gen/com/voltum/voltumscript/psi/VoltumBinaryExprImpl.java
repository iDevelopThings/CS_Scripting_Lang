// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;
import com.intellij.psi.stubs.IStubElementType;

public class VoltumBinaryExprImpl extends VoltumBinaryExprMixin implements VoltumBinaryExpr {

  public VoltumBinaryExprImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumBinaryExprImpl(@NotNull VoltumPlaceholderStub<?> stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  @Override
  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitBinaryExpr(this);
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
  public VoltumExpr getLeft() {
    List<VoltumExpr> p1 = getExprList();
    return p1.get(0);
  }

  @Override
  @Nullable
  public VoltumExpr getRight() {
    List<VoltumExpr> p1 = getExprList();
    return p1.size() < 2 ? null : p1.get(1);
  }

  @Override
  @NotNull
  public VoltumBinaryOp getOperator() {
    return notNullChild(PsiTreeUtil.getChildOfType(this, VoltumBinaryOp.class));
  }

}
