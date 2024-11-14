// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.intellij.psi.stubs.IStubElementType;

public class VoltumDictionaryValueImpl extends VoltumDictionaryMixin implements VoltumDictionaryValue {

  public VoltumDictionaryValueImpl(@NotNull ASTNode node) {
    super(node);
  }

  public VoltumDictionaryValueImpl(@NotNull VoltumDictionaryStub stub, @NotNull IStubElementType<?, ?> type) {
    super(stub, type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitDictionaryValue(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public List<VoltumDictionaryField> getDictionaryFieldList() {
    return PsiTreeUtil.getStubChildrenOfTypeAsList(this, VoltumDictionaryField.class);
  }

  @Override
  @NotNull
  public PsiElement getLcurly() {
    return notNullChild(findChildByType(LCURLY));
  }

  @Override
  @Nullable
  public PsiElement getRcurly() {
    return findChildByType(RCURLY);
  }

}
