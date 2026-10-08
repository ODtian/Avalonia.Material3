"""Reproduce the locked Android default400 face for the typed clock painter.

Offline generation only. The original ReferenceUi variable font stays byte-identical.
"""
from pathlib import Path
import hashlib
import json
import shutil
import fontTools
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

root = Path(__file__).resolve().parents[1]
source = root / "samples/ReferenceUi/Assets/Fonts/Roboto-Regular.ttf"
source_sha = "9CA9DEBB09459BF4E3E7F826F5CD0F35F253902B85684921FCE2BA3F28DD0F50"
assert hashlib.sha256(source.read_bytes()).hexdigest().upper() == source_sha
assert fontTools.__version__ == "4.53.0"
original = TTFont(source, recalcTimestamp=False)
requested = {"wght": 400, "wdth": 100, "ital": 0}
axes = {axis.axisTag: requested[axis.axisTag] for axis in original["fvar"].axes}
instance = instantiateVariableFont(original, axes, inplace=False, optimize=True)
assert instance.getGlyphOrder() == original.getGlyphOrder()
assert instance.getBestCmap() == original.getBestCmap()
assert "fvar" not in instance
instance["OS/2"].usWeightClass = 400
instance["OS/2"].usWidthClass = 5
instance["OS/2"].fsSelection = (instance["OS/2"].fsSelection & ~0x21) | 0x40
instance["head"].macStyle &= ~3
for record in instance["name"].names:
    names = {1: "Roboto", 2: "Regular", 4: "Roboto Regular", 6: "Roboto-Regular", 16: "Roboto", 17: "Regular"}
    if record.nameID in names:
        record.string = names[record.nameID].encode(record.getEncoding())
destination = root / "src/Avalonia.Material3/Assets/Fonts"
destination.mkdir(parents=True, exist_ok=True)
output = destination / "Roboto-Clock400.ttf"
instance.save(output)
reloaded = TTFont(output)
assert reloaded.getGlyphOrder() == original.getGlyphOrder()
assert reloaded.getBestCmap() == original.getBestCmap()
shutil.copyfile(source.parent / "LICENSE.txt", destination / "LICENSE.txt")
manifest = {
    "license": "Apache-2.0",
    "scope": "Internal typed clock reproduction for the matched sourceSHA/default400 profile; no default font registration",
    "source": "Android reference emulator /system/fonts/Roboto-Regular.ttf",
    "sourceSha256": source_sha,
    "sourceAxes": [{"tag": axis.axisTag, "min": axis.minValue, "default": axis.defaultValue, "max": axis.maxValue} for axis in original["fvar"].axes],
    "output": output.name,
    "outputSha256": hashlib.sha256(output.read_bytes()).hexdigest().upper(),
    "family": "Roboto",
    "weight": 400,
    "widthClass": 5,
    "axes": axes,
    "glyphCount": len(original.getGlyphOrder()),
    "glyphOrderPreserved": True,
    "cmapPreserved": True,
    "generator": "fontTools 4.53.0 instantiateVariableFont",
    "script": "scripts/generate-clock-font.py",
    "licenseMetadata": "Original name IDs13/14 and all glyph IDs preserved",
}
(destination / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(json.dumps(manifest, indent=2))
