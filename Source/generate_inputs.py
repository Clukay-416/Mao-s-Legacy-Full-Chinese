"""Build display dictionary and translated assets from the reviewed source JSON."""
import argparse, pathlib, json, base64, UnityPy

p=argparse.ArgumentParser();p.add_argument('original',type=pathlib.Path);p.add_argument('output',type=pathlib.Path)
a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
s=pathlib.Path(__file__).resolve().parent
mapping=json.loads((s/'translations.json').read_text('utf8'))
tsv='\n'.join(base64.b64encode(k.encode('utf8')).decode()+'\t'+base64.b64encode(v.encode('utf8')).decode() for k,v in mapping.items())
(a.output/'dictionary.tsv').write_text(tsv,'utf8')
translations=json.loads((s/'textassets.json').read_text('utf8'))
env=UnityPy.load(str(a.original/'China_Data/resources.assets'));seen=set()
for obj in env.objects:
    if obj.type.name!='TextAsset':continue
    data=obj.read()
    if data.m_Name not in translations:continue
    entry=translations[data.m_Name]
    text=data.m_Script if isinstance(data.m_Script,str) else data.m_Script.decode('utf8')
    newline='\r\n' if '\r\n' in text else '\n'
    assert text.rstrip('\r\n').split(newline)==entry['en'], 'Unsupported original TextAsset: '+data.m_Name
    assert len(entry['en'])==len(entry['zh']), 'Line count mismatch: '+data.m_Name
    ending=text[len(text.rstrip('\r\n')):]
    data.m_Script=newline.join(entry['zh'])+ending
    data.save();seen.add(data.m_Name)
assert seen==set(translations), 'Missing TextAssets: '+str(set(translations)-seen)
assets=a.output/'China_Data/resources.assets';assets.parent.mkdir(parents=True,exist_ok=True)
assets.write_bytes(env.file.save())
print('Prepared',len(mapping),'display entries and',len(seen),'TextAssets.')
