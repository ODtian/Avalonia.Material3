// npm install --prefix artifacts/reference/oracle @material/material-color-utilities@0.3.0
// node tests/ReferenceVectors/generate.mjs artifacts/reference artifacts/reference/oracle/node_modules
// Oracle: official Google MCU npm 0.3.0, git 6bda88814da380664aaecc163ecdb8ac8caebb0a.
// Role tones: AndroidX 11ece46a49d485c7644e53cb0684a611d7a0ec10.
// Source files are fetched from that pinned AndroidX commit, not the implementation.
import {readFileSync, writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {pathToFileURL} from 'node:url';
const [reference, modules] = process.argv.slice(2);
const {CorePalette, TonalPalette, Hct, hexFromArgb} = await import(pathToFileURL(resolve(modules, '@material/material-color-utilities/index.js')));
function mappings(dark) {
  const text = readFileSync(resolve(reference, `Color${dark ? 'Dark' : 'Light'}Tokens.kt`), 'utf8');
  const result = {};
  for (const match of text.matchAll(/val (\w+):[^]*?get\(\) = PaletteTokens\.(Primary|Secondary|Tertiary|NeutralVariant|Neutral|Error)(\d+)/g))
    result[match[1]] = [match[2], Number(match[3])];
  result.SurfaceTint = result.Primary;
  result.Shadow = ['Neutral', 0];
  return result;
}
const paletteNames = {Primary:'a1', Secondary:'a2', Tertiary:'a3', Neutral:'n1', NeutralVariant:'n2', Error:'error'};
const schemes = [];
for (const seed of [0xff6750a4, 0xffff0000, 0xff00ff00, 0xff0000ff, 0xff000000, 0xffffffff, 0xff808080, 0xff006c4c]) {
  const palette = CorePalette.of(seed);
  for (const dark of [false, true]) {
    const roles = {};
    for (const [role, [name,tone]] of Object.entries(mappings(dark)))
      roles[role] = hexFromArgb(palette[paletteNames[name]].tone(tone)).toUpperCase();
    schemes.push({seed:hexFromArgb(seed).toUpperCase(), dark, roles});
  }
}
const hct = [[0,0,0],[0,0,100],[27.408,113.358,53.233],[120,200,50],[360,50,50],[-30,70,80],[200,0,50],[280,100,1],[280,100,99]].map(([hue,chroma,tone]) => ({hue,chroma,tone,color:hexFromArgb(Hct.from(hue,chroma,tone).toInt()).toUpperCase()}));
const roundTrips = schemes.filter(x=>!x.dark).map(x=>{ const h = Hct.fromInt(Number.parseInt(x.seed.slice(1),16) | 0xff000000); return {color:x.seed,hue:h.hue,chroma:h.chroma,tone:h.tone}; });
const platformKeys = {Primary:[145,40],Secondary:[145,16],Tertiary:[205,24],Neutral:[145,4],NeutralVariant:[145,8],Error:[25,84]};
const platform = [false,true].map(dark=>({dark,roles:Object.fromEntries(Object.entries(mappings(dark)).map(([role,[name,tone]])=>[role,hexFromArgb(TonalPalette.fromHueAndChroma(...platformKeys[name]).tone(tone)).toUpperCase()]))}));
writeFileSync('tests/ReferenceVectors/mcu-0.3.0.json', JSON.stringify({oracle:'Google MCU 0.3.0 / 6bda88814da380664aaecc163ecdb8ac8caebb0a', roles:'AndroidX 11ece46a49d485c7644e53cb0684a611d7a0ec10 ColorLight/DarkTokens v0_210', schemes,hct,roundTrips,platformKeys,platform}, null, 2)+'\n');

const resources = {};
const typeText = readFileSync(resolve(reference, 'TypeScaleTokens.kt'), 'utf8');
for (const [,name,value] of typeText.matchAll(/inline val (\w+):[^\n]*\n\s*get\(\) = ([^\n]+)/g)) {
  const match = name.match(/^(.*?)(Size|LineHeight|Tracking|Weight)$/);
  if (!match) continue;
  const [,role,metric] = match;
  const suffix = {Size:'FontSize',LineHeight:'LineHeight',Tracking:'LetterSpacing',Weight:'FontWeight'}[metric];
  const number = metric === 'Weight' ? {WeightRegular:400,WeightMedium:500,WeightBold:700}[value.split('.').at(-1)] : Number(value.replace('.sp',''));
  if (!Number.isFinite(number)) throw Error(`Unrecognized token ${name}=${value}`);
  resources[`M3.${role}${suffix}`] = number;
}
for (const [,suffix,value] of readFileSync(resolve(reference, 'ShapeTokens.kt'),'utf8').matchAll(/val CornerValue(\w+) = CornerSize\(([\d.]+).dp\)/g))
  resources[`M3.Shape.Corner${suffix}`] = Number(value);
for (const [,name,value] of readFileSync(resolve(reference, 'ElevationTokens.kt'),'utf8').matchAll(/inline val (Level\d):[^]*?get\(\) = ([\d.]+).dp/g))
  resources[`M3.Elevation.${name}`] = Number(value);
for (const [,name,value] of readFileSync(resolve(reference, 'StateTokens.kt'),'utf8').matchAll(/const val (\w+) = ([\d.]+)f/g))
  resources[`M3.${name}`] = Number(value);
const motionText = readFileSync(resolve(reference, 'MotionTokens.kt'),'utf8');
const durations = Object.fromEntries([...motionText.matchAll(/const val (Duration\w+) = ([\d.]+)/g)].map(([,name,value])=>[`M3.Motion.${name}`,Number(value)]));
const easings = Object.fromEntries([...motionText.matchAll(/inline val (\w+)CubicBezier:[^]*?get\(\) = CubicBezierEasing\(([^)]+)\)/g)].map(([,name,values])=>[`M3.Motion.${name}`,values.split(',').map(x=>Number(x.trim().replace('f','')))]));
const springs = {};
for (const scheme of ['Expressive','Standard']) {
  const values = Object.fromEntries([...readFileSync(resolve(reference, `${scheme}MotionTokens.kt`),'utf8').matchAll(/const val Spring(\w+) = ([\d.]+)f/g)].map(([,name,value])=>[name,Number(value)]));
  springs[scheme] = Object.fromEntries(['DefaultSpatial','FastSpatial','SlowSpatial','DefaultEffects','FastEffects','SlowEffects'].map(name=>[name,[values[name+'Damping'],values[name+'Stiffness']]]));
}
const paletteValues = Object.fromEntries([...readFileSync(resolve(reference,'PaletteTokens.kt'),'utf8').matchAll(/inline val (\w+):[^]*?get\(\) = Color\(red = (\d+), green = (\d+), blue = (\d+)\)/g)].map(([,name,r,g,b])=>[name,'#'+[r,g,b].map(v=>Number(v).toString(16).padStart(2,'0')).join('').toUpperCase()]));
const staticSchemes = [false,true].map(dark=>({dark,roles:Object.fromEntries(Object.entries(mappings(dark)).map(([name,[palette,tone]])=>[name,paletteValues[palette+tone]]))}));
writeFileSync('tests/ReferenceVectors/androidx-tokens.json', JSON.stringify({source:'AndroidX 11ece46a49d485c7644e53cb0684a611d7a0ec10',resources,durations,easings,springs,staticSchemes},null,2)+'\n');
