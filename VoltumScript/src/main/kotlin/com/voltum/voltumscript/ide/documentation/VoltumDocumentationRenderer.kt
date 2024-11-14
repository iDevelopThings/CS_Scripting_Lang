package com.voltum.voltumscript.ide.documentation

import com.intellij.openapi.util.NlsSafe

class VoltumDocumentationRenderer(private val element: VoltumVirtualDocumentationComment?) {

  @NlsSafe
  fun render(): String? {
    if (element == null) {
      return null
    }

    return documentationMarkdownToHtml(element.content)
  }
}