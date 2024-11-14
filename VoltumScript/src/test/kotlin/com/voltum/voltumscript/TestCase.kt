/*
 * Use of this source code is governed by the MIT license that can be
 * found in the LICENSE file.
 */

package com.voltum.voltumscript

import java.nio.file.Path
import java.nio.file.Paths

interface TestCase {
    val testFileExtension: String
    fun getTestDataPath(): String
    fun getTestName(lowercaseFirstLetter: Boolean): String

    companion object {
        const val testResourcesPath = "src/test/testData"

        @JvmStatic
        fun camelOrWordsToSnake(name: String): String {
            if (' ' in name) return name.trim().replace(" ", "_")

            return name.split("(?=[A-Z])".toRegex()).joinToString("_", transform = String::lowercase)
        }
    }
}

fun TestCase.pathToSourceTestFile(): Path =
    Paths.get("${TestCase.testResourcesPath}/${getTestDataPath()}/${getTestName(true)}.$testFileExtension")

fun TestCase.pathToGoldTestFile(): Path =
    Paths.get("${TestCase.testResourcesPath}/${getTestDataPath()}/${getTestName(true)}.txt")
