package com.voltum.voltumscript.ext

import com.google.gson.*
import com.google.gson.reflect.TypeToken
import com.google.gson.stream.JsonReader
import java.io.StringReader

object JsonUtils {

    // a `tryParseJsonObject<T>` which handles gson() serialize
    inline fun <reified T> tryParseJsonObject(content: String?, lenient: Boolean = true): T? {
        try {
            val jObj = parseJsonObject(content, lenient)
            val gson = Gson()
            return gson.fromJson(jObj, T::class.java)
        } catch (ignored: Exception) {
            return null
        }
    }

    inline fun <reified T> tryParseJsonArray(content: String?, lenient: Boolean = true): T? {
        try {
            val jObj = parseJsonArray(content, lenient)
            val gson = Gson()
            val sType = object : TypeToken<T>() {}.type
            return gson.fromJson<T>(jObj, sType)
        } catch (ignored: Exception) {
            return null
        }
    }

    fun tryParseJsonObject(content: String?, lenient: Boolean = true): JsonObject? =
        try {
            parseJsonObject(content, lenient)
        } catch (ignored: Exception) {
            null
        }
    fun tryParseJsonArray(content: String?, lenient: Boolean = true): JsonArray? =
        try {
            parseJsonArray(content, lenient)
        } catch (ignored: Exception) {
            null
        }

    fun getJsonReader(content: String?, lenient: Boolean = true): JsonReader {
        val jsonReader = JsonReader(StringReader(content ?: ""))
        jsonReader.isLenient = lenient
        return jsonReader
    }

    @Throws(JsonIOException::class, JsonSyntaxException::class, IllegalStateException::class)
    fun parseJsonArray(content: String?, lenient: Boolean = true): JsonArray {
        return getJsonReader(content, lenient).use { JsonParser.parseReader(it).asJsonArray }
    }

    @Throws(JsonIOException::class, JsonSyntaxException::class, IllegalStateException::class)
    fun parseJsonObject(content: String?, lenient: Boolean = true): JsonObject {
        return getJsonReader(content, lenient).use { JsonParser.parseReader(it).asJsonObject }
    }
}
