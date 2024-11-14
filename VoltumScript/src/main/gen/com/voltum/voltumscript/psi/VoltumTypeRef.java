// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import java.util.List;
import org.jetbrains.annotations.*;
import com.intellij.psi.PsiElement;
import com.voltum.voltumscript.psi.ext.VoltumInferenceContextOwner;
import com.voltum.voltumscript.lang.references.VoltumReference;
import com.voltum.voltumscript.lang.types.TyReference;

public interface VoltumTypeRef extends VoltumReferenceElement, VoltumIdent, VoltumInferenceContextOwner {

  @Nullable
  PsiElement getDotdotdot();

  @Nullable
  PsiElement getId();

  @Nullable
  VoltumReference getReference();

  @Nullable
  TyReference tryResolveType();

  @Nullable
  PsiElement getArray();

  @Nullable
  VoltumTypeArgumentList getTypeArguments();

}
