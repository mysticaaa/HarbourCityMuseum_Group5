using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZxhMuseum;
public static class ShipGeometryAudit
{
 public static void Run()
 {
  try {
   EditorSceneManager.OpenScene("Assets/Scenes/Museum.unity");
   var source=UnityEngine.Object.FindObjectOfType<QuestExhibitSetup>().ship;
   var bounds=ExhibitInspectionHint.GetBounds(source);
   float scale=14/Mathf.Max(bounds.size.x,bounds.size.z);
   var yaw=bounds.size.x>bounds.size.z?Quaternion.Euler(0,90,0):Quaternion.identity;
   var histogram=new Dictionary<int,float>();
   var points=new List<Vector3>();
   var details=new List<string>();
   foreach(var f in source.GetComponentsInChildren<MeshFilter>(true)) {
    var vertices=f.sharedMesh.vertices.Select(v=>yaw*((f.transform.TransformPoint(v)-bounds.center)*scale)).ToArray();points.AddRange(vertices);
    var meshBounds=new Bounds(vertices[0],Vector3.zero);foreach(var p in vertices)meshBounds.Encapsulate(p);
    details.Add("MESH "+f.name+" min "+meshBounds.min+" max "+meshBounds.max);
    var t=f.sharedMesh.triangles;
    for(int i=0;i<t.Length;i+=3){var a=vertices[t[i]];var b=vertices[t[i+1]];var c=vertices[t[i+2]];var cross=Vector3.Cross(b-a,c-a);if(cross.magnitude<.00001f||Mathf.Abs(cross.normalized.y)<.95f)continue;int key=Mathf.RoundToInt((a.y+b.y+c.y)/3*20);if(!histogram.ContainsKey(key))histogram[key]=0;histogram[key]+=cross.magnitude*.5f;}
   }
   File.WriteAllLines("Logs/ZXH-ShipGeometry.txt",new[]{"SOURCE "+bounds,"FACTOR "+scale,"YAW "+yaw.eulerAngles,"NORMALIZED min "+new Vector3(points.Min(v=>v.x),points.Min(v=>v.y),points.Min(v=>v.z))+" max "+new Vector3(points.Max(v=>v.x),points.Max(v=>v.y),points.Max(v=>v.z))}.Concat(histogram.OrderByDescending(k=>k.Value).Take(20).Select(k=>"PLANE Y="+k.Key/20f+" AREA="+k.Value)));
   File.AppendAllLines("Logs/ZXH-ShipGeometry.txt",details);
   var root=new GameObject("Audit visual");
   foreach(var f in source.GetComponentsInChildren<MeshFilter>(true)){
    var go=new GameObject(f.name);go.layer=31;go.transform.SetParent(root.transform,false);go.transform.position=yaw*((f.transform.position-bounds.center)*scale);go.transform.rotation=yaw*f.transform.rotation;go.transform.localScale=f.transform.lossyScale*scale;go.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=f.GetComponent<MeshRenderer>().sharedMaterials;
   }
   var camera=new GameObject("Audit camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;camera.orthographic=true;camera.orthographicSize=8;
   RenderSettings.ambientLight=Color.white;
   foreach(var name in new[]{"top","side","stern"}){
    camera.transform.position=name=="top"?new Vector3(0,18,0):name=="side"?new Vector3(18,0,0):new Vector3(0,0,18);camera.transform.LookAt(Vector3.zero);
    var rt=new RenderTexture(1200,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1200,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,1000),0,0);tex.Apply();File.WriteAllBytes("Logs/ZXH-Ship-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
   }
   EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
