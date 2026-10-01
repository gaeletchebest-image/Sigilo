using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shows the guard's current vision field on the ground while a mission is active.</summary>
public sealed class GuardVisionIndicator : MonoBehaviour
{
    [SerializeField, Range(12, 96)] private int arcSegments = 48;
    [SerializeField, Min(0f)] private float groundOffset = .035f;
    [SerializeField, Range(0f, 1f)] private float fillOpacity = .16f;
    [SerializeField, Range(0f, 1f)] private float edgeOpacity = .85f;
    [SerializeField, Min(.005f)] private float edgeWidth = .035f;

    private static Material sharedMaterial;
    private GuardPerception perception;
    private GuardBrain brain;
    private MeshFilter fillFilter;
    private MeshRenderer fillRenderer;
    private MeshFilter edgeFilter;
    private MeshRenderer edgeRenderer;
    private Mesh fillMesh;
    private Mesh edgeMesh;
    private MaterialPropertyBlock properties;
    private float builtRange = -1f;
    private float builtAngle = -1f;
    private bool built;

    private void Awake()
    {
        perception = GetComponent<GuardPerception>();
        brain = GetComponent<GuardBrain>();
        if (perception == null) return;

        CreateIndicatorObjects();
        EnsureMaterial();
        RebuildCone();
    }

    private void LateUpdate()
    {
        if (perception == null) perception = GetComponent<GuardPerception>();
        if (brain == null) brain = GetComponent<GuardBrain>();
        if (perception == null) return;
        if (fillFilter == null || fillRenderer == null || edgeFilter == null || edgeRenderer == null)
            CreateIndicatorObjects();
        EnsureMaterial();

        if (!built || !Mathf.Approximately(builtRange, perception.VisionRange) ||
            !Mathf.Approximately(builtAngle, perception.VisionAngle))
            RebuildCone();

        MissionManager mission = MissionManager.Instance;
        bool visible = mission == null || mission.IsPlaying;
        fillRenderer.enabled = visible;
        edgeRenderer.enabled = visible;
        UpdateTint();
    }

    private void CreateIndicatorObjects()
    {
        Transform root = transform.Find("VisionRange");
        if (root == null)
        {
            GameObject indicator = new GameObject("VisionRange");
            root = indicator.transform;
            root.SetParent(transform, false);

            Vector3 parentScale = transform.lossyScale;
            root.localScale = new Vector3(
                1f / Mathf.Max(.001f, Mathf.Abs(parentScale.x)), 1f,
                1f / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            float baseOffset = agent != null ? agent.baseOffset : 0f;
            root.localPosition = new Vector3(0f,
                -baseOffset / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 0f);
        }

        Transform fill = root.Find("Fill");
        if (fill == null) fill = CreateChild(root, "Fill");
        Transform edges = root.Find("Edges");
        if (edges == null) edges = CreateChild(root, "Edges");
        fillFilter = GetOrAdd<MeshFilter>(fill.gameObject);
        fillRenderer = GetOrAdd<MeshRenderer>(fill.gameObject);
        edgeFilter = GetOrAdd<MeshFilter>(edges.gameObject);
        edgeRenderer = GetOrAdd<MeshRenderer>(edges.gameObject);
        ConfigureRenderer(fillRenderer);
        ConfigureRenderer(edgeRenderer);
        properties = properties ?? new MaterialPropertyBlock();
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void ConfigureRenderer(MeshRenderer target)
    {
        target.shadowCastingMode = ShadowCastingMode.Off;
        target.receiveShadows = false;
        target.lightProbeUsage = LightProbeUsage.Off;
        target.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void EnsureMaterial()
    {
        if (sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Renderer sourceRenderer = GetComponent<Renderer>();
                if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
                    shader = sourceRenderer.sharedMaterial.shader;
            }
            if (shader == null) return;

            sharedMaterial = new Material(shader) { name = "Guard Vision Indicator (Runtime)" };
            if (shader.name.StartsWith("Universal Render Pipeline/"))
            {
                sharedMaterial.SetFloat("_Surface", 1f);
                sharedMaterial.SetFloat("_Blend", 0f);
                sharedMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                sharedMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                sharedMaterial.SetFloat("_ZWrite", 0f);
                sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                sharedMaterial.renderQueue = (int)RenderQueue.Transparent;
            }
        }
        if (fillRenderer != null) fillRenderer.sharedMaterial = sharedMaterial;
        if (edgeRenderer != null) edgeRenderer.sharedMaterial = sharedMaterial;
    }

    private void RebuildCone()
    {
        if (perception == null || fillFilter == null || edgeFilter == null) return;
        int segments = Mathf.Max(12, arcSegments);
        float range = Mathf.Max(.1f, perception.VisionRange);
        float halfAngle = perception.VisionAngle * .5f;
        Vector3[] arc = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments) * Mathf.Deg2Rad;
            arc[i] = new Vector3(Mathf.Sin(angle) * range, groundOffset, Mathf.Cos(angle) * range);
        }

        Vector3[] fillVertices = new Vector3[segments + 2];
        int[] fillTriangles = new int[segments * 3];
        fillVertices[0] = new Vector3(0f, groundOffset, 0f);
        for (int i = 0; i <= segments; i++) fillVertices[i + 1] = arc[i];
        for (int i = 0; i < segments; i++)
        {
            int tri = i * 3;
            fillTriangles[tri] = 0;
            fillTriangles[tri + 1] = i + 1;
            fillTriangles[tri + 2] = i + 2;
        }
        fillMesh = ReplaceMesh(fillMesh, fillVertices, fillTriangles);
        fillFilter.sharedMesh = fillMesh;

        List<Vector3> edgeVertices = new List<Vector3>();
        List<int> edgeTriangles = new List<int>();
        Vector3 origin = new Vector3(0f, groundOffset + .012f, 0f);
        for (int i = 0; i < segments; i++) AddRibbon(edgeVertices, edgeTriangles,
            arc[i] + Vector3.up * .012f, arc[i + 1] + Vector3.up * .012f);
        AddRibbon(edgeVertices, edgeTriangles, origin, arc[0] + Vector3.up * .012f);
        AddRibbon(edgeVertices, edgeTriangles, origin, arc[segments] + Vector3.up * .012f);
        edgeMesh = ReplaceMesh(edgeMesh, edgeVertices.ToArray(), edgeTriangles.ToArray());
        edgeFilter.sharedMesh = edgeMesh;

        UpdateTint();
        builtRange = perception.VisionRange;
        builtAngle = perception.VisionAngle;
        built = true;
    }

    private void AddRibbon(List<Vector3> vertices, List<int> triangles, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * (edgeWidth * .5f);
        int first = vertices.Count;
        vertices.Add(start - side);
        vertices.Add(start + side);
        vertices.Add(end + side);
        vertices.Add(end - side);
        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 1);
        triangles.Add(first);
        triangles.Add(first + 3);
        triangles.Add(first + 2);
    }

    private static Mesh ReplaceMesh(Mesh previous, Vector3[] vertices, int[] triangles)
    {
        if (previous != null) Destroy(previous);
        Mesh mesh = new Mesh { name = "Guard Vision Indicator (Runtime)" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void SetTint(Renderer target, Color color)
    {
        target.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        target.SetPropertyBlock(properties);
    }

    private void UpdateTint()
    {
        float suspicion = brain != null ? brain.Suspicion : 0f;
        bool searching = brain != null &&
            (brain.CurrentState == GuardBrain.State.Investigate || brain.CurrentState == GuardBrain.State.Search);
        Color stateColor = brain != null &&
            (brain.CurrentState == GuardBrain.State.Chase || suspicion >= 100f)
            ? new Color(.9f, .12f, .1f)
            : suspicion > 0f || searching
                ? new Color(1f, .62f, .08f)
                : new Color(.35f, .85f, .45f);

        SetTint(fillRenderer, new Color(stateColor.r, stateColor.g, stateColor.b, fillOpacity));
        SetTint(edgeRenderer, new Color(stateColor.r, stateColor.g, stateColor.b, edgeOpacity));
    }

    private void OnDestroy()
    {
        if (fillMesh != null) Destroy(fillMesh);
        if (edgeMesh != null) Destroy(edgeMesh);
    }
}
