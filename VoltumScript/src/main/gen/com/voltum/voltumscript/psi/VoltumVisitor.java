// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElementVisitor;
import com.voltum.voltumscript.psi.ext.VoltumInferenceContextOwner;

public class VoltumVisitor extends PsiElementVisitor {

  public void visitAnonymousFunc(@NotNull VoltumAnonymousFunc o) {
    visitFunction(o);
  }

  public void visitArgumentId(@NotNull VoltumArgumentId o) {
    visitIdentifier(o);
  }

  public void visitAtomExpr(@NotNull VoltumAtomExpr o) {
    visitExpr(o);
  }

  public void visitAttribute(@NotNull VoltumAttribute o) {
    visitElement(o);
  }

  public void visitAwaitExpr(@NotNull VoltumAwaitExpr o) {
    visitExpr(o);
  }

  public void visitBinaryExpr(@NotNull VoltumBinaryExpr o) {
    visitExpr(o);
  }

  public void visitBinaryOp(@NotNull VoltumBinaryOp o) {
    visitElement(o);
  }

  public void visitBlockBody(@NotNull VoltumBlockBody o) {
    visitElement(o);
  }

  public void visitBreakExpr(@NotNull VoltumBreakExpr o) {
    visitExpr(o);
  }

  public void visitCallExpr(@NotNull VoltumCallExpr o) {
    visitExpr(o);
    // visitReferenceElement(o);
  }

  public void visitContinueExpr(@NotNull VoltumContinueExpr o) {
    visitExpr(o);
  }

  public void visitDeferExpr(@NotNull VoltumDeferExpr o) {
    visitExpr(o);
  }

  public void visitDictionaryField(@NotNull VoltumDictionaryField o) {
    visitElement(o);
  }

  public void visitDictionaryValue(@NotNull VoltumDictionaryValue o) {
    visitValueTypeElement(o);
  }

  public void visitElseStatement(@NotNull VoltumElseStatement o) {
    visitElement(o);
  }

  public void visitExpr(@NotNull VoltumExpr o) {
    visitElement(o);
  }

  public void visitExpressionCodeFragmentElement(@NotNull VoltumExpressionCodeFragmentElement o) {
    visitElement(o);
  }

  public void visitFieldId(@NotNull VoltumFieldId o) {
    visitIdentifier(o);
  }

  public void visitForLoopStatement(@NotNull VoltumForLoopStatement o) {
    visitElement(o);
  }

  public void visitFuncDeclaration(@NotNull VoltumFuncDeclaration o) {
    visitFunction(o);
    // visitInferenceContextOwner(o);
  }

  public void visitFuncId(@NotNull VoltumFuncId o) {
    visitIdentifier(o);
  }

  public void visitIdentifier(@NotNull VoltumIdentifier o) {
    visitIdent(o);
    // visitReferenceElement(o);
    // visitInferenceContextOwner(o);
  }

  public void visitIdentifierReference(@NotNull VoltumIdentifierReference o) {
    visitIdentifier(o);
  }

  public void visitIdentifierWithType(@NotNull VoltumIdentifierWithType o) {
    visitElement(o);
  }

  public void visitIfStatement(@NotNull VoltumIfStatement o) {
    visitElement(o);
  }

  public void visitListValue(@NotNull VoltumListValue o) {
    visitValueTypeElement(o);
  }

  public void visitLiteralBool(@NotNull VoltumLiteralBool o) {
    visitLiteralValue(o);
  }

  public void visitLiteralExpr(@NotNull VoltumLiteralExpr o) {
    visitExpr(o);
    // visitValueTypeElement(o);
  }

  public void visitLiteralFloat(@NotNull VoltumLiteralFloat o) {
    visitLiteralValue(o);
  }

  public void visitLiteralInt(@NotNull VoltumLiteralInt o) {
    visitLiteralValue(o);
  }

  public void visitLiteralNull(@NotNull VoltumLiteralNull o) {
    visitLiteralValue(o);
  }

  public void visitLiteralString(@NotNull VoltumLiteralString o) {
    visitLiteralValue(o);
  }

  public void visitLiteralValue(@NotNull VoltumLiteralValue o) {
    visitValueTypeElement(o);
  }

  public void visitParenExpr(@NotNull VoltumParenExpr o) {
    visitExpr(o);
  }

  public void visitPath(@NotNull VoltumPath o) {
    visitReferenceElement(o);
    // visitQualifiedReferenceElement(o);
    // visitInferenceContextOwner(o);
  }

  public void visitPostfixDecExpr(@NotNull VoltumPostfixDecExpr o) {
    visitPostfixExpr(o);
  }

  public void visitPostfixExpr(@NotNull VoltumPostfixExpr o) {
    visitExpr(o);
  }

  public void visitPostfixIncExpr(@NotNull VoltumPostfixIncExpr o) {
    visitPostfixExpr(o);
  }

  public void visitPrefixDecExpr(@NotNull VoltumPrefixDecExpr o) {
    visitPrefixExpr(o);
  }

  public void visitPrefixExpr(@NotNull VoltumPrefixExpr o) {
    visitExpr(o);
  }

  public void visitPrefixIncExpr(@NotNull VoltumPrefixIncExpr o) {
    visitPrefixExpr(o);
  }

  public void visitRangeExpr(@NotNull VoltumRangeExpr o) {
    visitExpr(o);
  }

  public void visitReturnExpr(@NotNull VoltumReturnExpr o) {
    visitExpr(o);
  }

  public void visitSignalDeclaration(@NotNull VoltumSignalDeclaration o) {
    visitInferenceContextOwner(o);
  }

  public void visitStatement(@NotNull VoltumStatement o) {
    visitBaseStatement(o);
  }

  public void visitStatementCodeFragmentElement(@NotNull VoltumStatementCodeFragmentElement o) {
    visitElement(o);
  }

  public void visitTupleExpr(@NotNull VoltumTupleExpr o) {
    visitExpr(o);
  }

  public void visitTypeArgumentList(@NotNull VoltumTypeArgumentList o) {
    visitElement(o);
  }

  public void visitTypeDeclaration(@NotNull VoltumTypeDeclaration o) {
    visitDeclaration(o);
    // visitInferenceContextOwner(o);
  }

  public void visitTypeDeclarationBody(@NotNull VoltumTypeDeclarationBody o) {
    visitElement(o);
  }

  public void visitTypeDeclarationConstructor(@NotNull VoltumTypeDeclarationConstructor o) {
    visitElement(o);
  }

  public void visitTypeDeclarationFieldMember(@NotNull VoltumTypeDeclarationFieldMember o) {
    visitElement(o);
  }

  public void visitTypeDeclarationMethodMember(@NotNull VoltumTypeDeclarationMethodMember o) {
    visitElement(o);
  }

  public void visitTypeId(@NotNull VoltumTypeId o) {
    visitIdentifier(o);
  }

  public void visitTypeRef(@NotNull VoltumTypeRef o) {
    visitReferenceElement(o);
    // visitIdent(o);
    // visitInferenceContextOwner(o);
  }

  public void visitUnaryExpr(@NotNull VoltumUnaryExpr o) {
    visitExpr(o);
  }

  public void visitVarId(@NotNull VoltumVarId o) {
    visitIdentifier(o);
  }

  public void visitVarReference(@NotNull VoltumVarReference o) {
    visitIdentifierReference(o);
  }

  public void visitVariableDeclaration(@NotNull VoltumVariableDeclaration o) {
    visitReferenceElement(o);
  }

  public void visitBaseStatement(@NotNull VoltumBaseStatement o) {
    visitElement(o);
  }

  public void visitDeclaration(@NotNull VoltumDeclaration o) {
    visitElement(o);
  }

  public void visitFunction(@NotNull VoltumFunction o) {
    visitElement(o);
  }

  public void visitIdent(@NotNull VoltumIdent o) {
    visitElement(o);
  }

  public void visitReferenceElement(@NotNull VoltumReferenceElement o) {
    visitElement(o);
  }

  public void visitValueTypeElement(@NotNull VoltumValueTypeElement o) {
    visitElement(o);
  }

  public void visitInferenceContextOwner(@NotNull VoltumInferenceContextOwner o) {
    visitElement(o);
  }

  public void visitElement(@NotNull VoltumElement o) {
    super.visitElement(o);
  }

}
