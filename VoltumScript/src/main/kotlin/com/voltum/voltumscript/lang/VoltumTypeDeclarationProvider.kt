package com.voltum.voltumscript.lang

import com.intellij.codeInsight.navigation.actions.TypeDeclarationProvider
import com.intellij.openapi.diagnostic.logger
import com.intellij.psi.PsiElement

class VoltumTypeDeclarationProvider : TypeDeclarationProvider {
    private val LOG = logger<VoltumTypeDeclarationProvider>()

    override fun getSymbolTypeDeclarations(symbol: PsiElement): Array<PsiElement>? {
        val ref = symbol.reference
        var resolved = ref?.resolve()
        
        if (resolved != null) {
            return arrayOf(resolved)
        }

        LOG.warn("failed to resolve symbol: $symbol -> '${symbol.text}'")
        
        return null
    }
}