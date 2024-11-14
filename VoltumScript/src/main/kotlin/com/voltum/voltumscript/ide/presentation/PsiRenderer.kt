package com.voltum.voltumscript.ide.presentation

import com.intellij.psi.PsiElement

class VoltumPsiRenderer {
    fun build(element: PsiElement?): String {
        return element?.text ?: ""
    }
}