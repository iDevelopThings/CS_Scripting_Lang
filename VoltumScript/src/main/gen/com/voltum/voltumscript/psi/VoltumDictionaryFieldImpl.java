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
import com.intellij.psi.stubs.IStubElementType;

public class VoltumDictionaryFieldImpl extends VoltumStubbedElementImpl<VoltumDictionaryFieldStub> implements VoltumDictionaryField {

  public VoltumDictionaryFieldImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumDictionaryFieldImpl(@NotNull VoltumDictionaryFieldStub stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitDictionaryField(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public VoltumFieldId getFieldId() {
    return notNullChild(PsiTreeUtil.getChildOfType(this, VoltumFieldId.class));
  }

  @Override
  @NotNull
  public PsiElement getColon() {
    return notNullChild(findChildByType(COLON));
  }

  @Override
  @NotNull
  public VoltumExpr getValue() {
    return notNullChild(PsiTreeUtil.getStubChildOfType(this, VoltumExpr.class));
  }

}
