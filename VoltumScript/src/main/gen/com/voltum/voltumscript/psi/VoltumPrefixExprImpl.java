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

public class VoltumPrefixExprImpl extends VoltumPrefixExprMixin implements VoltumPrefixExpr {

  public VoltumPrefixExprImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumPrefixExprImpl(@NotNull VoltumPlaceholderStub<?> stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  @Override
  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitPrefixExpr(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public VoltumExpr getExpr() {
    return notNullChild(PsiTreeUtil.getStubChildOfType(this, VoltumExpr.class));
  }

  @Override
  @Nullable
  public PsiElement getMinusminus() {
    return findChildByType(MINUSMINUS);
  }

  @Override
  @Nullable
  public PsiElement getPlusplus() {
    return findChildByType(PLUSPLUS);
  }

}
