package com.voltum.voltumscript.psi

import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.intellij.psi.PsiTreeChangeEvent
import com.intellij.psi.PsiTreeChangeListener
import com.intellij.psi.impl.PsiTreeChangeEventImpl
import java.util.*

sealed class VoltumPsiTreeChangeEvent {
    /**
     * Event can relate to changes in a file system, e.g. file creation/deletion/movement.
     * In this case the property is set to null.
     */
    open val file: PsiFile? get() = null

    sealed class ChildAddition(
        override val file: PsiFile?,
        val parent: PsiElement
    ) : VoltumPsiTreeChangeEvent() {
        abstract val child: PsiElement?

        class Before(
            file: PsiFile?,
            parent: PsiElement,
            override val child: PsiElement?
        ) : ChildAddition(file, parent)

        class After(
            file: PsiFile?,
            parent: PsiElement,
            override val child: PsiElement
        ) : ChildAddition(file, parent)

        override fun toStringSimple(): String = "ChildAddition.${javaClass.simpleName}(file=$file)"
        override fun toString(): String =
            "ChildAddition.${javaClass.simpleName}(file=$file, parent=`${parent.text}`, child=`${child?.text}`)"
    }

    sealed class ChildRemoval(
        override val file: PsiFile?,
        val parent: PsiElement,
        /** Invalid in [ChildRemoval.After] */
        val child: PsiElement
    ) : VoltumPsiTreeChangeEvent() {
        class Before(file: PsiFile?, parent: PsiElement, child: PsiElement) : ChildRemoval(file, parent, child)
        class After(file: PsiFile?, parent: PsiElement, child: PsiElement) : ChildRemoval(file, parent, child)

        override fun toStringSimple(): String = "ChildRemoval.${javaClass.simpleName}(file=$file)"

        override fun toString(): String =
            "ChildRemoval.${javaClass.simpleName}(file=$file, parent=`${parent.text}`, child=`${child.safeText}`)"
    }

    sealed class ChildReplacement(
        override val file: PsiFile?,
        val parent: PsiElement,
        /** Invalid in [ChildReplacement.After] */
        val oldChild: PsiElement
    ) : VoltumPsiTreeChangeEvent() {

        abstract val newChild: PsiElement?

        class Before(
            file: PsiFile?,
            parent: PsiElement,
            oldChild: PsiElement,
            override val newChild: PsiElement?
        ) : ChildReplacement(file, parent, oldChild)

        class After(
            file: PsiFile?,
            parent: PsiElement,
            oldChild: PsiElement,
            override val newChild: PsiElement
        ) : ChildReplacement(file, parent, oldChild)

        override fun toStringSimple(): String = "ChildReplacement.${javaClass.simpleName}(file=$file)"

        override fun toString(): String =
            "ChildReplacement.${javaClass.simpleName}(file=$file, parent=`${parent.text}`, " +
                    "oldChild=`${oldChild.safeText}`, newChild=`${newChild?.text}`)"
    }

    @Suppress("MemberVisibilityCanBePrivate")
    sealed class ChildMovement(
        override val file: PsiFile?,
        val oldParent: PsiElement,
        val newParent: PsiElement,
        val child: PsiElement
    ) : VoltumPsiTreeChangeEvent() {
        class Before(file: PsiFile?, oldParent: PsiElement, newParent: PsiElement, child: PsiElement) : ChildMovement(file, oldParent, newParent, child)
        class After(file: PsiFile?, oldParent: PsiElement, newParent: PsiElement, child: PsiElement) : ChildMovement(file, oldParent, newParent, child)

        override fun toStringSimple(): String = "ChildMovement.${javaClass.simpleName}(file=$file)"

        override fun toString(): String =
            "ChildMovement.${javaClass.simpleName}(file=$file, oldParent=`${oldParent.text}`, " +
                    "newParent=`${newParent.text}`, child=`${child.text}`)"


    }

    sealed class ChildrenChange(
        override val file: PsiFile?,
        val parent: PsiElement,
        /**
         * "generic change" event means that "something changed inside an element" and
         * sends before/after all events for concrete PSI changes in the element.
         */
        val isGenericChange: Boolean
    ) : VoltumPsiTreeChangeEvent() {
        class Before(file: PsiFile?, parent: PsiElement, isGenericChange: Boolean) : ChildrenChange(file, parent, isGenericChange)
        class After(file: PsiFile?, parent: PsiElement, isGenericChange: Boolean) : ChildrenChange(file, parent, isGenericChange)

        override fun toStringSimple(): String = "ChildrenChange.${javaClass.simpleName}(file=$file)"
        override fun toString(): String =
            "ChildrenChange.${javaClass.simpleName}(file=$file, parent=`${parent.text}`, " +
                    "isGenericChange=$isGenericChange)"
    }

    @Suppress("MemberVisibilityCanBePrivate")
    sealed class PropertyChange(
        val propertyName: String,
        val oldValue: Any?,
        val newValue: Any?,
        val element: PsiElement?,
        val child: PsiElement?,
    ) : VoltumPsiTreeChangeEvent() {
        class Before(
            propertyName: String,
            oldValue: Any?,
            newValue: Any?,
            element: PsiElement?,
            child: PsiElement?
        ) : PropertyChange(propertyName, oldValue, newValue, element, child)

        class After(
            propertyName: String,
            oldValue: Any?,
            newValue: Any?,
            element: PsiElement?,
            child: PsiElement?
        ) : PropertyChange(propertyName, oldValue, newValue, element, child)

        override fun toStringSimple(): String = "PropertyChange.${javaClass.simpleName}(propertyName='$propertyName')"
        
        override fun toString(): String {
            val oldValue = if (oldValue is Array<*>) Arrays.toString(oldValue) else oldValue
            val newValue = if (newValue is Array<*>) Arrays.toString(newValue) else newValue
            return "PropertyChange.${javaClass.simpleName}(propertyName='$propertyName', " +
                    "oldValue=$oldValue, newValue=$newValue, element=$element, child=$child)"
        }
    }

    open fun toStringSimple() = "VoltumPsiTreeChangeEvent.${javaClass.simpleName}"
}

abstract class VoltumPsiTreeChangeAdapter : PsiTreeChangeListener {

    abstract fun handleEvent(event: VoltumPsiTreeChangeEvent)

    override fun beforePropertyChange(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.PropertyChange.Before(
                event.propertyName,
                event.oldValue,
                event.newValue,
                event.element,
                event.child
            )
        )
    }

    override fun propertyChanged(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.PropertyChange.After(
                event.propertyName,
                event.oldValue,
                event.newValue,
                event.element,
                event.child
            )
        )
    }

    override fun beforeChildReplacement(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildReplacement.Before(
                event.file,
                event.parent,
                event.oldChild,
                event.newChild
            )
        )
    }

    override fun childReplaced(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildReplacement.After(
                event.file,
                event.parent,
                event.oldChild,
                event.newChild
            )
        )
    }

    override fun beforeChildAddition(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildAddition.Before(
                event.file,
                event.parent,
                event.child
            )
        )
    }

    override fun childAdded(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildAddition.After(
                event.file,
                event.parent,
                event.child
            )
        )
    }

    override fun beforeChildMovement(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildMovement.Before(
                event.file,
                event.oldParent,
                event.newParent,
                event.child
            )
        )
    }

    override fun childMoved(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildMovement.After(
                event.file,
                event.oldParent,
                event.newParent,
                event.child
            )
        )
    }

    override fun beforeChildRemoval(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildRemoval.Before(
                event.file,
                event.parent,
                event.child
            )
        )
    }

    override fun childRemoved(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildRemoval.After(
                event.file,
                event.parent,
                event.child
            )
        )
    }

    override fun beforeChildrenChange(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildrenChange.Before(
                event.file,
                event.parent,
                (event as? PsiTreeChangeEventImpl)?.isGenericChange == true
            )
        )
    }

    override fun childrenChanged(event: PsiTreeChangeEvent) {
        handleEvent(
            VoltumPsiTreeChangeEvent.ChildrenChange.After(
                event.file,
                event.parent,
                (event as? PsiTreeChangeEventImpl)?.isGenericChange == true
            )
        )
    }
}

/**
 * It is not safe to call getText() on invalid PSI elements, but sometimes it works,
 * so we can try to use it for debug purposes
 */
private val PsiElement.safeText
    get() = try {
        text
    } catch (ignored: Exception) {
        "<exception>"
    }
