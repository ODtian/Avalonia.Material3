// Explicit diagnostic launch: source-proven faces share one physical origin and
// baseline. Paragraph placement and letter tracking remain separate inputs.
package org.pixivyou.m3reference

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Rect
import android.graphics.Typeface
import android.graphics.text.TextRunShaper
import android.os.Build
import android.util.Log
import android.view.View
import java.io.File
import java.security.MessageDigest

private const val SOURCE_SHA = "9CA9DEBB09459BF4E3E7F826F5CD0F35F253902B85684921FCE2BA3F28DD0F50"
private const val STATIC_SHA = "BC75B0FDA23E7859E81034E2571126341636CD9C8853B66A57D51D17D094433F"
private const val GALLERY_SHA = "AA6F953F06F8070C8CB258E686743EF3214A38B612B946FB5EBCD79772FFF5A7"

private fun hash(bytes: ByteArray) = MessageDigest.getInstance("SHA-256")
    .digest(bytes).joinToString("") { "%02X".format(it.toInt() and 255) }

internal enum class ProbeFace { Default, Variable400, Static400, Gallery400 }

internal fun probeTypeface(context: Context, mode: ProbeFace): Typeface = when (mode) {
    ProbeFace.Default -> Typeface.create(Typeface.DEFAULT, Typeface.NORMAL)
    ProbeFace.Variable400 -> {
        val file = File("/system/fonts/Roboto-Regular.ttf")
        check(hash(file.readBytes()) == SOURCE_SHA)
        Typeface.Builder(file).setWeight(400).setItalic(false)
            .setFontVariationSettings("'wght' 400, 'wdth' 100, 'ital' 0").build()
    }
    ProbeFace.Static400 -> {
        // Copy the already verified derived asset into this diagnostic host.
        context.assets.open("Roboto-Clock400.ttf").use { check(hash(it.readBytes()) == STATIC_SHA) }
        Typeface.Builder(context.assets, "Roboto-Clock400.ttf")
            .setWeight(400).setItalic(false).build()
    }
    ProbeFace.Gallery400 -> {
        context.assets.open("GalleryRoboto400.ttf").use { check(hash(it.readBytes()) == GALLERY_SHA) }
        Typeface.Builder(context.assets, "GalleryRoboto400.ttf").setWeight(400).setItalic(false).build()
    }
}

internal fun probePaint(face: Typeface, physicalSize: Float) = Paint(Paint.ANTI_ALIAS_FLAG).apply {
    typeface = face
    textSize = physicalSize
    color = Color.rgb(29, 27, 32)
    textAlign = Paint.Align.LEFT
    textScaleX = 1f
    textSkewX = 0f
    letterSpacing = 0f
    isFakeBoldText = false
    isSubpixelText = false
    isLinearText = false
    hinting = Paint.HINTING_ON
}

// The coordinator captures this view through the existing hardware window.
// One face/digit per frame avoids local-origin differences between grid cells.
internal class NativeGlyphProbeView(context: Context, mode: ProbeFace, private val digit: String) : View(context) {
    private val face = probeTypeface(context, mode)
    private val paint = probePaint(face, 16f * resources.displayMetrics.density)
    private val faceName = mode.name
    private val originX = 128f
    private val baselineY = 192f
    private var logged = false

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        canvas.drawColor(Color.rgb(230, 224, 233))
        canvas.drawText(digit, originX, baselineY, paint)
        if (logged) return
        logged = true
        val bounds = Rect()
        paint.getTextBounds(digit, 0, digit.length, bounds)
        val matrix = FloatArray(9)
        canvas.matrix.getValues(matrix)
        val location = IntArray(2)
        getLocationOnScreen(location)
        val digitWidths = FloatArray(10)
        paint.getTextWidths("0123456789", digitWidths)
        Log.i("M3NativeGlyphProbe", "face=$faceName digit=$digit hardware=${canvas.isHardwareAccelerated} " +
            "physicalSize=${paint.textSize} density=${resources.displayMetrics.density} origin=$originX,$baselineY " +
            "windowOrigin=${location.contentToString()} matrix=${matrix.contentToString()} flags=${paint.flags} " +
            "weight=${face.weight} italic=${face.isItalic} measure=${paint.measureText(digit)} " +
            "fontMetrics=${paint.fontMetricsInt} abstractBounds=$bounds digitWidths=${digitWidths.contentToString()}")
        if (Build.VERSION.SDK_INT >= 31) {
            val glyphs = TextRunShaper.shapeTextRun(digit, 0, digit.length, 0, digit.length, 0f, 0f, false, paint)
            for (index in 0 until glyphs.glyphCount()) {
                val font = glyphs.getFont(index)
                val buffer = font.buffer.duplicate().apply { position(0) }
                val bytes = ByteArray(buffer.remaining()).also { buffer.get(it) }
                Log.i("M3NativeGlyphProbe", "font-run face=$faceName digit=$digit glyph=${glyphs.getGlyphId(index)} " +
                    "position=${glyphs.getGlyphX(index)},${glyphs.getGlyphY(index)} advance=${glyphs.advance} " +
                    "file=${font.file} sha=${hash(bytes)} ttc=${font.ttcIndex} style=${font.style} axes=${font.axes?.contentToString()}")
            }
        }
    }
}
