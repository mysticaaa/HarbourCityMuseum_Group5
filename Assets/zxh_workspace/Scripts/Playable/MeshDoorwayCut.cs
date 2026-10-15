using System.Collections.Generic;
using UnityEngine;
namespace ZxhMuseum
{
    // Subtract an axis-aligned doorway without removing whole imported wall triangles.
    public static class MeshDoorwayCut
    {
        private struct Vertex
        {
            public Vector3 p,n; public Vector2 uv;
            public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};
        }
        public static void Apply(Mesh mesh,Bounds box)
        {
            var vertices=new List<Vector3>(mesh.vertices);var normals=new List<Vector3>(mesh.normals);var uv=new List<Vector2>(mesh.uv);var result=new List<int>();
            var indices=mesh.triangles;
            Vector3[] directions={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            float[] limits={box.max.x,-box.min.x,box.max.y,-box.min.y,box.max.z,-box.min.z};
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                var triangleBounds=new Bounds(a,Vector3.zero);triangleBounds.Encapsulate(b);triangleBounds.Encapsulate(c);
                if(!triangleBounds.Intersects(box)){result.Add(indices[i]);result.Add(indices[i+1]);result.Add(indices[i+2]);continue;}
                var inside=new List<Vertex>();
                for(int j=0;j<3;j++){int index=indices[i+j];inside.Add(new Vertex{p=vertices[index],n=normals[index],uv=uv[index]});}
                for(int plane=0;plane<6 && inside.Count>=3;plane++)
                {
                    var next=new List<Vertex>();var outside=new List<Vertex>();
                    for(int j=0;j<inside.Count;j++)
                    {
                        var start=inside[j];var end=inside[(j+1)%inside.Count];
                        float sd=Vector3.Dot(directions[plane],start.p)-limits[plane],ed=Vector3.Dot(directions[plane],end.p)-limits[plane];
                        if(sd<=0)next.Add(start);else outside.Add(start);
                        if((sd<0 && ed>0)||(sd>0 && ed<0))
                        {var crossing=Vertex.Lerp(start,end,sd/(sd-ed));next.Add(crossing);outside.Add(crossing);}
                    }
                    if(outside.Count>=3)
                    {
                        int first=vertices.Count;
                        foreach(var v in outside){vertices.Add(v.p);normals.Add(v.n);uv.Add(v.uv);}
                        for(int j=1;j<outside.Count-1;j++){result.Add(first);result.Add(first+j);result.Add(first+j+1);}
                    }
                    inside=next;
                }
            }
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(result,0);Compact(mesh);
        }
        public static void Compact(Mesh mesh)
        {
            var oldVertices=mesh.vertices;var oldNormals=mesh.normals;var oldUv=mesh.uv;var oldIndices=mesh.triangles;
            var map=new Dictionary<int,int>();var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            var indices=new int[oldIndices.Length];
            for(int i=0;i<indices.Length;i++)
            {
                int old=oldIndices[i];
                if(!map.TryGetValue(old,out int index)){index=vertices.Count;map.Add(old,index);vertices.Add(oldVertices[old]);normals.Add(oldNormals[old]);uv.Add(oldUv[old]);}
                indices[i]=index;
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
            mesh.RecalculateBounds();if(indices.Length>0)mesh.RecalculateTangents();
        }
    }
}
