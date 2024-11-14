// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumTypeDeclarationBody extends VoltumElement {

  @NotNull
  PsiElement getLcurly();

  @Nullable
  PsiElement getRcurly();

  @NotNull
  List<VoltumTypeDeclarationFieldMember> getFields();

  @NotNull
  List<VoltumTypeDeclarationMethodMember> getMethods();

  @NotNull
  List<VoltumTypeDeclarationConstructor> getConstructors();

}
