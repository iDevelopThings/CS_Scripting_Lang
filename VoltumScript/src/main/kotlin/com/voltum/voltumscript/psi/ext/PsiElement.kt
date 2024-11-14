package com.voltum.voltumscript.psi.ext

import com.intellij.extapi.psi.StubBasedPsiElementBase
import com.intellij.openapi.roots.ProjectRootManager
import com.intellij.openapi.util.TextRange
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.*
import com.intellij.psi.impl.source.PsiFileImpl
import com.intellij.psi.stubs.IStubElementType
import com.intellij.psi.stubs.StubElement
import com.intellij.psi.tree.IElementType
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.psi.util.descendantsOfType
import com.intellij.psi.util.elementType
import com.intellij.psi.util.prevLeaf
import com.intellij.util.SmartList
import com.voltum.voltumscript.ext.document
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumFile

val PsiFileSystemItem.sourceRoot: VirtualFile?
    get() = virtualFile.let { ProjectRootManager.getInstance(project).fileIndex.getSourceRootForFile(it) }

val PsiElement.ancestors: Sequence<PsiElement>
    get() = generateSequence(this) {
        if (it is PsiFile) null else it.parent
    }

val PsiElement.stubAncestors: Sequence<PsiElement>
    get() = generateSequence(this) {
        if (it is PsiFile) null else it.stubParent
    }

val PsiElement.contexts: Sequence<PsiElement>
    get() = generateSequence(this) {
        if (it is PsiFile) null else it.context
    }

val PsiElement.ancestorPairs: Sequence<Pair<PsiElement, PsiElement>>
    get() {
        val parent = this.parent ?: return emptySequence()
        return generateSequence(Pair(this, parent)) { (_, parent) ->
            val grandPa = parent.parent
            if (parent is PsiFile || grandPa == null) null else parent to grandPa
        }
    }

/*val PsiElement.greenStub: PsiElement?
    get() {
        if (this is StubBasedPsiElement<*>) {
            return greenStub?.psi
        }
        return null
    }*/
val PsiElement.voltumParent: VoltumElement?
    get() = parent as? VoltumElement

val PsiElement.stubParent: PsiElement?
    get() {
        if (this is StubBasedPsiElement<*>) {
            val stub = this.greenStub
            if (stub != null) return stub.parentStub?.psi
        }
        return parent
    }
val PsiElement.stubChildren get() = stubChildrenOfType<PsiElement>()

val PsiElement.leftLeaves: Sequence<PsiElement>
    get() = generateSequence(this, PsiTreeUtil::prevLeaf).drop(1)

val PsiElement.rightSiblings: Sequence<PsiElement>
    get() = generateSequence(this.nextSibling) { it.nextSibling }

val PsiElement.leftSiblings: Sequence<PsiElement>
    get() = generateSequence(this.prevSibling) { it.prevSibling }

val PsiElement.childrenWithLeaves: Sequence<PsiElement>
    get() = generateSequence(this.firstChild) { it.nextSibling }

fun PsiElement.hasChildOfType(elementType: IElementType)=
    this.node.findChildByType(elementType) != null

inline fun <reified T : PsiElement> PsiElement.ancestorStrict(): T? =
    PsiTreeUtil.getParentOfType(this, T::class.java, /* strict */ true)

inline fun <reified T : PsiElement> PsiElement.ancestorStrict(stopAt: Class<out PsiElement>): T? =
    PsiTreeUtil.getParentOfType(this, T::class.java, /* strict */ true, stopAt)

inline fun <reified T : PsiElement> PsiElement.ancestorOrSelf(): T? =
    PsiTreeUtil.getParentOfType(this, T::class.java, /* strict */ false)

inline fun <reified T : PsiElement> PsiElement.ancestorOrSelf(stopAt: Class<out PsiElement>): T? =
    PsiTreeUtil.getParentOfType(this, T::class.java, /* strict */ false, stopAt)

inline fun <reified T : PsiElement> PsiElement.stubAncestorStrict(): T? =
    PsiTreeUtil.getStubOrPsiParentOfType(this, T::class.java)

/**
 * Same as [ancestorStrict], but with "fake" parent links. See [org.rust.lang.core.macros.VoltumExpandedElement].
 */
inline fun <reified T : PsiElement> PsiElement.contextStrict(): T? =
    PsiTreeUtil.getContextOfType(this, T::class.java, /* strict */ true)

/**
 * Same as [ancestorOrSelf], but with "fake" parent links. See [org.rust.lang.core.macros.VoltumExpandedElement].
 */
inline fun <reified T : PsiElement> PsiElement.contextOrSelf(): T? =
    PsiTreeUtil.getContextOfType(this, T::class.java, /* strict */ false)


inline fun <reified T : PsiElement> PsiElement.childOfType(): T? =
    PsiTreeUtil.getChildOfType(this, T::class.java)

inline fun <reified T : PsiElement> PsiElement.childrenOfType(): List<T> =
    PsiTreeUtil.getChildrenOfTypeAsList(this, T::class.java)

inline fun <reified T : PsiElement> PsiElement.stubChildrenOfType(): List<T> {
    return if (this is PsiFileImpl) {
        stub?.childrenStubs?.mapNotNull { it.psi as? T } ?: return childrenOfType()
    } else {
        PsiTreeUtil.getStubChildrenOfTypeAsList(this, T::class.java)
    }
}

inline fun <reified T : PsiElement> PsiElement.descendantOfTypeStrict(): T? =
    PsiTreeUtil.findChildOfType(this, T::class.java, /* strict */ true)

inline fun <reified T : PsiElement> PsiElement.descendantOfTypeOrSelf(): T? =
    PsiTreeUtil.findChildOfType(this, T::class.java, /* strict */ false)


fun PsiElement.descendantOfTypeOrSelf(type: IElementType): PsiElement {
    return this.node.findChildByType(type)?.psi ?: this
}

inline fun <reified T : PsiElement> PsiElement.descendantsOfType(): Collection<T> =
    PsiTreeUtil.findChildrenOfType(this, T::class.java)

inline fun <reified T : PsiElement> PsiElement.descendantsOfTypeOrSelf(): Collection<T> =
    PsiTreeUtil.findChildrenOfAnyType(this, false, T::class.java)

inline fun <reified T : PsiElement> PsiElement.descendantOfType(predicate: (T) -> Boolean): T? {
    return descendantsOfType<T>().firstOrNull(predicate)
}

inline fun <reified T : PsiElement> PsiElement.lastDescendantOfType(): T? =
    descendantsOfType<T>().lastOrNull()


@Suppress("unused")
inline fun <reified T : PsiElement> PsiElement.stubDescendantsOfTypeStrict(): Collection<T> =
    getStubDescendantsOfType(this, true, T::class.java)

inline fun <reified T : PsiElement> PsiElement.stubDescendantsOfTypeOrSelf(): Collection<T> =
    getStubDescendantsOfType(this, false, T::class.java)

inline fun <reified T : PsiElement> PsiElement.stubDescendantOfTypeOrStrict(): T? =
    getStubDescendantOfType(this, true, T::class.java)

@Suppress("unused")
inline fun <reified T : PsiElement> PsiElement.stubDescendantOfTypeOrSelf(): T? =
    getStubDescendantOfType(this, false, T::class.java)

fun <T : PsiElement> getStubDescendantsOfType(
    element: PsiElement?,
    strict: Boolean,
    aClass: Class<T>
): Collection<T> {
    if (element == null) return emptyList()
    @Suppress("DEPRECATION")
    val stub = (element as? PsiFileImpl)?.greenStub
        ?: (element as? StubBasedPsiElement<*>)?.greenStub
        ?: return PsiTreeUtil.findChildrenOfAnyType(element, strict, aClass)

    val result = SmartList<T>()

    fun go(childrenStubs: List<StubElement<out PsiElement>>) {
        for (childStub in childrenStubs) {
            val child = childStub.psi
            if (aClass.isInstance(child)) {
                result.add(aClass.cast(child))
            }
            go(childStub.childrenStubs)
        }

    }

    if (strict) {
        go(stub.childrenStubs)
    } else {
        go(listOf(stub))
    }

    return result
}

fun <T : PsiElement> getStubDescendantOfType(
    element: PsiElement?,
    strict: Boolean,
    aClass: Class<T>
): T? {
    if (element == null) return null
    @Suppress("DEPRECATION")
    val stub = (element as? PsiFileImpl)?.greenStub
        ?: (element as? StubBasedPsiElement<*>)?.greenStub
        ?: return PsiTreeUtil.findChildOfType(element, aClass, strict)

    fun go(childrenStubs: List<StubElement<out PsiElement>>): T? {
        for (childStub in childrenStubs) {
            val child = childStub.psi
            if (aClass.isInstance(child)) {
                return aClass.cast(child)
            } else {
                go(childStub.childrenStubs)?.let { return it }
            }
        }

        return null
    }

    return if (strict) {
        go(stub.childrenStubs)
    } else {
        go(listOf(stub))
    }
}

fun PsiElement.stubChildOfElementType(elementType: IElementType): PsiElement? {
    val stub = (this as? StubBasedPsiElement<*>)?.stub
    return if (stub != null) {
        @Suppress("UNCHECKED_CAST")
        stub.findChildStubByType(elementType as IStubElementType<StubElement<PsiElement>, PsiElement>)?.psi
    } else {
        node.findChildByType(elementType)?.psi
    }
}

/**
 * Similar to [PsiElement.getContainingFile], but return a "fake" file if real file is
 * [com.intellij.psi.impl.source.DummyHolder] or [org.rust.lang.core.psi.VoltumCodeFragment].
 */
val PsiElement.containingVoltumFileSkippingCodeFragments: VoltumFile?
    get() {
        var containingFile = containingFile.originalFile
        /** Unwrap possible [com.intellij.psi.impl.source.DummyHolder]s and [org.rust.lang.core.psi.VoltumCodeFragment]s */
        while (containingFile !is VoltumFile) {
            containingFile = containingFile.context?.containingFile?.originalFile ?: break
        }
        return containingFile as? VoltumFile
    }

fun PsiElement?.skipWhitespacesForwardWithoutNewLines() =
    PsiTreeUtil.skipMatching(this, { it.nextSibling }, { it is PsiWhiteSpace && !it.textContains('\n') })

fun PsiElement?.skipWhitespacesBackwardWithoutNewLines() =
    PsiTreeUtil.skipMatching(this, { it.prevSibling }, { it is PsiWhiteSpace && !it.textContains('\n') })

/** Finds first sibling that is neither comment, nor whitespace before given element */
fun PsiElement?.getPrevNonCommentSibling(): PsiElement? =
    PsiTreeUtil.skipWhitespacesAndCommentsBackward(this)

/** Finds first sibling that is neither comment, nor whitespace after given element */
fun PsiElement?.getNextNonCommentSibling(): PsiElement? =
    PsiTreeUtil.skipWhitespacesAndCommentsForward(this)

/** Finds first sibling that is not whitespace before given element */
fun PsiElement?.getPrevNonWhitespaceSibling(): PsiElement? =
    PsiTreeUtil.skipWhitespacesBackward(this)

/** Finds first sibling that is not whitespace after given element */
fun PsiElement?.getNextNonWhitespaceSibling(): PsiElement? =
    PsiTreeUtil.skipWhitespacesForward(this)

fun PsiElement.isAncestorOf(child: PsiElement): Boolean =
    child.ancestors.contains(this)

fun PsiElement.isContextOf(child: PsiElement): Boolean =
    child.contexts.contains(this)

val PsiElement.startOffset: Int
    get() = textRange.startOffset

val PsiElement.endOffset: Int
    get() = textRange.endOffset

val PsiElement.endOffsetInParent: Int
    get() = startOffsetInParent + textLength

fun PsiElement.rangeWithPrevSpace(prev: PsiElement?): TextRange =
    when (prev) {
        is PsiWhiteSpace -> textRange.union(prev.textRange)
        else             -> textRange
    }

val PsiElement.rangeWithPrevSpace: TextRange
    get() = rangeWithPrevSpace(prevLeaf())

private fun PsiElement.getLineCount(): Int {
    val doc = containingFile?.document
    if (doc != null) {
        val spaceRange = textRange ?: TextRange.EMPTY_RANGE

        if (spaceRange.endOffset <= doc.textLength) {
            val startLine = doc.getLineNumber(spaceRange.startOffset)
            val endLine = doc.getLineNumber(spaceRange.endOffset)

            return endLine - startLine
        }
    }

    return (text ?: "").count { it == '\n' } + 1
}

fun PsiWhiteSpace.isMultiLine(): Boolean = getLineCount() > 1

@Suppress("UNCHECKED_CAST")
fun <U : PsiElement, T : StubElement<U>> U.greenStub(): T? =
    (this as? StubBasedPsiElementBase<T>)?.greenStub

@Suppress("UNCHECKED_CAST")
inline val <T : StubElement<*>> StubBasedPsiElement<T>.greenStub: T?
    get() = (this as? StubBasedPsiElementBase<T>)?.greenStub

/*

fun PsiElement.isKeywordLike(): Boolean {
    return when (elementTypeOrNull) {
        in RS_KEYWORDS,
        VoltumElementTypes.BOOL_LITERAL -> true

        VoltumElementTypes.IDENTIFIER -> {
            val parent = parent as? VoltumFieldLookup ?: return false
            if (parent.edition == CargoWorkspace.Edition.EDITION_2015) return false
            text == "await"
        }

        else -> false
    }
}
*/
/*

val PsiElement.isIntentionPreviewElement: Boolean
    get() {
        val source = findMacroCallExpandedFrom() ?: this
        return IntentionPreviewUtils.isPreviewElement(source)
    }

*/
/**
 * Consider we do some `resolve` in a quick-fix which is called in preview mode.
 * Quick-fix is called on copy of the original file, but `resolve` will return original element.
 * We will have an exception if we try to modify the original element.
 * Thus, we should call this function on `resolve` result to obtain element in the copy of the original file.
 *//*

fun <T : PsiElement> T.findPreviewCopyIfNeeded(): T {
    val previewEditor = IntentionPreviewUtils.getPreviewEditor() ?: return this
    val copyFile = PsiDocumentManager.getInstance(project).getPsiFile(previewEditor.document) ?: return this
    if (!copyFile.isIntentionPreviewElement) return this
    return when (containingFile) {
        copyFile -> this
        copyFile.originalFile -> PsiTreeUtil.findSameElementInCopy(this, copyFile)
        // TODO this is incorrect, intention will fail with "Must not change PSI outside write action"
        else -> this
    }
}
*/
