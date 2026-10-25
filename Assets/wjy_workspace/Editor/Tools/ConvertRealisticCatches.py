from pathlib import Path
import struct,json,io,hashlib
import numpy as np
from PIL import Image

root=Path.cwd(); base=root/'Assets/wjy_workspace/Models/RealisticCatches'; work=root/'Assets/wjy_workspace/.work'
target=work/'CatchValidation/Assets/wjy_workspace/Models/RealisticCatches/Prepared'
types={5120:np.int8,5121:np.uint8,5122:np.int16,5123:np.uint16,5125:np.uint32,5126:np.float32}
widths={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}

def convert(name):
    raw=(base/'Source~'/f'{name}.glb').read_bytes();pos=12;doc=None;binary=None
    while pos<len(raw):
        size,kind=struct.unpack_from('<II',raw,pos);chunk=raw[pos+8:pos+8+size];pos+=8+size
        if kind==0x4e4f534a:doc=json.loads(chunk)
        elif kind==0x004e4942:binary=chunk
    assert doc and binary is not None
    assert set(doc.get('extensionsRequired',[])) <= {'KHR_materials_pbrSpecularGlossiness'},doc.get('extensionsRequired')
    def accessor(i):
        a=doc['accessors'][i];assert 'sparse' not in a
        v=doc['bufferViews'][a['bufferView']];dtype=np.dtype(types[a['componentType']]).newbyteorder('<');count=a['count'];w=widths[a['type']]
        x=np.ndarray((count,w),dtype=dtype,buffer=binary,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',w*dtype.itemsize),dtype.itemsize)).copy()
        if a.get('normalized') and a['componentType']!=5126:
            maximum=np.iinfo(dtype).max;x=np.maximum(x.astype(float)/maximum,-1)
        return x
    def matrix(n):
        if 'matrix' in n:return np.array(n['matrix']).reshape(4,4,order='F')
        x,y,z,w=n.get('rotation',[0,0,0,1]);r=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])
        m=np.eye(4);m[:3,:3]=r@np.diag(n.get('scale',[1,1,1]));m[:3,3]=n.get('translation',[0,0,0]);return m
    groups={}
    def visit(i,parent):
        node=doc['nodes'][i];world=parent@matrix(node)
        if 'mesh' in node:
            # The crab's AO floor belongs to the online display, not the animal.
            if name=='Crab' and 'plane' in doc['meshes'][node['mesh']].get('name','').lower():return
            for primitive in doc['meshes'][node['mesh']]['primitives']:
                assert primitive.get('mode',4)==4
                attrs=primitive['attributes'];p=accessor(attrs['POSITION']);p=(np.c_[p,np.ones(len(p))]@world.T)[:,:3]
                if 'NORMAL' in attrs:
                    n=accessor(attrs['NORMAL'])@np.linalg.inv(world[:3,:3]);n/=np.maximum(np.linalg.norm(n,axis=1)[:,None],1e-10)
                else:n=np.zeros_like(p)
                uv=accessor(attrs['TEXCOORD_0']) if 'TEXCOORD_0' in attrs else np.zeros((len(p),2));uv[:,1]=1-uv[:,1]
                indices=accessor(primitive['indices']).reshape(-1) if 'indices' in primitive else np.arange(len(p))
                if np.linalg.det(world[:3,:3])<0:indices=indices.reshape(-1,3)[:,[0,2,1]].reshape(-1)
                mat=primitive.get('material',0);g=groups.setdefault(mat,{'v':[],'i':[],'count':0})
                # Reflect X for Unity's left-handed coordinates, including triangle winding.
                p[:,0]*=-1;n[:,0]*=-1;indices=indices.reshape(-1,3)[:,[0,2,1]].reshape(-1)
                g['v'].append(np.c_[p,n,uv]);g['i'].append(indices+g['count']);g['count']+=len(p)
        for child in node.get('children',[]):visit(child,world)
    for i in doc['scenes'][doc.get('scene',0)]['nodes']:visit(i,np.eye(4))
    folder=target/name;folder.mkdir(parents=True,exist_ok=True)
    textures={}
    def texture(info,role):
        if not info:return ''
        tex=doc['textures'][info['index']];image=doc['images'][tex['source']];view=doc['bufferViews'][image['bufferView']];b=binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']]
        key=hashlib.sha256(b+role.encode()).hexdigest()[:12];filename=f'{role}_{key}.png'
        if filename not in textures:
            im=Image.open(io.BytesIO(b)).convert('RGBA');im.thumbnail((1024,1024))
            if role=='Metallic':
                a=np.array(im);out=np.zeros_like(a);out[:,:,0]=a[:,:,2];out[:,:,3]=255-a[:,:,1];im=Image.fromarray(out)
            im.save(folder/filename);textures[filename]=True
        return filename
    descriptors=[]
    for index,g in sorted(groups.items()):
        v=np.concatenate(g['v']).astype('<f4');idx=np.concatenate(g['i']).astype('<i4')
        filename=f'Mesh_{index:03}.bin';(folder/filename).write_bytes(struct.pack('<II',len(v),len(idx))+v.tobytes()+idx.tobytes())
        mat=doc.get('materials',[{}])[index];pbr=mat.get('pbrMetallicRoughness',{});spec=mat.get('extensions',{}).get('KHR_materials_pbrSpecularGlossiness',{})
        color=pbr.get('baseColorFactor',spec.get('diffuseFactor',[1,1,1,1]));baseInfo=pbr.get('baseColorTexture',spec.get('diffuseTexture'))
        descriptors.append({'mesh':filename,'name':f'Material_{index:03}','color':color,'baseTexture':texture(baseInfo,'Albedo'),'normalTexture':texture(mat.get('normalTexture'),'Normal'),'metallicTexture':texture(pbr.get('metallicRoughnessTexture'),'Metallic'),'metallic':pbr.get('metallicFactor',0),'smoothness':1-pbr.get('roughnessFactor',1-spec.get('glossinessFactor',0)),'alphaMode':mat.get('alphaMode','OPAQUE'),'cutoff':mat.get('alphaCutoff',.5),'doubleSided':mat.get('doubleSided',False)})
    (folder/'build.json').write_text(json.dumps({'name':name,'parts':descriptors},indent=2))
    print(name,'triangles',sum(len(np.concatenate(g['i']))//3 for g in groups.values()),'material groups',len(groups),'textures',len(textures))

for name in ['Fish','Turtle','Crab','Trash','Lobster','Seal','Whale']:convert(name)
