import pathlib,json,hashlib,shutil,argparse
from make_delta import make_delta
p=argparse.ArgumentParser();p.add_argument('original',type=pathlib.Path);p.add_argument('rebuilt',type=pathlib.Path);p.add_argument('source',type=pathlib.Path);a=p.parse_args()
output=a.rebuilt.parent/'package';output.mkdir(parents=True,exist_ok=True)
manifest=json.loads((a.source/'manifest.json').read_text('utf8'))
def sha(f):return hashlib.sha256(f.read_bytes()).hexdigest()
for e in manifest['files']:
    target=a.rebuilt/e['path'];e['patched']=sha(target)
    payload=output/e['payload'];payload.parent.mkdir(parents=True,exist_ok=True)
    if e['original']:
        original=a.original/e['path'];assert sha(original)==e['original'],'Unsupported original: '+e['path']
        payload.write_bytes(make_delta(original.read_bytes(),target.read_bytes()))
    else:shutil.copy2(target,payload)
    e['payload_sha256']=sha(payload)
(output/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n','utf8')
for name in ['Install.cmd','Uninstall.cmd','Install.ps1','Uninstall.ps1','README.md','使用说明.md','NOTICE.md','BUILD.md','Build.ps1']:
    shutil.copy2(a.source/name,output/name)
for name in ['Source','预览']:shutil.copytree(a.source/name,output/name,dirs_exist_ok=True)
print('Rebuilt binary delta package:',output)
