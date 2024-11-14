package com.voltum.voltumscript.lang

import com.intellij.lang.HelpID
import com.intellij.lang.cacheBuilder.DefaultWordsScanner
import com.intellij.lang.cacheBuilder.WordsScanner
import com.intellij.lang.findUsages.FindUsagesProvider
import com.intellij.navigation.NavigationItem
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.lexer.VoltumLexerAdapter
import com.voltum.voltumscript.parser.COMMENTS
import com.voltum.voltumscript.parser.IDENTIFIERS
import com.voltum.voltumscript.parser.LITERAL_VALUES
import com.voltum.voltumscript.parser.VoltumTokenSets
import com.voltum.voltumscript.psi.*


class VoltumFindUsagesProvider : FindUsagesProvider {

    override fun getWordsScanner(): WordsScanner {
        return DefaultWordsScanner(
            VoltumLexerAdapter(),
            IDENTIFIERS,
            COMMENTS,
            LITERAL_VALUES
        )
    }

    override fun canFindUsagesFor(element: PsiElement): Boolean {
        if(element.isFunctionLike()) return true
        if(element.isVariableLike()) return true
        if(element.isTypeDeclarationLike()) return true
        
        return false
    }

    override fun getHelpId(psiElement: PsiElement): String {
        return HelpID.FIND_OTHER_USAGES
    }

    override fun getType(element: PsiElement): String {
        if(element.isFunctionLike()) return "Function"
        if(element.isVariableLike()) return "Variable"
        if(element.isTypeDeclarationLike()) return "Type"
        
        return "Unknown"
    }

    override fun getDescriptiveName(element: PsiElement): String {
        // when (element) {
        // is VoltumObjectFieldKey -> return element.id.text
        // }
        if (element is NavigationItem) {
            return element.presentation?.locationString + " " + element.presentation?.presentableText
        }

        return "Unknown"
    }

    override fun getNodeText(element: PsiElement, useFullName: Boolean): String {
        //        when (element) {
        //        is VoltumObjectFieldKey -> return element.text
        //        }

        if (element is NavigationItem) {
            return element.presentation?.presentableText ?: "Unknown"
        }

        return "Unknown"
    }
}