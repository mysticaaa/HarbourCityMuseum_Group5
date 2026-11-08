from pathlib import Path
import shutil, tempfile, json, hashlib, re, uuid
root=Path.cwd()
work=root/'Assets/zzx_workspace/Integration~'
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
files=[p for d in ('Assets','Packages','ProjectSettings') for p in (root/d).rglob('*') if p.is_file() and work not in p.parents]
(work/'baseline.json').write_text(json.dumps({str(p.relative_to(root)):digest(p) for p in files}))
clone=Path(tempfile.mkdtemp(prefix='ZZX_Museum261016_'))
(work/'clone.txt').write_text(str(clone))
for d in ('Assets','Packages','ProjectSettings','Library'):
    shutil.copytree(root/d,clone/d,ignore=shutil.ignore_patterns('Integration~','*-lock','ShaderCache','Bee','TempArtifacts'))
# Only the disposable copy disables unrelated editor auto-configuration and hardware startup.
for p in (clone/'Assets').rglob('*.cs'):
    text=p.read_text(encoding='utf-8-sig',errors='replace')
    if '[InitializeOnLoadMethod]' in text:
        p.write_text(text.replace('[InitializeOnLoadMethod]','// Disabled in isolated validation copy.'),encoding='utf-8')
for p in (clone/'Assets').rglob('*.asset'):
    text=p.read_text(encoding='utf-8-sig',errors='replace')
    if 'm_InitManagerOnStart: 1' in text:
        p.write_text(text.replace('m_InitManagerOnStart: 1','m_InitManagerOnStart: 0'),encoding='utf-8')
print(clone)
