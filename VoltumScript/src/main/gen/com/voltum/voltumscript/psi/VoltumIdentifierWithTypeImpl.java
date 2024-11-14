// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.voltum.voltumscript.lang.references.VoltumReference;
import com.intellij.psi.tree.IElementType;

public class VoltumIdentifierWithTypeImpl extends VoltumElementImpl implements VoltumIdentifierWithType {

  public VoltumIdentifierWithTypeImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitIdentifierWithType(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @NotNull
  public VoltumArgumentId getNameIdentifier() {
    return PsiTreeUtil.getChildOfType(this, VoltumArgumentId.class);
  }

  @Override
  @NotNull
  public VoltumTypeRef getType() {
    return PsiTreeUtil.getChildOfType(this, VoltumTypeRef.class);
  }

  @Override
  @Nullable
  public VoltumReference getReference() {
    return VoltumPsiUtilImpl.getReference(this);
  }

}
