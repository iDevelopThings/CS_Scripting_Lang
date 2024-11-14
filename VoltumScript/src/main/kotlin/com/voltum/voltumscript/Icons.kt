package com.voltum.voltumscript

import com.intellij.icons.AllIcons
import com.intellij.openapi.util.IconLoader
import com.intellij.ui.ColorUtil
import com.intellij.ui.JBColor
import com.intellij.ui.LayeredIcon
import com.intellij.ui.LayeredIcon.Companion.layeredIcon
import com.intellij.ui.icons.RgbImageFilterSupplier
import com.intellij.util.IconUtil
import java.awt.Color
import java.awt.Component
import java.awt.Graphics
import java.awt.Graphics2D
import java.awt.image.RGBImageFilter
import javax.swing.Icon


object Icons {
    val Logo = load("/icons/Voltum.svg")
    val ModuleFolder = load("/icons/voltum_module.svg")

    // Marks

    val FINAL_MARK = AllIcons.Nodes.FinalMark
    val STATIC_MARK = AllIcons.Nodes.StaticMark
    val TEST_MARK = AllIcons.Nodes.JunitTestMark
    val FEATURE_CHECKED_MARK = AllIcons.Diff.GutterCheckBoxSelected
    val FEATURE_UNCHECKED_MARK = AllIcons.Diff.GutterCheckBox
    val FEATURE_CHECKED_MARK_GRAYED = FEATURE_CHECKED_MARK.grayed()
    val FEATURE_UNCHECKED_MARK_GRAYED = FEATURE_UNCHECKED_MARK.grayed()
    val FEATURES_SETTINGS = AllIcons.General.Settings

    val MODULE = load("/icons/nodes/module.svg")

    val TRAIT = load("/icons/nodes/trait.svg")
    val STRUCT = load("/icons/nodes/struct.svg")
    val UNION = load("/icons/nodes/union.svg")
    val ENUM = load("/icons/nodes/enum.svg")
    val TYPE_ALIAS = load("/icons/nodes/typeAlias.svg")
    val IMPL = load("/icons/nodes/impl.svg")
    val FUNCTION = load("/icons/nodes/function.svg")
    val MACRO = load("/icons/nodes/macro.svg")
    val MACRO2 = load("/icons/nodes/macro2.svg")
    val PROC_MACRO = load("/icons/nodes/macroP.svg")

    val CONSTANT = load("/icons/nodes/constant.svg")
    val MUT_STATIC = load("/icons/nodes/static.svg")
    val STATIC = MUT_STATIC.addFinalMark()

    val METHOD = load("/icons/nodes/method.svg")
    val ASSOC_FUNCTION = FUNCTION.addStaticMark()
    val ASSOC_CONSTANT = CONSTANT.addStaticMark()
    val ASSOC_TYPE_ALIAS = TYPE_ALIAS.addStaticMark()

    val ABSTRACT_METHOD = load("/icons/nodes/abstractMethod.svg")
    val ABSTRACT_ASSOC_FUNCTION = load("/icons/nodes/abstractFunction.svg").addStaticMark()
    val ABSTRACT_ASSOC_CONSTANT = load("/icons/nodes/abstractConstant.svg").addStaticMark()
    val ABSTRACT_ASSOC_TYPE_ALIAS = load("/icons/nodes/abstractTypeAlias.svg").addStaticMark()

    val ATTRIBUTE = AllIcons.Nodes.Annotationtype
    val MUT_ARGUMENT = AllIcons.Nodes.Parameter
    val ARGUMENT = MUT_ARGUMENT.addFinalMark()
    val MUT_BINDING = AllIcons.Nodes.Variable
    val BINDING = MUT_BINDING.addFinalMark()

    val FIELD = load("/icons/nodes/field.svg")
    val ENUM_VARIANT = load("/icons/nodes/enumVariant.svg")

    private fun load(path: String): Icon = IconLoader.getIcon(path, Icons::class.java)
}


fun Icon.addFinalMark(): Icon = layeredIcon(arrayOf(this, Icons.FINAL_MARK))
fun Icon.addStaticMark(): Icon = layeredIcon(arrayOf(this, Icons.STATIC_MARK))
fun Icon.addTestMark(): Icon = layeredIcon(arrayOf(this, Icons.TEST_MARK))

fun Icon.multiple(): Icon {
    val compoundIcon = LayeredIcon(2)
    compoundIcon.setIcon(this, 0, 2 * iconWidth / 5, 0)
    compoundIcon.setIcon(this, 1, 0, 0)
    return compoundIcon
}

fun Icon.grayed(): Icon =
    IconLoader.filterIcon(this, object : RgbImageFilterSupplier {
        override fun getFilter(): RGBImageFilter {
            return object : RGBImageFilter() {
                override fun filterRGB(x: Int, y: Int, rgb: Int): Int {
                    val color = Color(rgb, true)
                    return ColorUtil.toAlpha(color, (color.alpha / 2.2).toInt()).rgb
                }
            }
        }

    })

/**
 * Rotates the icon by the given angle, in degrees.
 *
 * **Important**: Do ***not*** rotate the icon by ±90 degrees (or any sufficiently close amount)!
 * The implementation of rotation by that amount in AWT is broken, and results in erratic shifts for composed
 * transformations. In other words, the (final) transformation matrix as a function of rotation angle
 * is discontinuous at those points.
 */
fun Icon.rotated(angle: Double): Icon {
    val q = this
    return object : Icon by this {
        override fun paintIcon(c: Component, g: Graphics, x: Int, y: Int) {
            val g2d = g.create() as Graphics2D
            try {
                g2d.translate(x.toDouble(), y.toDouble())
                g2d.rotate(Math.toRadians(angle), iconWidth / 2.0, iconHeight / 2.0)
                q.paintIcon(c, g2d, 0, 0)
            } finally {
                g2d.dispose()
            }
        }
    }
}
