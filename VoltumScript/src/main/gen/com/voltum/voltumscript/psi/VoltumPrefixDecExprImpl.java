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

public class VoltumPrefixDecExprImpl extends VoltumPrefixExprImpl implements VoltumPrefixDecExpr {

  public VoltumPrefixDecExprImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumPrefixDecExprImpl(@NotNull VoltumPlaceholderStub<?> stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  @Override
  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitPrefixDecExpr(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public VoltumExpr getExpr() {
    return PsiTreeUtil.getChildOfType(this, VoltumExpr.class);
  }

  @Override
  @NotNull
  public PsiElement getMinusminus() {
    return notNullChild(findChildByType(MINUSMINUS));
  }

}
