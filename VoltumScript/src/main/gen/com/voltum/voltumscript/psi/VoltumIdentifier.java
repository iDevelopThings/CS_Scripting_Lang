// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.voltum.voltumscript.psi.ext.VoltumInferenceContextOwner;

public interface VoltumIdentifier extends VoltumIdent, VoltumReferenceElement, VoltumInferenceContextOwner {

  @Nullable
  PsiElement getId();

  @Nullable
  VoltumIdent getNameId();

  @Nullable
  PsiElement getNameIdentifier();

}
