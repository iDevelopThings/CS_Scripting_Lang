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

public class VoltumLiteralValueImpl extends VoltumLiteralMixin implements VoltumLiteralValue {

  public VoltumLiteralValueImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumLiteralValueImpl(@NotNull VoltumPlaceholderStub<?> stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitLiteralValue(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public PsiElement getStringLiteral() {
    return findChildByType(STRING_LITERAL);
  }

  @Override
  @Nullable
  public PsiElement getValueBool() {
    return findChildByType(VALUE_BOOL);
  }

  @Override
  @Nullable
  public PsiElement getValueFloat() {
    return findChildByType(VALUE_FLOAT);
  }

  @Override
  @Nullable
  public PsiElement getValueInteger() {
    return findChildByType(VALUE_INTEGER);
  }

  @Override
  @Nullable
  public PsiElement getValueNull() {
    return findChildByType(VALUE_NULL);
  }

}
