"""Offline reproducible Material Symbols instances. Requires fontTools==4.53.0.
Usage: python scripts/generate-symbol-assets.py --font ORIGINAL.ttf --codepoints ORIGINAL.codepoints
Inputs must be the exact official upstream files; normal library builds never run this script.
"""
import argparse
import hashlib
import json
from pathlib import Path
import fontTools
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

PIN = '737e3324305806514d7909874fa1818ae1808232'
HASHES = ['95b24392bb49efd1bc3e92cff4e2452ad094461bab7c97e7d8723fab97e330ca',
          '225bd09137103cb7746bc93dc08d08764c9f0c3bd04f4b958d4a3c3c19432dd6']
p = argparse.ArgumentParser()
p.add_argument('--font', type=Path, required=True)
p.add_argument('--codepoints', type=Path, required=True)
a = p.parse_args()
assert fontTools.__version__ == '4.53.0', 'Pin fontTools==4.53.0 for reproducibility'
for file, digest in zip([a.font, a.codepoints], HASHES):
    assert hashlib.sha256(file.read_bytes()).hexdigest() == digest, 'Wrong upstream asset: ' + str(file)
root = Path(__file__).resolve().parent.parent
out = root / 'src/Avalonia.Material3/Assets/Icons'
out.mkdir(parents=True, exist_ok=True)
entries = [line.split() for line in a.codepoints.read_text().splitlines()]
manifest = {'upstream': 'google/material-design-icons', 'commit': PIN, 'license': 'Apache-2.0',
            'originalSha256': HASHES[0], 'codepointsSha256': HASHES[1], 'fontTools': '4.53.0',
            'modification': 'Static FILL instances, renamed families; GRAD0/opsz24/wght400. No icon redrawing.', 'instances': []}
for fill, label in [(0, 'Unfilled'), (1, 'Filled')]:
    font = TTFont(a.font, recalcTimestamp=False)
    font = instantiateVariableFont(font, {'FILL': fill, 'GRAD': 0, 'opsz': 24, 'wght': 400}, inplace=True)
    family = 'Material Symbols Rounded ' + label
    for n in font['name'].names:
        text = {1: family, 2: 'Regular', 4: family, 6: 'MaterialSymbolsRounded-' + label,
                16: family, 17: 'Regular'}.get(n.nameID)
        if text:
            n.string = text.encode(n.getEncoding())
    target = out / ('MaterialSymbolsRounded-' + label + '.ttf')
    font.save(target, reorderTables=True)
    manifest['instances'].append({'file': target.name, 'family': family, 'FILL': fill,
                                 'sha256': hashlib.sha256(target.read_bytes()).hexdigest()})
(out / 'MaterialSymbolsRounded.codepoints').write_bytes(a.codepoints.read_bytes())
(out / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf8')
lines = ['// Generated from the official pinned codepoints by scripts/generate-symbol-assets.py.',
         '// Google LLC, Apache-2.0; see Assets/Icons/LICENSE.txt and manifest.json.',
         'namespace Avalonia.Material3.Controls;', '', 'internal static class MaterialSymbolCodepoints', '{',
         '    private static readonly Dictionary<string, int> Values = new(StringComparer.Ordinal)', '    {']
for name, cp in entries:
    lines.append('        ["' + name + '"] = 0x' + cp.upper() + ',')
lines += ['    };', '', '    public static bool TryGet(string name, out int codepoint) => Values.TryGetValue(name, out codepoint);', '}']
(root / 'src/Avalonia.Material3/Controls/MaterialSymbolCodepoints.cs').write_text('\n'.join(lines) + '\n', encoding='utf8')
print(json.dumps(manifest, indent=2))
