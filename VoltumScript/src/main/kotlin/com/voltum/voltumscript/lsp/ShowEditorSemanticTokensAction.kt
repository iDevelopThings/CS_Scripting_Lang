package com.voltum.voltumscript.lsp

import com.intellij.codeInsight.hint.HintManager
import com.intellij.codeInsight.hint.HintManagerImpl
import com.intellij.codeInsight.hint.HintUtil
import com.intellij.injected.editor.EditorWindow
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.IdeActions
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.editor.Caret
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.actionSystem.EditorAction
import com.intellij.openapi.editor.actionSystem.EditorActionHandler
import com.intellij.openapi.editor.actionSystem.EditorActionManager
import com.intellij.openapi.editor.event.EditorMouseEvent
import com.intellij.openapi.editor.event.EditorMouseEventArea
import com.intellij.openapi.editor.event.EditorMouseMotionListener
import com.intellij.openapi.editor.ex.EditorEx
import com.intellij.openapi.editor.markup.EffectType
import com.intellij.openapi.editor.markup.HighlighterTargetArea
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.text.StringUtil
import com.intellij.ui.JBColor
import com.intellij.ui.LightweightHint
import com.intellij.util.Alarm
import com.voltum.voltumscript.lsp.ShowEditorSemanticTokensAction.cleanup
import com.voltum.voltumscript.lsp.ShowEditorSemanticTokensAction.ourEscHandlerInstalled
import java.awt.Font

//object ShowEditorSemanticTokensAction

object ShowEditorSemanticTokensAction : EditorAction(object : EditorActionHandler() {
    override fun isEnabledForCaret(editor: Editor, caret: Caret, dataContext: DataContext): Boolean {
        return editor is EditorEx
    }

    override fun doExecute(editor: Editor, caret: Caret?, dataContext: DataContext) {
        if (editor.getUserData(Holder.LISTENER_ADDED) != null) cleanup(editor)

        val it = editor.highlighter.createIterator(0)
        while (!it.atEnd()) {
            val h = editor.markupModel.addRangeHighlighter(
                it.start, it.end,
                0, Holder.OUR_TEXT_ATTRIBUTES, HighlighterTargetArea.EXACT_RANGE
            )
            val tokenType = it.tokenType
            
            thisLogger().warn("Key data: ${h.getUserData<Any>(Key.findKeyByName("LSP_SEMANTIC_TOKEN_INFOS") as Key<Any>)}")
            h.putUserData(Holder.TOKEN_NAME, tokenType.toString())
            it.advance()
        }
        editor.addEditorMouseMotionListener(Holder.MOUSE_MOTION_LISTENER)
        editor.putUserData(Holder.LISTENER_ADDED, true)

        if (!ourEscHandlerInstalled) {
            ourEscHandlerInstalled = true
            val currentHandler = EditorActionManager.getInstance().getActionHandler(IdeActions.ACTION_EDITOR_ESCAPE)
            EditorActionManager.getInstance().setActionHandler(IdeActions.ACTION_EDITOR_ESCAPE, EscapeHandler(currentHandler))
        }
    }
}) {
    

    private const val DELAY: Long = 200

    private var ourEscHandlerInstalled = false


    private fun cleanup(editor: Editor) {
        editor.putUserData(Holder.LISTENER_ADDED, null)
        editor.removeEditorMouseMotionListener(Holder.MOUSE_MOTION_LISTENER)
        for (rangeHighlighter in editor.markupModel.allHighlighters) {
            if (rangeHighlighter.getUserData(Holder.TOKEN_NAME) != null) rangeHighlighter.dispose()
        }
    }

    private object Holder {
        val TOKEN_NAME: Key<String> = Key.create("token.name")
        val LISTENER_ADDED: Key<Boolean> = Key.create("token.mouse.listener.added")
        val OUR_TEXT_ATTRIBUTES = TextAttributes(
            null, null,
            JBColor.MAGENTA, EffectType.ROUNDED_BOX, Font.PLAIN
        )
        private val ourAlarm = Alarm()

        val MOUSE_MOTION_LISTENER: EditorMouseMotionListener = object : EditorMouseMotionListener {
            override fun mouseMoved(e: EditorMouseEvent) {
                if (e.area != EditorMouseEventArea.EDITING_AREA || !e.isOverText) return
                val editor = e.editor
                val logicalPosition = e.logicalPosition
                val offset = e.offset
                for (highlighter in editor.markupModel.allHighlighters) {
                    val text = highlighter.getUserData(TOKEN_NAME)
                    if (!StringUtil.isEmpty(text) && (highlighter.startOffset < offset && highlighter.endOffset > offset || !logicalPosition.leansForward && highlighter.endOffset == offset || logicalPosition.leansForward && highlighter.startOffset == offset)) {
                        val hintOffset = highlighter.startOffset
                        ourAlarm.cancelAllRequests()
                        ourAlarm.addRequest({
                                                val hint = LightweightHint(HintUtil.createInformationLabel(text!!))
                                                val point = HintManagerImpl.getHintPosition(
                                                    hint, editor,
                                                    editor.offsetToLogicalPosition(hintOffset).leanForward(true), HintManager.ABOVE
                                                )
                                                (HintManager.getInstance() as HintManagerImpl).showEditorHint(hint, editor, point, 0, 0, false)
                                            }, DELAY)
                        break
                    }
                }
            }
        }
    }

    private class EscapeHandler(private val myDelegate: EditorActionHandler) : EditorActionHandler() {
        override fun isEnabledForCaret(editor: Editor, caret: Caret, dataContext: DataContext): Boolean {
            val hostEditor = if (editor is EditorWindow) editor.delegate else editor
            return hostEditor.getUserData(Holder.LISTENER_ADDED) != null || myDelegate.isEnabled(editor, caret, dataContext)
        }

        override fun doExecute(editor: Editor, caret: Caret?, dataContext: DataContext) {
            val hostEditor = if (editor is EditorWindow) editor.delegate else editor
            if (hostEditor.getUserData(Holder.LISTENER_ADDED) != null) {
                cleanup(hostEditor)
            } else {
                myDelegate.execute(editor, caret, dataContext)
            }
        }
    }
}
