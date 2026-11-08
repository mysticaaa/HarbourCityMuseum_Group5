from pathlib import Path
import re, json, hashlib
root=Path.cwd();work=root/'Assets/zzx_workspace/Integration~'
baseline=json.loads((work/'baseline.json').read_text())
changed=[name for name,h in baseline.items() if hashlib.sha256((root/name).read_bytes()).hexdigest()!=h]
assert changed==['Assets\\261016\\Museum_261016_3.unity'],changed
before=(work/'Museum_261016_3.before.unity').read_bytes()
after=(root/'Assets/261016/Museum_261016_3.unity').read_bytes()
restored=re.sub(rb'--- !u!1001 &209592756\r?\n.*?(?=--- !u!)',b'',after,flags=re.S)
restored=restored.replace(b'  - {fileID: 209592756}\r\n',b'')
assert restored==before,'Pre-existing scene bytes changed'
ids=re.findall(rb'^--- !u!\d+ &(\d+)',after,re.M)
assert len(ids)==len(set(ids)),'Duplicate object ID'
report='PASS: all pre-existing scene objects retained byte-for-byte.\nPASS: only target scene changed among baseline Assets, Packages and ProjectSettings files.\nPASS: no duplicate scene file IDs.\n'
clone=Path((work/'clone.txt').read_text())
survey=(clone/'Assets/zzx_workspace/Integration~/survey.txt').read_text()
if 'FirstDayExhibit active=True' in survey:
    exhibit=survey.split('FirstDayExhibit active=True')[1].split('\nXR ')[0]
    bounds=re.findall(r'bounds=Center: \(([^)]+)\), Extents: \(([^)]+)\)',exhibit)
    lo=[float('inf')]*3;hi=[-float('inf')]*3
    for c,e in bounds:
        c=list(map(float,c.split(',')));e=list(map(float,e.split(',')))
        for i in range(3):lo[i]=min(lo[i],c[i]-e[i]);hi[i]=max(hi[i],c[i]+e[i])
    assert lo[0]>4.45 and hi[0]<6.45 and lo[2]>-5.28 and hi[2]<2.74,(lo,hi)
    report+=f'PASS: exhibit renderer bounds {lo} to {hi}; inside room bounds, beyond existing fishing basin x=4.45.\n'
(work/'PreservationReport.txt').write_text(report)
print(report)
