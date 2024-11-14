// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.voltum.voltumscript.lang.references.VoltumReference;

public interface VoltumIdentifierWithType extends VoltumElement {

  @NotNull
  VoltumArgumentId getNameIdentifier();

  @NotNull
  VoltumTypeRef getType();

  @Nullable
  VoltumReference getReference();

}
