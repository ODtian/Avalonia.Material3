"""Offline static document-font instances; fontTools==4.53.0. Never invoked by builds/runtime.
Usage: python scripts/generate-gallery-fonts.py --roboto ORIGINAL.ttf --cjk ORIGINAL.ttf
See samples/Gallery/Assets/Fonts/manifest.json for exact Google source/license pins.
"""
import argparse,hashlib,json
from pathlib import Path
from fontTools import __version__
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
p=argparse.ArgumentParser();p.add_argument('--roboto',type=Path,required=True);p.add_argument('--cjk',type=Path,required=True);a=p.parse_args()
assert __version__=='4.53.0'
out=Path(__file__).resolve().parent.parent/'samples/Gallery/Assets/Fonts'
m=json.loads((out/'manifest.json').read_text(encoding='utf-8-sig'));m['instances']=[]
for source,family,axes,digest in [(a.roboto,'Roboto',{'wdth':100},m['Roboto']['sha256']),(a.cjk,'NotoSansSC',{},m['NotoSansSC']['sha256'])]:
 assert hashlib.sha256(source.read_bytes()).hexdigest().upper()==digest
 for weight in (400,500,700):
  font=TTFont(source,recalcTimestamp=False)
  font=instantiateVariableFont(font,dict(axes,wght=weight),inplace=True)
  face='Gallery Roboto' if family=='Roboto' else 'Gallery Noto Sans SC'
  style={400:'Regular',500:'Medium',700:'Bold'}[weight]
  for name in font['name'].names:
   text={1:face,2:style,4:face+' '+style,6:face.replace(' ','')+'-'+style,16:face,17:style}.get(name.nameID)
   if text: name.string=text.encode(name.getEncoding())
  target=out/(family+'-'+str(weight)+'.ttf');font.save(target,reorderTables=True)
  m['instances'].append({'file':target.name,'weight':weight,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf8')
