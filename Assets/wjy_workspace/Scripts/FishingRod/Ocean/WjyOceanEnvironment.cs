using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wjy.Museum
{
    /// <summary>Local origin is the centre of the dry deck. The open bow faces positive Z.</summary>
    [DisallowMultipleComponent]
    public sealed class WjyOceanEnvironment : MonoBehaviour
    {
        public const float SeaLevel = -.32f;
        public const float RaftHalfWidth = 1.55f;
        public const float RaftHalfLength = 1.65f;

        // Match the two vertex waves in WjyOceanRaftSurface so the float follows the visible water.
        internal static float WaveHeight(Vector3 local, float time)
        {
            float swell = .011f * Mathf.Sin(local.x * .71f + local.z * .97f + time * .83f)
                        + .007f * Mathf.Sin(local.x * -1.73f + local.z * .58f - time * .62f);
            float distance = new Vector2(local.x, local.z).magnitude;
            return SeaLevel + swell * Mathf.SmoothStep(.65f, 2.2f, Mathf.InverseLerp(7f, 30f, distance));
        }

        private readonly List<Object> owned = new List<Object>();
        private AudioSource surf;
        private const float SurfVolume = .46f;

        private void Awake()
        {
            var shader = Resources.Load<Shader>("WjyOceanRaftSurface");
            if (!shader)
            {
                Debug.LogError("Missing the ocean environment shader.", this);
                return;
            }
            var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name = "Twilight and moon";
            sky.transform.SetParent(transform, false);
            sky.transform.localScale = Vector3.one * 138f;
            var sphereCollider = sky.GetComponent<Collider>();
            sphereCollider.enabled = false;
            Destroy(sphereCollider);
            FinishRenderer(sky.GetComponent<MeshRenderer>(), MaterialFor(shader, 0));
            var sea = AddMesh("Moonlit sea", CreateSea(), MaterialFor(shader, 1));
            sea.localPosition = Vector3.up * SeaLevel;
            AddMesh("Lashed cedar raft", BuildRaft(), MaterialFor(shader, 2));

            var floor = new GameObject("Walkable deck");
            floor.transform.SetParent(transform, false);
            var deck = floor.AddComponent<BoxCollider>();
            deck.size = new Vector3(RaftHalfWidth * 2f, .28f, RaftHalfLength * 2f);
            deck.center = Vector3.down * .14f;

            var lamp = new GameObject("Hurricane lantern light");
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = new Vector3(-1.28f, .91f, -1.32f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .68f, .32f);
            light.range = 3.8f;
            light.intensity = .85f;
            light.shadows = LightShadows.None;
            // Light the visitor's rod, hands and catch in the same direction as the moon.
            var moon = new GameObject("Moonlight on the raft");
            moon.transform.SetParent(transform, false);
            moon.transform.localPosition = new Vector3(2.4f, 4.7f, 4.3f);
            var fill = moon.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(.70f, .82f, 1f);
            fill.intensity = 2.7f;
            fill.range = 25f;
            fill.shadows = LightShadows.None;
            var clip = Resources.Load<AudioClip>("WjyOpenWaterAmbience");
            if (clip)
            {
                surf = gameObject.AddComponent<AudioSource>();
                surf.playOnAwake = false;
                surf.clip = clip;
                surf.loop = true;
                surf.spatialBlend = 0f; // Diffuse stereo water bed; catch voices have their own position.
                surf.volume = 0f;
            }
        }

        private void OnEnable()
        {
            if (!surf) return;
            surf.volume = 0f;
            surf.Play();
        }

        private void Update()
        {
            if (surf && surf.volume < SurfVolume)
                surf.volume = Mathf.MoveTowards(surf.volume, SurfVolume, Time.unscaledDeltaTime * .45f);
        }

        private void OnDisable()
        {
            if (surf) surf.Stop();
        }

        private void OnDestroy()
        {
            foreach (var resource in owned) Destroy(resource);
        }

        private Material MaterialFor(Shader shader, int surface)
        {
            var material = new Material(shader);
            material.SetFloat("_Surface", surface);
            owned.Add(material);
            return material;
        }

        private static void FinishRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private Transform AddMesh(string meshName, Mesh mesh, Material material)
        {
            owned.Add(mesh);
            var child = new GameObject(meshName, typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            FinishRenderer(child.GetComponent<MeshRenderer>(), material);
            return child.transform;
        }

        private static Mesh CreateSea()
        {
            // Concentrate the tessellation beside the float rather than at the horizon.
            const int sides = 96, rings = 44;
            var vertices = new Vector3[1 + rings * sides];
            var indices = new List<int>(sides * rings * 6);
            for (int j = 0; j < rings; j++)
            {
                float t = (j + 1f) / rings;
                float radius = 6f * t + 60f * t * t;
                for (int i = 0; i < sides; i++)
                {
                    float a = 2f * Mathf.PI * i / sides;
                    int outer = 1 + j * sides + i;
                    int next = 1 + j * sides + (i + 1) % sides;
                    vertices[outer] = new Vector3(radius * Mathf.Cos(a), 0, radius * Mathf.Sin(a));
                    if (j == 0) indices.AddRange(new[] { 0, next, outer });
                    else
                    {
                        int inner = outer - sides, innerNext = next - sides;
                        indices.AddRange(new[] { inner, next, outer, inner, innerNext, next });
                    }
                }
            }
            var mesh = new Mesh { name = "Radial sea surface", vertices = vertices };
            mesh.SetTriangles(indices, 0);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(134, 1, 134));
            return mesh;
        }

        private static Mesh BuildRaft()
        {
            var wood = new RaftGeometry();
            var rope = new Color(.57f, .40f, .22f);
            var iron = new Color(.18f, .20f, .21f);
            var lantern = new Color(1f, .76f, .32f);
            var random = new System.Random(147);
            Vector3 up = Vector3.up;
            for (int i = 0; i < 11; i++)
            {
                float x = (i - 5) * .284f;
                float end = RaftHalfLength + (float)random.NextDouble() * .11f;
                float tone = (float)random.NextDouble();
                var tint = Color.Lerp(new Color(.38f,.24f,.15f), new Color(.69f,.52f,.32f), tone);
                var middle = new Vector3(x, -.151f, 0);
                wood.Spar(middle - Vector3.forward * end, middle + Vector3.forward * end, .156f, .148f, tint, 0, 14);
                foreach (float z in new[] { -1.36f, 1.36f })
                {
                    // Raised turns loop around each individual log at both ends.
                    var points = new Vector3[29];
                    for (int k=0; k<points.Length; k++)
                    {
                        float f=k/(float)(points.Length-1), a=f*4f*Mathf.PI;
                        points[k]=middle + new Vector3(Mathf.Cos(a)*.16f, Mathf.Sin(a)*.16f, z + .072f*f);
                    }
                    wood.Cord(points,.012f,rope);
                }
            }
            foreach (float z in new[] { -1.49f, 1.49f })
                wood.Spar(new Vector3(-1.64f,-.27f,z),new Vector3(1.64f,-.27f,z),.082f,.083f,new Color(.44f,.31f,.19f),0);

            // A low hand rope and storm lantern frame the stern, leaving the cast clear.
            var stern = new Color(.43f,.29f,.19f);
            var left = new Vector3(-1.4f,0,-1.36f);
            var right = new Vector3(1.4f,0,-1.36f);
            wood.Spar(left-up*.12f,left+up*1.22f,.05f,.037f,stern,0);
            wood.Spar(right-up*.12f,right+up*.55f,.048f,.036f,stern,0);
            var handline = new Vector3[15];
            for (int i=0;i<handline.Length;i++)
            {
                float t=i/(handline.Length-1f);
                handline[i]=Vector3.Lerp(left,right,t)+up*(.49f-.40f*t*(1-t));
            }
            wood.Cord(handline,.017f,rope);

            var coil = new Vector3[111];
            for(int i=0;i<coil.Length;i++)
            {
                float t=i/(coil.Length-1f), a=t*8f*Mathf.PI;
                float r=Mathf.Lerp(.105f,.30f,t);
                coil[i]=new Vector3(.83f+Mathf.Cos(a)*r,.018f,-1.14f+Mathf.Sin(a)*r*.75f);
            }
            wood.Cord(coil,.016f,rope);
            // An open, lashed bamboo socket marks where the visitor can take the rod.
            var socket = new Vector3(.66f, 0f, .88f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var rim = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * .072f;
                wood.Spar(socket + rim, socket + rim * 1.12f + up * .166f, .021f, .02f, stern, 0, 8);
            }
            foreach (float y in new[] { .055f, .108f })
            {
                var binding = new Vector3[21];
                for (int i = 0; i < binding.Length; i++)
                {
                    float a = i * Mathf.PI / 10f;
                    binding[i] = socket + new Vector3(Mathf.Cos(a) * .092f, y, Mathf.Sin(a) * .092f);
                }
                wood.Cord(binding, .01f, rope);
            }
            var basePoint=new Vector3(-1.28f,.81f,-1.32f);
            wood.Spar(left+up*1.15f,basePoint+up*.34f,.025f,.025f,iron,1);
            wood.Spar(basePoint+up*.21f,basePoint+up*.34f,.012f,.012f,iron,1);
            wood.Spar(basePoint,basePoint+up*.04f,.086f,.083f,iron,1);
            wood.Spar(basePoint+up*.04f,basePoint+up*.175f,.062f,.07f,lantern,2);
            wood.Spar(basePoint+up*.175f,basePoint+up*.218f,.093f,.045f,iron,1);
            for (int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;
                var offset=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.072f;
                wood.Spar(basePoint+offset,basePoint+offset+up*.185f,.005f,.005f,iron,1,5);
            }
            return wood.Mesh();
        }

        // Each spar uses one indexed tube with its own end caps. All the timber, knots,
        // coil and lantern share one mesh and one material.
        private sealed class RaftGeometry
        {
            private readonly List<Vector3> vertices=new List<Vector3>();
            private readonly List<Vector3> normals=new List<Vector3>();
            private readonly List<Color> colors=new List<Color>();
            private readonly List<Vector2> uv=new List<Vector2>();
            private readonly List<int> triangles=new List<int>();

            private void Vertex(Vector3 p,Vector3 normal,Color tint,int kind)
            {
                vertices.Add(p); normals.Add(normal); colors.Add(tint); uv.Add(new Vector2(kind,0));
            }

            public void Cord(Vector3[] path,float radius,Color tint)
            {
                for (int i=1;i<path.Length;i++)
                    Spar(path[i-1],path[i],radius,radius,tint,1,5,false);
            }

            public void Spar(Vector3 start,Vector3 end,float r0,float r1,Color tint,int kind,int sides=10,bool caps=true)
            {
                var tangent=(end-start).normalized;
                var side=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.forward:Vector3.up).normalized;
                var second=Vector3.Cross(tangent,side);
                int first=vertices.Count;
                for (int i=0;i<sides;i++)
                {
                    float a=i*2f*Mathf.PI/sides;
                    var n=side*Mathf.Cos(a)+second*Mathf.Sin(a);
                    Vertex(start+r0*n,n,tint,kind); Vertex(end+r1*n,n,tint,kind);
                    int current=first+i*2,next=first+((i+1)%sides)*2;
                    triangles.AddRange(new[]{current,next,next+1,current,next+1,current+1});
                }
                if(!caps) return;
                for (int cap=0;cap<2;cap++)
                {
                    int center=vertices.Count;
                    var normal=cap==0?-tangent:tangent;
                    var endTint=tint*.82f;
                    Vertex(cap==0?start:end,normal,endTint,kind);
                    for(int i=0;i<sides;i++) Vertex(vertices[first+i*2+cap],normal,endTint,kind);
                    for(int i=0;i<sides;i++)
                    {
                        int cur=center+1+i,next=center+1+(i+1)%sides;
                        triangles.AddRange(cap==0?new[]{center,next,cur}:new[]{center,cur,next});
                    }
                }
            }

            public Mesh Mesh()
            {
                var mesh=new Mesh{name="Cordage, hull and lantern"};
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors);
                mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
