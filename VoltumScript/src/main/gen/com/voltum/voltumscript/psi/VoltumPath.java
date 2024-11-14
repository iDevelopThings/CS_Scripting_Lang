// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.voltum.voltumscript.psi.ext.VoltumInferenceContextOwner;
import com.intellij.psi.StubBasedPsiElement;
import com.voltum.voltumscript.lang.stubs.VoltumPlaceholderStub;
import com.voltum.voltumscript.lang.references.VoltumReference;

public interface VoltumPath extends VoltumReferenceElement, VoltumQualifiedReferenceElement, VoltumInferenceContextOwner, StubBasedPsiElement<VoltumPlaceholderStub<?>> {

  @NotNull
  VoltumPath getPath();

  @Nullable
  VoltumTypeArgumentList getTypeArgumentList();

  @Nullable
  VoltumVarReference getLastVarReference();

  @Nullable
  VoltumPath getQualifier();

  @Nullable
  PsiElement leftMostQualifier();

  @Nullable
  PsiElement getTopMostPathParent();

  @NotNull
  List<VoltumElement> getPathParts();

  @Nullable
  VoltumPath @Nullable [] getQualifiers();

  @Nullable
  VoltumReference getReference();

  @NotNull
  VoltumReference[] getReferences();

}
