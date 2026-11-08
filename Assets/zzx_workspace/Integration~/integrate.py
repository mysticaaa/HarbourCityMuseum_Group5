from pathlib import Path
import re, uuid, shutil, json, hashlib
root=Path.cwd(); work=root/'Assets/zzx_workspace/Integration~'
scene=root/'Assets/261016/Museum_261016_3.unity'
original=scene.read_bytes(); (work/'Museum_261016_3.before.unity').write_bytes(original)
def meta(path,guid):
    Path(str(path)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\n',encoding='utf-8')
hint=root/'Assets/zzx_workspace/FirstDayExhibit/Scripts/FirstDayInspectionHint.cs'
hintguid=uuid.uuid4().hex; meta(hint,hintguid)
prefab=root/'Assets/zzx_workspace/FirstDayExhibit/Prefabs/FirstDayExhibit_261016.prefab'
prefabguid=uuid.uuid4().hex
prefabtext=(prefab.parent/'FirstDayExhibit.prefab').read_text(encoding='utf-8-sig')
assert prefabtext.count('13e3efb4e96ddfc4dbc80708fe5f76b0')==2
prefabtext=prefabtext.replace('13e3efb4e96ddfc4dbc80708fe5f76b0',hintguid)
prefab.write_text(prefabtext,encoding='utf-8'); meta(prefab,prefabguid)
source=(root/'Assets/zzx_workspace/Scenes/Museum_FirstDay_ZZX.unity').read_text(encoding='utf-8-sig')
instance=re.search(r'--- !u!1001 &209592756\n.*?(?=--- !u!)',source,re.S).group()
instance=instance.replace('7bbb11bfb50de6e418d036672e7a5f3b',prefabguid)
instance=re.sub(r'    m_AddedComponents:.*?(?=  m_SourcePrefab:)', '    m_AddedComponents: []\n', instance,flags=re.S)
instance=instance.replace('propertyPath: m_LocalPosition.x\n      value: 3.5','propertyPath: m_LocalPosition.x\n      value: 5.7')
instance=instance.replace('propertyPath: m_LocalPosition.z\n      value: -0.75','propertyPath: m_LocalPosition.z\n      value: 0.5')
instance=instance.replace('propertyPath: m_LocalEulerAnglesHint.y\n      value: 0','propertyPath: m_LocalEulerAnglesHint.y\n      value: 90')
components=[]
for block in re.split(r'(?=--- !u!)',prefabtext):
    if '  m_InteractionManager:' in block:
        components.append(re.search(r'&([0-9]+)',block).group(1))
assert len(components)==6
overrides=''.join(f'    - target: {{fileID: {fid}, guid: {prefabguid}, type: 3}}\n      propertyPath: m_InteractionManager\n      value: \n      objectReference: {{fileID: 496874954}}\n' for fid in components)
instance=instance.replace('    m_RemovedComponents:',overrides+'    m_RemovedComponents:')
assert b'&209592756\r\n' not in original
newline=b'\r\n' if b'\r\n' in original else b'\n'
marker=b'--- !u!1660057539 &9223372036854775807'
result=original.replace(marker,instance.encode().replace(b'\n',newline)+marker)
result+=b'  - {fileID: 209592756}'+newline
scene.write_bytes(result)
check=root/'Assets/zzx_workspace/Editor/FirstDayMuseumIntegrationChecks.cs'; meta(check,uuid.uuid4().hex)
clone=Path((work/'clone.txt').read_text())
for p in (hint,Path(str(hint)+'.meta'),prefab,Path(str(prefab)+'.meta'),scene,check,Path(str(check)+'.meta')):
    shutil.copyfile(p,clone/p.relative_to(root))
print('Added exhibit at (5.7, 0.145, 0.5), Y=90. Explicit shared manager on six interactions. Original scene bytes retained.')
