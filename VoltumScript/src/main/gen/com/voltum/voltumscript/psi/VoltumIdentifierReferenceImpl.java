// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.lang.ASTNode;
import com.intellij.psi.PsiElement;
import com.intellij.psi.PsiElementVisitor;
import com.intellij.psi.util.PsiTreeUtil;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import com.intellij.psi.tree.IElementType;

public class VoltumIdentifierReferenceImpl extends VoltumIdentifierImpl implements VoltumIdentifierReference {

  public VoltumIdentifierReferenceImpl(@NotNull IElementType type) {
    super(type);
  }

  @Override
  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitIdentifierReference(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

}
