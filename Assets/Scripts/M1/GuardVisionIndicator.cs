using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shows the guard's current vision field on the ground while a mission is active.</summary>
public sealed class GuardVisionIndicator : MonoBehaviour
{
    [SerializeField, Range(12, 96)] private int arcSegments = 32;
    [SerializeField, Tooltip("Layers considered as physical blockers for the drawn vision field.")]
    private LayerMask visionOccluders = 1;
    [SerializeField, Min(0f)] private float groundOffset = .035f;
    [SerializeField, Range(0f, 1f)] private float fillOpacity = .16f;
    [SerializeField, Range(0f, 1f)] private float edgeOpacity = .85f;
    [SerializeField, Min(.005f)] private float edgeWidth = .035f;
    [SerializeField, Min(.05f)] private float poseRefreshInterval = .12f;
    [SerializeField, Min(.05f)] private float occlusionRefreshInterval = .25f;
    [SerializeField, Min(.01f)] private float positionUpdateThreshold = .12f;
    [SerializeField, Min(.1f)] private float rotationUpdateThreshold = 1.5f;
    [SerializeField, Min(.01f)] private float blendDuration = .12f;

    private static Material sharedMaterial;
    private static readonly Plane[] sharedCameraFrustumPlanes = new Plane[6];
    private static int sharedFrustumFrame = -1;
    private static int sharedFrustumCameraId;
    private GuardPerception perception;
    private GuardBrain brain;
    private MeshFilter fillFilter;
    private MeshRenderer fillRenderer;
    private MeshFilter edgeFilter;
    private MeshRenderer edgeRenderer;
    private Mesh fillMesh;
    private Mesh edgeMesh;
    private MaterialPropertyBlock properties;
    private Camera viewCamera;
    private readonly Collider[] occlusionCandidates = new Collider[256];
    private readonly RaycastHit[] occlusionHits = new RaycastHit[64];
    private readonly List<float> sampleAngles = new List<float>(128);
    private readonly List<Vector3> arc = new List<Vector3>(128);
    private readonly List<Vector3> fillVertices = new List<Vector3>(130);
    private readonly List<int> fillTriangles = new List<int>(384);
    private readonly List<Vector3> edgeVertices = new List<Vector3>(520);
    private readonly List<int> edgeTriangles = new List<int>(780);
    private readonly List<Vector3> fillStartVertices = new List<Vector3>(130);
    private readonly List<Vector3> edgeStartVertices = new List<Vector3>(520);
    private readonly List<Vector3> fillTargetVertices = new List<Vector3>(130);
    private readonly List<Vector3> edgeTargetVertices = new List<Vector3>(520);
    private readonly List<Vector3> fillDisplayVertices = new List<Vector3>(130);
    private readonly List<Vector3> edgeDisplayVertices = new List<Vector3>(520);
    private float builtRange = -1f;
    private float builtAngle = -1f;
    private float nextOcclusionRefresh;
    private float nextPoseRefresh;
    private float blendStartedAt;
    private Vector3 lastBuildPosition;
    private Quaternion lastBuildRotation;
    private int occlusionCandidateCount;
    private int occlusionFingerprint;
    private int builtOcclusionFingerprint;
    private bool blending;
    private bool built;
    private bool hasAppliedStateColor;
    private Color lastAppliedStateColor;

    private void Awake()
    {
        perception = GetComponent<GuardPerception>();
        brain = GetComponent<GuardBrain>();
        if (perception == null) return;

        CreateIndicatorObjects();
        EnsureMaterial();
        UpdateCameraReference();
        if (IsFieldVisibleToCamera()) RebuildCone();
    }

    private void LateUpdate()
    {
        if (perception == null) perception = GetComponent<GuardPerception>();
        if (brain == null) brain = GetComponent<GuardBrain>();
        if (perception == null) return;

        MissionManager mission = MissionManager.Instance;
        bool visible = mission == null || mission.IsPlaying;
        if (!visible)
        {
            SetRenderersEnabled(false);
            built = false;
            return;
        }

        if (fillFilter == null || fillRenderer == null || edgeFilter == null || edgeRenderer == null)
            CreateIndicatorObjects();
        EnsureMaterial();
        UpdateCameraReference();
        if (!IsFieldVisibleToCamera())
        {
            SetRenderersEnabled(false);
            built = false;
            blending = false;
            return;
        }

        SetRenderersEnabled(true);
        bool settingsChanged = !Mathf.Approximately(builtRange, perception.VisionRange) ||
            !Mathf.Approximately(builtAngle, perception.VisionAngle);
        bool moved = (transform.position - lastBuildPosition).sqrMagnitude >=
            positionUpdateThreshold * positionUpdateThreshold ||
            Quaternion.Angle(lastBuildRotation, transform.rotation) >= rotationUpdateThreshold;
        bool occludersChanged = false;
        bool candidatesCollected = false;
        if (Time.time >= nextOcclusionRefresh)
        {
            int refreshedFingerprint = CollectOcclusionCandidates(GetGroundOrigin());
            candidatesCollected = true;
            occludersChanged = refreshedFingerprint != builtOcclusionFingerprint;
            nextOcclusionRefresh = Time.time + Mathf.Max(.05f, occlusionRefreshInterval);
        }

        bool poseRefreshDue = moved && Time.time >= nextPoseRefresh;
        if (!built || settingsChanged || poseRefreshDue || occludersChanged)
            RebuildCone(candidatesCollected);

        UpdateTint();
        UpdateBlendedMeshes();
    }

    private void UpdateCameraReference()
    {
        if (viewCamera == null) viewCamera = Camera.main;
    }

    private bool IsFieldVisibleToCamera()
    {
        if (viewCamera == null) return true;

        int cameraId = viewCamera.GetInstanceID();
        if (sharedFrustumFrame != Time.frameCount || sharedFrustumCameraId != cameraId)
        {
            GeometryUtility.CalculateFrustumPlanes(viewCamera, sharedCameraFrustumPlanes);
            sharedFrustumFrame = Time.frameCount;
            sharedFrustumCameraId = cameraId;
        }
        Vector3 center = (fillFilter != null ? fillFilter.transform.position : transform.position) +
            Vector3.up * groundOffset;
        float range = Mathf.Max(0f, perception.VisionRange + edgeWidth);
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
        forward.Normalize();
        float halfAngle = perception.VisionAngle * .5f;
        float halfAngleCos = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        Bounds fieldBounds = new Bounds(center, Vector3.zero);
        AddFieldBoundsPoint(ref fieldBounds, center, forward, range, -halfAngle);
        AddFieldBoundsPoint(ref fieldBounds, center, forward, range, halfAngle);
        if (Vector3.Dot(forward, Vector3.forward) >= halfAngleCos)
            fieldBounds.Encapsulate(center + Vector3.forward * range);
        if (Vector3.Dot(forward, Vector3.back) >= halfAngleCos)
            fieldBounds.Encapsulate(center + Vector3.back * range);
        if (Vector3.Dot(forward, Vector3.right) >= halfAngleCos)
            fieldBounds.Encapsulate(center + Vector3.right * range);
        if (Vector3.Dot(forward, Vector3.left) >= halfAngleCos)
            fieldBounds.Encapsulate(center + Vector3.left * range);
        fieldBounds.Expand(new Vector3(edgeWidth * 2f, .1f, edgeWidth * 2f));
        return GeometryUtility.TestPlanesAABB(sharedCameraFrustumPlanes, fieldBounds);
    }

    private void AddFieldBoundsPoint(ref Bounds bounds, Vector3 center,
        Vector3 forward, float range, float angleDegrees)
    {
        Vector3 direction = Quaternion.AngleAxis(angleDegrees, Vector3.up) * forward;
        bounds.Encapsulate(center + direction * range);
    }

    private void SetRenderersEnabled(bool enabled)
    {
        if (fillRenderer != null) fillRenderer.enabled = enabled;
        if (edgeRenderer != null) edgeRenderer.enabled = enabled;
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
        if (fillRenderer != null && fillRenderer.sharedMaterial != sharedMaterial)
            fillRenderer.sharedMaterial = sharedMaterial;
        if (edgeRenderer != null && edgeRenderer.sharedMaterial != sharedMaterial)
            edgeRenderer.sharedMaterial = sharedMaterial;
    }

    private void RebuildCone(bool candidatesAlreadyCollected = false)
    {
        if (perception == null || fillFilter == null || edgeFilter == null) return;
        int segments = Mathf.Max(12, arcSegments);
        float halfAngle = perception.VisionAngle * .5f;
        Vector3 groundOrigin = GetGroundOrigin();
        if (!candidatesAlreadyCollected) CollectOcclusionCandidates(groundOrigin);

        sampleAngles.Clear();
        for (int i = 0; i <= segments; i++)
            sampleAngles.Add(Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments));
        AppendOcclusionCornerAngles(groundOrigin, halfAngle);
        sampleAngles.Sort();

        arc.Clear();
        float previousAngle = float.NegativeInfinity;
        for (int i = 0; i < sampleAngles.Count; i++)
        {
            float angleDegrees = sampleAngles[i];
            if (Mathf.Abs(angleDegrees - previousAngle) < .0001f) continue;
            previousAngle = angleDegrees;
            float angle = angleDegrees * Mathf.Deg2Rad;
            Vector3 localDirection = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 worldDirection = transform.TransformDirection(localDirection).normalized;
            float visibleDistance = GetGroundBoundaryDistance(worldDirection, groundOrigin);
            arc.Add(new Vector3(Mathf.Sin(angle) * visibleDistance, groundOffset,
                Mathf.Cos(angle) * visibleDistance));
        }

        fillVertices.Clear();
        fillTriangles.Clear();
        fillVertices.Add(new Vector3(0f, groundOffset, 0f));
        for (int i = 0; i < arc.Count; i++) fillVertices.Add(arc[i]);
        for (int i = 0; i < arc.Count - 1; i++)
        {
            fillTriangles.Add(0);
            fillTriangles.Add(i + 1);
            fillTriangles.Add(i + 2);
        }

        edgeVertices.Clear();
        edgeTriangles.Clear();
        Vector3 origin = new Vector3(0f, groundOffset + .012f, 0f);
        for (int i = 0; i < arc.Count - 1; i++) AddRibbon(edgeVertices, edgeTriangles,
            arc[i] + Vector3.up * .012f, arc[i + 1] + Vector3.up * .012f);
        AddRibbon(edgeVertices, edgeTriangles, origin, arc[0] + Vector3.up * .012f);
        AddRibbon(edgeVertices, edgeTriangles, origin, arc[arc.Count - 1] + Vector3.up * .012f);
        UpdateMeshTargets();

        UpdateTint();
        builtRange = perception.VisionRange;
        builtAngle = perception.VisionAngle;
        lastBuildPosition = transform.position;
        lastBuildRotation = transform.rotation;
        builtOcclusionFingerprint = occlusionFingerprint;
        nextOcclusionRefresh = Time.time + Mathf.Max(.02f, occlusionRefreshInterval);
        nextPoseRefresh = Time.time + Mathf.Max(.05f, poseRefreshInterval);
        built = true;
    }

    private Vector3 GetGroundOrigin()
    {
        return (fillFilter != null ? fillFilter.transform.position : transform.position) +
            Vector3.up * groundOffset;
    }

    private int CollectOcclusionCandidates(Vector3 origin)
    {
        occlusionCandidateCount = Physics.OverlapSphereNonAlloc(origin, perception.VisionRange,
            occlusionCandidates, visionOccluders, QueryTriggerInteraction.Ignore);
        int sum = 0;
        int xor = 0;
        int included = 0;
        for (int i = 0; i < occlusionCandidateCount; i++)
        {
            Collider candidate = occlusionCandidates[i];
            if (ShouldIgnoreOccluder(candidate)) continue;
            int item = unchecked(candidate.GetInstanceID() * 397 ^ candidate.bounds.GetHashCode());
            sum = unchecked(sum + item);
            xor ^= item;
            included++;
        }
        occlusionFingerprint = unchecked(sum * 397 ^ xor ^ included);
        return occlusionFingerprint;
    }

    private bool ShouldIgnoreOccluder(Collider candidate)
    {
        if (candidate == null) return true;
        Transform candidateTransform = candidate.transform;
        if (candidateTransform == transform || candidateTransform.IsChildOf(transform)) return true;
        if (candidate.GetComponentInParent<GuardBrain>() != null ||
            candidate.GetComponentInParent<PlayerStealthState>() != null) return true;

        for (Transform current = candidateTransform; current != null; current = current.parent)
            if (current.CompareTag("Player")) return true;
        return false;
    }

    private void AppendOcclusionCornerAngles(Vector3 origin, float halfAngle)
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        const float edgeOffset = .05f;

        for (int i = 0; i < occlusionCandidateCount; i++)
        {
            Collider candidate = occlusionCandidates[i];
            if (ShouldIgnoreOccluder(candidate)) continue;

            Bounds bounds = candidate.bounds;
            AddCornerAngle(origin, forward, bounds.min.x, bounds.min.z, halfAngle, edgeOffset);
            AddCornerAngle(origin, forward, bounds.min.x, bounds.max.z, halfAngle, edgeOffset);
            AddCornerAngle(origin, forward, bounds.max.x, bounds.min.z, halfAngle, edgeOffset);
            AddCornerAngle(origin, forward, bounds.max.x, bounds.max.z, halfAngle, edgeOffset);
        }
    }

    private void AddCornerAngle(Vector3 origin, Vector3 forward,
        float cornerX, float cornerZ, float halfAngle, float edgeOffset)
    {
        Vector3 toCorner = new Vector3(cornerX - origin.x, 0f, cornerZ - origin.z);
        if (toCorner.sqrMagnitude < .0001f ||
            toCorner.sqrMagnitude > perception.VisionRange * perception.VisionRange) return;
        float angle = Vector3.SignedAngle(forward, toCorner.normalized, Vector3.up);
        if (Mathf.Abs(angle) > halfAngle + edgeOffset) return;
        sampleAngles.Add(Mathf.Clamp(angle - edgeOffset, -halfAngle, halfAngle));
        sampleAngles.Add(Mathf.Clamp(angle, -halfAngle, halfAngle));
        sampleAngles.Add(Mathf.Clamp(angle + edgeOffset, -halfAngle, halfAngle));
    }

    private float GetGroundBoundaryDistance(Vector3 direction, Vector3 origin)
    {
        int hitCount = Physics.RaycastNonAlloc(origin, direction, occlusionHits,
            perception.VisionRange, visionOccluders, QueryTriggerInteraction.Ignore);
        float nearestDistance = perception.VisionRange;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = occlusionHits[i].collider;
            if (ShouldIgnoreOccluder(hitCollider)) continue;
            if (occlusionHits[i].distance < nearestDistance)
                nearestDistance = occlusionHits[i].distance;
        }

        return nearestDistance < perception.VisionRange
            ? Mathf.Max(0f, nearestDistance - .025f)
            : perception.VisionRange;
    }

    private void UpdateMeshTargets()
    {
        if (fillMesh == null)
        {
            fillMesh = new Mesh { name = "Guard Vision Fill (Reusable)" };
            fillMesh.MarkDynamic();
            fillFilter.sharedMesh = fillMesh;
        }
        if (edgeMesh == null)
        {
            edgeMesh = new Mesh { name = "Guard Vision Edge (Reusable)" };
            edgeMesh.MarkDynamic();
            edgeFilter.sharedMesh = edgeMesh;
        }

        fillStartVertices.Clear();
        edgeStartVertices.Clear();
        if (fillMesh.vertexCount > 0) fillMesh.GetVertices(fillStartVertices);
        if (edgeMesh.vertexCount > 0) edgeMesh.GetVertices(edgeStartVertices);

        bool canBlend = fillStartVertices.Count == fillVertices.Count &&
            edgeStartVertices.Count == edgeVertices.Count && fillStartVertices.Count > 0;
        fillTargetVertices.Clear();
        edgeTargetVertices.Clear();
        fillTargetVertices.AddRange(fillVertices);
        edgeTargetVertices.AddRange(edgeVertices);

        fillMesh.Clear();
        edgeMesh.Clear();
        if (canBlend)
        {
            fillMesh.SetVertices(fillStartVertices);
            edgeMesh.SetVertices(edgeStartVertices);
        }
        else
        {
            fillMesh.SetVertices(fillTargetVertices);
            edgeMesh.SetVertices(edgeTargetVertices);
        }
        fillMesh.SetTriangles(fillTriangles, 0);
        edgeMesh.SetTriangles(edgeTriangles, 0);

        float extent = perception.VisionRange + edgeWidth;
        fillMesh.bounds = new Bounds(new Vector3(0f, groundOffset, 0f),
            new Vector3(extent * 2f, .1f, extent * 2f));
        edgeMesh.bounds = new Bounds(new Vector3(0f, groundOffset + .012f, 0f),
            new Vector3(extent * 2f, .1f, extent * 2f));

        blending = canBlend && blendDuration > .001f;
        blendStartedAt = Time.time;
        if (!blending)
        {
            fillMesh.SetVertices(fillTargetVertices);
            edgeMesh.SetVertices(edgeTargetVertices);
        }
    }

    private void UpdateBlendedMeshes()
    {
        if (!blending) return;
        float t = Mathf.Clamp01((Time.time - blendStartedAt) / Mathf.Max(.001f, blendDuration));
        fillDisplayVertices.Clear();
        edgeDisplayVertices.Clear();
        for (int i = 0; i < fillTargetVertices.Count; i++)
            fillDisplayVertices.Add(Vector3.Lerp(fillStartVertices[i], fillTargetVertices[i], t));
        for (int i = 0; i < edgeTargetVertices.Count; i++)
            edgeDisplayVertices.Add(Vector3.Lerp(edgeStartVertices[i], edgeTargetVertices[i], t));
        fillMesh.SetVertices(fillDisplayVertices);
        edgeMesh.SetVertices(edgeDisplayVertices);
        if (t >= 1f) blending = false;
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

        if (hasAppliedStateColor && stateColor == lastAppliedStateColor) return;
        hasAppliedStateColor = true;
        lastAppliedStateColor = stateColor;

        SetTint(fillRenderer, new Color(stateColor.r, stateColor.g, stateColor.b, fillOpacity));
        SetTint(edgeRenderer, new Color(stateColor.r, stateColor.g, stateColor.b, edgeOpacity));
    }

    private void OnDestroy()
    {
        if (fillMesh != null) Destroy(fillMesh);
        if (edgeMesh != null) Destroy(edgeMesh);
    }
}
