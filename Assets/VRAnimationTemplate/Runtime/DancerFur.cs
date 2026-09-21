using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace VRAnimationTemplate
{
    /// <summary>One combined tube mesh, with skinned roots and damped strand motion.</summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class DancerFur : MonoBehaviour
    {
        public SkinnedMeshRenderer body;
        public DancerFurAsset groom;
        [Range(0, 1)] public float inertia = .9f;
        [Range(0, 30)] public float stiffness = .5f;
        [Range(0, 1)] public float gravity = .4f;
        public bool simulate = true;
        [Tooltip("Discrete collision against fitted body volumes plus the strand root surface plane.")]
        public bool bodyCollision = true;
        [System.Serializable] public struct Volume
        {
            public Transform bone;
            public Matrix4x4 bindPose;
            public Vector3 center, radii;
        }
        public Volume[] volumes;
        [HideInInspector] public Mesh previewMesh;
        Mesh baked, tubes;
        readonly List<Vector3> skin = new List<Vector3>();
        readonly List<Vector3> skinNormals = new List<Vector3>();
        NativeArray<Vector3> particles, previous, vertices, normals, skinNative, normalNative;
        NativeArray<DancerFurAsset.Strand> strandNative;
        NativeArray<int> offsetNative, vertexOffsetNative;
        NativeArray<Matrix4x4> volumeToNative, volumeFromNative;
        NativeArray<Vector2> ringNative;
        Vector3[] skinBuffer, normalBuffer;
        Color[] colors;
        int[] offsets, vertexOffsets;
        Matrix4x4[] toVolume, fromVolume;
        bool initialized;
        public int TriangleCount { get; private set; }
        public Mesh GeneratedMesh => tubes;

        void OnEnable() { if(Application.isPlaying && groom && body) ResetSimulation(); }
        void LateUpdate() { if (groom && body) Step(Time.deltaTime); }
        void OnDisable() { ReleaseSimulation(); }
        void OnDestroy() { ReleaseSimulation(); }
        public void ReleaseSimulation()
        {
            DisposeArrays();
            if (baked) { if (Application.isPlaying) Destroy(baked); else DestroyImmediate(baked); }
            if (tubes) { if (Application.isPlaying) Destroy(tubes); else DestroyImmediate(tubes); }
            baked=null; tubes=null; initialized=false;
            if(previewMesh) GetComponent<MeshFilter>().sharedMesh=previewMesh;
        }
        void DisposeArrays()
        {
            if(particles.IsCreated) particles.Dispose(); if(previous.IsCreated) previous.Dispose();
            if(vertices.IsCreated) vertices.Dispose(); if(normals.IsCreated) normals.Dispose();
            if(skinNative.IsCreated) skinNative.Dispose(); if(normalNative.IsCreated) normalNative.Dispose();
            if(strandNative.IsCreated) strandNative.Dispose(); if(offsetNative.IsCreated) offsetNative.Dispose();
            if(vertexOffsetNative.IsCreated) vertexOffsetNative.Dispose(); if(volumeToNative.IsCreated) volumeToNative.Dispose();
            if(volumeFromNative.IsCreated) volumeFromNative.Dispose(); if(ringNative.IsCreated) ringNative.Dispose();
        }
        public void ResetSimulation()
        {
            if (!groom || !body || groom.strands == null) return;
            var filter=GetComponent<MeshFilter>();
            if(!previewMesh && filter.sharedMesh && filter.sharedMesh != tubes) previewMesh=filter.sharedMesh;
            var coatProperties=new MaterialPropertyBlock(); body.GetPropertyBlock(coatProperties);
            coatProperties.SetColor("_BaseColor",groom.undercoatColor);
            coatProperties.SetColor("_SkinColor",groom.bareSkinColor); body.SetPropertyBlock(coatProperties);
            if (!baked) baked = new Mesh { name = "Fur body snapshot", hideFlags = HideFlags.DontSave };
            if (!tubes) { tubes = new Mesh { name = "Dynamic combined fur", hideFlags = HideFlags.DontSave }; tubes.MarkDynamic(); }
            offsets = new int[groom.strands.Length]; vertexOffsets = new int[offsets.Length];
            int points = 0, count = 0; var indices = new List<int>();
            for (int h = 0; h < offsets.Length; h++)
            {
                offsets[h] = points; vertexOffsets[h] = count;
                int segments = groom.strands[h].segments;
                for (int r = 0; r < segments; r++) for (int s = 0; s < groom.sides; s++)
                {
                    int a = count + r * groom.sides + s, b = count + r * groom.sides + (s + 1) % groom.sides;
                    indices.Add(a); indices.Add(a + groom.sides); indices.Add(b);
                    indices.Add(b); indices.Add(a + groom.sides); indices.Add(b + groom.sides);
                }
                points += segments + 1; count += (segments + 1) * groom.sides;
            }
            DisposeArrays();
            particles = new NativeArray<Vector3>(points,Allocator.Persistent); previous = new NativeArray<Vector3>(points,Allocator.Persistent);
            vertices = new NativeArray<Vector3>(count,Allocator.Persistent); normals = new NativeArray<Vector3>(count,Allocator.Persistent); colors = new Color[count];
            skinBuffer=new Vector3[body.sharedMesh.vertexCount]; normalBuffer=new Vector3[skinBuffer.Length];
            skinNative=new NativeArray<Vector3>(skinBuffer.Length,Allocator.Persistent); normalNative=new NativeArray<Vector3>(skinBuffer.Length,Allocator.Persistent);
            strandNative=new NativeArray<DancerFurAsset.Strand>(groom.strands,Allocator.Persistent);
            offsetNative=new NativeArray<int>(offsets,Allocator.Persistent); vertexOffsetNative=new NativeArray<int>(vertexOffsets,Allocator.Persistent);
            ringNative=new NativeArray<Vector2>(groom.sides,Allocator.Persistent);
            for(int s=0;s<groom.sides;s++) {float a=(s+.125f)*Mathf.PI*2/groom.sides;ringNative[s]=new Vector2(Mathf.Cos(a),Mathf.Sin(a));}
            for(int h=0;h<groom.strands.Length;h++) for(int r=0;r<=groom.strands[h].segments;r++) for(int s=0;s<groom.sides;s++)
                colors[vertexOffsets[h]+r*groom.sides+s]=Color.Lerp(groom.rootColor,groom.tipColor,groom.strands[h].shade*.55f+(float)r/groom.strands[h].segments*.45f);
            tubes.Clear(); tubes.indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            tubes.SetVertices(vertices); tubes.SetTriangles(indices, 0); tubes.SetColors(colors); TriangleCount = indices.Count / 3;
            toVolume = new Matrix4x4[volumes == null ? 0 : volumes.Length]; fromVolume = new Matrix4x4[toVolume.Length];
            volumeToNative=new NativeArray<Matrix4x4>(toVolume.Length,Allocator.Persistent); volumeFromNative=new NativeArray<Matrix4x4>(toVolume.Length,Allocator.Persistent);
            initialized = false; Step(0);
            GetComponent<MeshFilter>().sharedMesh = tubes;
        }
        public void Step(float dt)
        {
            if (!particles.IsCreated) { ResetSimulation(); return; }
            body.BakeMesh(baked); baked.GetVertices(skin); baked.GetNormals(skinNormals);
            var skinToWorld = body.transform.localToWorldMatrix;
            var worldToFur = transform.worldToLocalMatrix;
            bool reset = !initialized || dt > .15f || !simulate;
            dt = Mathf.Clamp(dt, 0, 1f / 30f);
            for (int i = 0; i < toVolume.Length; i++)
            {
                fromVolume[i] = volumes[i].bone.localToWorldMatrix * volumes[i].bindPose * Matrix4x4.TRS(volumes[i].center, Quaternion.identity, volumes[i].radii);
                toVolume[i] = fromVolume[i].inverse;
            }
            skin.CopyTo(skinBuffer); skinNormals.CopyTo(normalBuffer); skinNative.CopyFrom(skinBuffer); normalNative.CopyFrom(normalBuffer);
            volumeToNative.CopyFrom(toVolume); volumeFromNative.CopyFrom(fromVolume);
            new FurJob {particles=particles,previous=previous,vertices=vertices,normals=normals,skin=skinNative,skinNormals=normalNative,
                strands=strandNative,offsets=offsetNative,vertexOffsets=vertexOffsetNative,toVolume=volumeToNative,fromVolume=volumeFromNative,ring=ringNative,
                skinToWorld=skinToWorld,worldToFur=worldToFur,reset=reset,dt=dt,inertia=inertia,stiffness=stiffness,gravity=gravity,bodyCollision=bodyCollision,
                scale=transform.lossyScale.x,sides=groom.sides,taper=groom.taper,taperPower=groom.taperPower}.Schedule(groom.strands.Length,64).Complete();
            tubes.SetVertices(vertices); tubes.SetNormals(normals); tubes.RecalculateBounds();
            initialized = true;
        }
        [BurstCompile(CompileSynchronously=true)]
        struct FurJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<Vector3> particles,previous,vertices,normals;
            [ReadOnly] public NativeArray<Vector3> skin,skinNormals;
            [ReadOnly] public NativeArray<DancerFurAsset.Strand> strands;
            [ReadOnly] public NativeArray<int> offsets,vertexOffsets;
            [ReadOnly] public NativeArray<Matrix4x4> toVolume,fromVolume;
            [ReadOnly] public NativeArray<Vector2> ring;
            public Matrix4x4 skinToWorld,worldToFur;
            public bool reset,bodyCollision;
            public float dt,inertia,stiffness,gravity,scale,taper,taperPower;
            public int sides;
            public void Execute(int h)
            {
                var strand = strands[h]; var b = strand.barycentric; int o = offsets[h];
                var root = skinToWorld.MultiplyPoint3x4(skin[strand.a] * b.x + skin[strand.b] * b.y + skin[strand.c] * b.z);
                var n = skinToWorld.MultiplyVector(skinNormals[strand.a] * b.x + skinNormals[strand.b] * b.y + skinNormals[strand.c] * b.z).normalized;
                var down = Vector3.ProjectOnPlane(Vector3.down, n).normalized;
                var sideways = Vector3.Cross(n, down).normalized;
                var direction = (n * .70f + down * .65f + sideways * strand.curl).normalized;
                float length = strand.length * scale;
                float step = length / strand.segments;
                root += n * .0015f;
                particles[o] = previous[o] = root;
                for (int r = 1; r <= strand.segments; r++)
                {
                    int p = o + r; float t = (float)r / strand.segments;
                    var target = root + direction * (length * t) + sideways * (Mathf.Sin(t * 3.14f) * length * strand.curl * .25f);
                    if (reset) particles[p] = previous[p] = target;
                    else
                    {
                        var current = particles[p];
                        var velocity = (current - previous[p]) * Mathf.Pow(inertia, dt * 60);
                        particles[p] += velocity + (target - current) * Mathf.Min(.35f, stiffness * stiffness * dt * dt) + Vector3.down * (gravity * 9.81f * dt * dt);
                        previous[p] = current;
                    }
                }
                for (int iteration = 0; iteration < 2; iteration++)
                {
                    for (int r = 1; r <= strand.segments; r++)
                    {
                        int p = o + r; var d = particles[p] - particles[p - 1];
                        particles[p] = particles[p - 1] + (d.sqrMagnitude > 1e-12f ? d.normalized : direction) * step;
                        if (bodyCollision)
                        {
                            float radius = strand.diameter * .5f + .0015f;
                            // Prevent bending back through the attachment's local surface.
                            float signed = Vector3.Dot(particles[p] - root, n);
                            if (signed < radius) particles[p] += n * (radius - signed);
                            for (int v = 0; v < toVolume.Length; v++)
                            {
                                var local = toVolume[v].MultiplyPoint3x4(particles[p]);
                                if (local.sqrMagnitude < 1f)
                                    particles[p] = fromVolume[v].MultiplyPoint3x4(local.sqrMagnitude > 1e-8f ? local.normalized * 1.015f : Vector3.up * 1.015f);
                            }
                        }
                    }
                }
                for (int r = 0; r <= strand.segments; r++)
                {
                    int p = o + r; var tangent = (particles[o + Mathf.Min(r + 1, strand.segments)] - particles[o + Mathf.Max(r - 1, 0)]).normalized;
                    var right = Vector3.Cross(tangent, Mathf.Abs(tangent.y) < .9f ? Vector3.up : Vector3.right).normalized;
                    var up = Vector3.Cross(right, tangent).normalized;
                    float t = (float)r / strand.segments, radius = strand.diameter * .5f * (1-taper*Mathf.Pow(t,taperPower)) * scale;
                    for (int s = 0; s < sides; s++)
                    {
                        var radial = right * ring[s].x + up * ring[s].y;
                        int index = vertexOffsets[h] + r * sides + s;
                        vertices[index] = worldToFur.MultiplyPoint3x4(particles[p] + radial * radius);
                        normals[index] = worldToFur.MultiplyVector(radial).normalized;
                    }
                }
            }
        }
        public bool IsFinite()
        {
            foreach (var p in particles) if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.sqrMagnitude)) return false;
            return true;
        }
    }
}

