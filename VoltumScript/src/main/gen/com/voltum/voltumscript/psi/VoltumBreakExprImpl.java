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

public class VoltumBreakExprImpl extends VoltumExprImpl implements VoltumBreakExpr {

  public VoltumBreakExprImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumBreakExprImpl(@NotNull VoltumPlaceholderStub<?> stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  @Override
  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitBreakExpr(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public VoltumLiteralInt getLiteralInt() {
    return PsiTreeUtil.getStubChildOfType(this, VoltumLiteralInt.class);
  }

  @Override
  @NotNull
  public PsiElement getBreakKw() {
    return notNullChild(findChildByType(BREAK_KW));
  }

}
