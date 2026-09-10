using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRAnimationTemplate
{
    /// <summary>Bounded rain simulation with collider impacts and surface-attached ripple rings.</summary>
    public sealed class RainEffect : MonoBehaviour
    {
        [SerializeField] private Material rainMaterial;
        [SerializeField] private Vector2 area = new Vector2(10f, 10f);
        [SerializeField, Min(1f)] private float height = 6f;
        [SerializeField, Range(1f, 300f)] private float dropsPerSecond = 150f;
        [SerializeField] private Vector3 velocity = new Vector3(0.35f, -7f, 0.1f);
        [SerializeField] private LayerMask surfaceLayers = ~0;
        [SerializeField, Min(0.1f)] private float rippleLifetime = 0.65f;
        [SerializeField, Min(0.01f)] private float rippleRadius = 0.14f;

        private const int Capacity = 384;
        private const int RingSegments = 12;
        private struct Drop { public Vector3 position; public float age; public bool active; }
        private struct Ripple
        {
            public Transform surface;
            public Vector3 point, tangent, bitangent;
            public float age;
            public bool active;
        }
        private readonly Drop[] drops = new Drop[Capacity];
        private readonly Ripple[] ripples = new Ripple[Capacity];
        private readonly List<Vector3> vertices = new List<Vector3>(Capacity * 52);
        private readonly List<Color> colors = new List<Color>(Capacity * 52);
        private readonly List<int> triangles = new List<int>(Capacity * 78);
        private Mesh mesh;
        private GameObject visual;
        private Camera view;
        private float emission;
        private int nextDrop, nextRipple;

        private void Awake()
        {
            if (rainMaterial == null) { enabled = false; Debug.LogError("Rain needs its material assigned.", this); return; }
            // Vertices are world-space, so keep this rendering object at identity.
            visual = new GameObject("Rain and surface ripples");
            mesh = new Mesh { name = "Rain mesh" };
            mesh.MarkDynamic();
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = rainMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            view = Camera.main;
        }

        private void Update()
        {
            if (view == null) view = Camera.main;
            float dt = Time.deltaTime;
            emission += dropsPerSecond * dt;
            int count = Mathf.Min(Mathf.FloorToInt(emission), Capacity);
            emission -= Mathf.Floor(emission);
            for (int i = 0; i < count; i++)
            {
                drops[nextDrop] = new Drop { active = true, position = transform.position +
                    new Vector3(Random.Range(-area.x / 2f, area.x / 2f), height, Random.Range(-area.y / 2f, area.y / 2f)) };
                nextDrop = (nextDrop + 1) % Capacity;
            }

            vertices.Clear(); colors.Clear(); triangles.Clear();
            for (int i = 0; i < Capacity; i++)
            {
                if (!drops[i].active) continue;
                Drop drop = drops[i];
                Vector3 step = velocity * dt;
                if (step.sqrMagnitude > 0f && Physics.Raycast(drop.position, step.normalized, out RaycastHit hit,
                    step.magnitude, surfaceLayers, QueryTriggerInteraction.Ignore))
                {
                    Vector3 tangent = Vector3.Cross(hit.normal, Vector3.right);
                    if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(hit.normal, Vector3.forward);
                    tangent.Normalize();
                    Transform surface = hit.collider.transform;
                    ripples[nextRipple] = new Ripple { active = true, surface = surface,
                        point = surface.InverseTransformPoint(hit.point + hit.normal * 0.006f),
                        tangent = surface.InverseTransformVector(tangent),
                        bitangent = surface.InverseTransformVector(Vector3.Cross(hit.normal, tangent)) };
                    nextRipple = (nextRipple + 1) % Capacity;
                    drop.active = false;
                }
                else
                {
                    drop.position += step;
                    drop.age += dt;
                    if (drop.age > 4f || drop.position.y < transform.position.y - 3f) drop.active = false;
                    else
                    {
                        Vector3 tail = -velocity.normalized * 0.14f;
                        Vector3 facing = view != null ? view.transform.position - drop.position : Vector3.forward;
                        Vector3 side = Vector3.Cross(tail, facing).normalized * 0.004f;
                        Quad(drop.position - side, drop.position + side, drop.position + tail + side,
                            drop.position + tail - side, new Color(0.55f, 0.78f, 1f, 0.5f));
                    }
                }
                drops[i] = drop;
            }
            for (int i = 0; i < Capacity; i++)
            {
                Ripple ripple = ripples[i];
                if (!ripple.active) continue;
                ripple.age += dt;
                if (ripple.surface == null || ripple.age >= rippleLifetime) ripple.active = false;
                else
                {
                    float t = ripple.age / rippleLifetime;
                    float radius = Mathf.Lerp(0.012f, rippleRadius, t);
                    Vector3 center = ripple.surface.TransformPoint(ripple.point);
                    Vector3 x = ripple.surface.TransformVector(ripple.tangent);
                    Vector3 y = ripple.surface.TransformVector(ripple.bitangent);
                    Color color = new Color(0.55f, 0.85f, 1f, (1f - t) * 0.65f);
                    for (int segment = 0; segment < RingSegments; segment++)
                    {
                        float a = segment * Mathf.PI * 2f / RingSegments;
                        float b = (segment + 1) * Mathf.PI * 2f / RingSegments;
                        Vector3 u = x * Mathf.Cos(a) + y * Mathf.Sin(a);
                        Vector3 v = x * Mathf.Cos(b) + y * Mathf.Sin(b);
                        Quad(center + u * radius, center + v * radius,
                            center + v * (radius + 0.006f), center + u * (radius + 0.006f), color);
                    }
                }
                ripples[i] = ripple;
            }
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            int first = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int i = 0; i < 4; i++) colors.Add(color);
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
        }

        private void OnDisable() { if (visual != null) visual.SetActive(false); }
        private void OnEnable() { if (visual != null) visual.SetActive(true); }
        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
            if (mesh != null) Destroy(mesh);
        }
    }
}
