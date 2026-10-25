#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Offline preparation of downloaded, attributed models. Never runs automatically.
public static class WjyRealisticCatchBuilder
{
    [Serializable] private class Model { public string name; public Part[] parts; }
    [Serializable] private class Part
    {
        public string mesh, name, baseTexture, normalTexture, metallicTexture, alphaMode;
        public float[] color;
        public float metallic, smoothness, cutoff;
        public bool doubleSided;
    }
    private const string Prepared = "Assets/wjy_workspace/Models/RealisticCatches/Prepared";
    private const string Prefabs = "Assets/wjy_workspace/Resources/WjyFishingCatches";
    private static readonly string[] Names = { "Fish", "Turtle", "Crab", "Trash", "Lobster", "Seal", "Whale" };

    public static void Run()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').Contains("/.work/CatchValidation/"))
            throw new InvalidOperationException("Use the disposable CatchValidation project.");
        try
        {
            BuildAll();
            File.WriteAllText("../build-result.txt", "PASS: seven attributed models prepared as Unity meshes, materials and Resources prefabs.");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("../build-result.txt", "FAIL: " + e); EditorApplication.Exit(1); }
    }

    public static void BuildAll()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').Contains("/.work/CatchValidation/"))
            throw new InvalidOperationException("Use the disposable CatchValidation project.");
        Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
        foreach (string name in Names) Build(name);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }

    private static void Build(string name)
    {
        string folder = Prepared + "/" + name;
        var source = JsonUtility.FromJson<Model>(File.ReadAllText(folder + "/build.json"));
        var root = new GameObject(name + "_Realistic");
        var visual = new GameObject("ImportedGeometry"); visual.transform.SetParent(root.transform, false);
        foreach (var part in source.parts)
        {
            var mesh = ReadMesh(folder + "/" + part.mesh);
            if (part.doubleSided) MakeDoubleSided(mesh);
            mesh.name = name + "_" + part.name;
            string meshPath = folder + "/" + part.name + ".asset";
            AssetDatabase.CreateAsset(mesh, meshPath);
            var material = new Material(Shader.Find("Standard")) { name = name + "_" + part.name };
            material.color = new Color(part.color[0], part.color[1], part.color[2], part.color[3]);
            material.SetFloat("_Metallic", part.metallic); material.SetFloat("_Glossiness", part.smoothness);
            if (!string.IsNullOrEmpty(part.baseTexture)) material.mainTexture = Texture(folder + "/" + part.baseTexture, false, true);
            if (!string.IsNullOrEmpty(part.normalTexture))
            {
                material.SetTexture("_BumpMap", Texture(folder + "/" + part.normalTexture, true, false)); material.EnableKeyword("_NORMALMAP");
            }
            if (!string.IsNullOrEmpty(part.metallicTexture))
            {
                material.SetTexture("_MetallicGlossMap", Texture(folder + "/" + part.metallicTexture, false, false));
                material.EnableKeyword("_METALLICGLOSSMAP"); material.SetFloat("_GlossMapScale", part.smoothness);
            }
            if (part.alphaMode == "MASK")
            {
                material.SetFloat("_Mode",1); material.SetInt("_SrcBlend",(int)BlendMode.One); material.SetInt("_DstBlend",(int)BlendMode.Zero);
                material.EnableKeyword("_ALPHATEST_ON"); material.SetFloat("_Cutoff",part.cutoff);material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
            }
            else if (part.alphaMode == "BLEND")
            {
                material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite",0);material.EnableKeyword("_ALPHAPREMULTIPLY_ON");material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
            }
            AssetDatabase.CreateAsset(material,folder+"/"+part.name+".mat");
            var obj = new GameObject(part.name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(visual.transform,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        var initial = BoundsOf(root);
        if (name == "Fish" || name == "Trash")
        {
            var size = initial.size;
            Vector3 longest = size.x > size.y && size.x > size.z ? Vector3.right : size.z > size.y ? Vector3.forward : Vector3.up;
            if (name == "Fish") longest = -longest;
            visual.transform.localRotation = Quaternion.FromToRotation(longest,Vector3.up);
        }
        if (name == "Seal") visual.transform.localRotation = Quaternion.Euler(0,180,0);
        if (name == "Whale") visual.transform.localRotation = Quaternion.Euler(0,180,0);
        var bounds = BoundsOf(root);
        float sizeTarget = name == "Whale" || name == "Lobster" ? .42f : name == "Trash" ? .26f : .32f;
        float scale = sizeTarget/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
        visual.transform.localScale=Vector3.one*scale;
        visual.transform.localPosition=-bounds.center*scale+Vector3.down*(bounds.size.y*scale*.5f+.015f);
        PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/"+name+".prefab");
        var final = BoundsOf(root);
        if (final.size.magnitude <= .01f || final.size.magnitude > 1f || final.max.y > .001f) throw new Exception("Invalid hook bounds: "+name);
        Debug.Log("BUILT "+name+" parts="+source.parts.Length+" size="+final.size);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static Texture2D Texture(string path, bool normal, bool srgb)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        bool changed = importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) || importer.sRGBTexture != srgb || importer.maxTextureSize != 1024;
        if (changed)
        {
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=srgb;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Bounds BoundsOf(GameObject obj)
    {
        var renderers=obj.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);return bounds;
    }
    private static Mesh ReadMesh(string path)
    {
        using(var r=new BinaryReader(File.OpenRead(path)))
        {
            int count=r.ReadInt32(), indexCount=r.ReadInt32();var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
                normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
            }
            var indices=new int[indexCount];for(int i=0;i<indexCount;i++)indices[i]=r.ReadInt32();
            var mesh=new Mesh();if(count>65535)mesh.indexFormat=IndexFormat.UInt32;
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=indices;
            if(normals[0].sqrMagnitude<.1f)mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }

    private static void MakeDoubleSided(Mesh mesh)
    {
        var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var triangles=mesh.triangles;int count=vertices.Length;
        var v=new Vector3[count*2];var n=new Vector3[count*2];var t=new Vector2[count*2];var indices=new int[triangles.Length*2];
        for(int i=0;i<count;i++){v[i]=v[i+count]=vertices[i];n[i]=normals[i];n[i+count]=-normals[i];t[i]=t[i+count]=uv[i];}
        Array.Copy(triangles,indices,triangles.Length);
        for(int i=0;i<triangles.Length;i+=3){int j=triangles.Length+i;indices[j]=triangles[i]+count;indices[j+1]=triangles[i+2]+count;indices[j+2]=triangles[i+1]+count;}
        mesh.Clear();if(v.Length>65535)mesh.indexFormat=IndexFormat.UInt32;mesh.vertices=v;mesh.normals=n;mesh.uv=t;mesh.triangles=indices;mesh.RecalculateTangents();mesh.RecalculateBounds();
    }
}
#endif
