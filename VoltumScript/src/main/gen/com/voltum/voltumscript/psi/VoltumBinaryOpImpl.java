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

public class VoltumBinaryOpImpl extends VoltumBinaryOpMixin implements VoltumBinaryOp {

  public VoltumBinaryOpImpl(@NotNull IElementType type) {
    super(type);
  }

  public void accept(@NotNull VoltumVisitor visitor) {
    visitor.visitBinaryOp(this);
  }

  @Override
  public void accept(@NotNull PsiElementVisitor visitor) {
    if (visitor instanceof VoltumVisitor) accept((VoltumVisitor)visitor);
    else super.accept(visitor);
  }

  @Override
  @Nullable
  public PsiElement getAnd() {
    return findPsiChildByType(AND);
  }

  @Override
  @Nullable
  public PsiElement getAndand() {
    return findPsiChildByType(ANDAND);
  }

  @Override
  @Nullable
  public PsiElement getAndeq() {
    return findPsiChildByType(ANDEQ);
  }

  @Override
  @Nullable
  public PsiElement getDiv() {
    return findPsiChildByType(DIV);
  }

  @Override
  @Nullable
  public PsiElement getDiveq() {
    return findPsiChildByType(DIVEQ);
  }

  @Override
  @Nullable
  public PsiElement getEq() {
    return findPsiChildByType(EQ);
  }

  @Override
  @Nullable
  public PsiElement getEqeq() {
    return findPsiChildByType(EQEQ);
  }

  @Override
  @Nullable
  public PsiElement getExcleq() {
    return findPsiChildByType(EXCLEQ);
  }

  @Override
  @Nullable
  public PsiElement getGt() {
    return findPsiChildByType(GT);
  }

  @Override
  @Nullable
  public PsiElement getGteq() {
    return findPsiChildByType(GTEQ);
  }

  @Override
  @Nullable
  public PsiElement getGtgt() {
    return findPsiChildByType(GTGT);
  }

  @Override
  @Nullable
  public PsiElement getLt() {
    return findPsiChildByType(LT);
  }

  @Override
  @Nullable
  public PsiElement getLteq() {
    return findPsiChildByType(LTEQ);
  }

  @Override
  @Nullable
  public PsiElement getLtlt() {
    return findPsiChildByType(LTLT);
  }

  @Override
  @Nullable
  public PsiElement getMinus() {
    return findPsiChildByType(MINUS);
  }

  @Override
  @Nullable
  public PsiElement getMinuseq() {
    return findPsiChildByType(MINUSEQ);
  }

  @Override
  @Nullable
  public PsiElement getMul() {
    return findPsiChildByType(MUL);
  }

  @Override
  @Nullable
  public PsiElement getMuleq() {
    return findPsiChildByType(MULEQ);
  }

  @Override
  @Nullable
  public PsiElement getOr() {
    return findPsiChildByType(OR);
  }

  @Override
  @Nullable
  public PsiElement getOreq() {
    return findPsiChildByType(OREQ);
  }

  @Override
  @Nullable
  public PsiElement getOror() {
    return findPsiChildByType(OROR);
  }

  @Override
  @Nullable
  public PsiElement getPlus() {
    return findPsiChildByType(PLUS);
  }

  @Override
  @Nullable
  public PsiElement getPluseq() {
    return findPsiChildByType(PLUSEQ);
  }

  @Override
  @Nullable
  public PsiElement getRem() {
    return findPsiChildByType(REM);
  }

  @Override
  @Nullable
  public PsiElement getRemeq() {
    return findPsiChildByType(REMEQ);
  }

  @Override
  @Nullable
  public PsiElement getXor() {
    return findPsiChildByType(XOR);
  }

  @Override
  @Nullable
  public PsiElement getXoreq() {
    return findPsiChildByType(XOREQ);
  }

}
