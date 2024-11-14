// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;

public interface VoltumTypeArgumentList extends VoltumElement {

  @NotNull
  List<VoltumTypeRef> getTypeRefList();

  @NotNull
  PsiElement getGt();

  @NotNull
  PsiElement getLt();

}
