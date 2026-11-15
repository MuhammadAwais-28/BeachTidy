//using System.Collections.Generic;
//using UnityEngine;

//// Add to the same Camera object that has PickupSystem.
//// Draws a green stroke around whatever the crosshair is aiming at (and is in pickup range).
//public class PickupHighlighter : MonoBehaviour
//{
//    public PickupSystem pickupSystem;

//    [Tooltip("A material that uses the BeachTidy/OutlineHull shader")]
//    public Material outlineMaterial;

//    public Color outlineColor = new Color(0.1f, 1f, 0.2f, 1f);
//    [Range(1f, 12f)] public float widthPixels = 4f;

//    private Material outlineInstance;
//    private GameObject target;
//    private MeshFilter[] filters = new MeshFilter[0];

//    private static readonly HashSet<Mesh> preparedMeshes = new HashSet<Mesh>();

//    private void Awake()
//    {
//        if (pickupSystem == null)
//            pickupSystem = GetComponent<PickupSystem>();

//        if (outlineMaterial != null)
//            outlineInstance = new Material(outlineMaterial);
//        else
//            Debug.LogError("PickupHighlighter: assign an Outline Material (shader BeachTidy/OutlineHull).");
//    }

//    private void OnDestroy()
//    {
//        if (outlineInstance != null)
//            Destroy(outlineInstance);
//    }

//    private void LateUpdate()
//    {
//        if (pickupSystem == null || outlineInstance == null)
//            return;

//        GameObject aimed = pickupSystem.GetAimedObject(out _);

//        if (aimed != target)
//            SetTarget(aimed);

//        if (target == null)
//            return;

//        outlineInstance.SetColor("_OutlineColor", outlineColor);
//        outlineInstance.SetFloat("_Width", widthPixels);

//        for (int i = 0; i < filters.Length; i++)
//        {
//            MeshFilter mf = filters[i];
//            if (mf == null || mf.sharedMesh == null)
//                continue;

//            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
//            if (mr == null || !mr.enabled)
//                continue;

//            Mesh mesh = mf.sharedMesh;
//            Matrix4x4 matrix = mf.transform.localToWorldMatrix;

//            for (int s = 0; s < mesh.subMeshCount; s++)
//            {
//                Graphics.DrawMesh(mesh, matrix, outlineInstance, 0, null, s, null, false, false);
//            }
//        }
//    }

//    private void SetTarget(GameObject newTarget)
//    {
//        target = newTarget;

//        if (target == null)
//        {
//            filters = new MeshFilter[0];
//            return;
//        }

//        filters = target.GetComponentsInChildren<MeshFilter>();
//        for (int i = 0; i < filters.Length; i++)
//            EnsureSmoothNormals(filters[i].sharedMesh);
//    }

//    // Averages normals of vertices that share a position and stores the result in UV3,
//    // so the outline stays connected on hard-edged meshes (benches, crates).
//    private static void EnsureSmoothNormals(Mesh mesh)
//    {
//        if (mesh == null || preparedMeshes.Contains(mesh))
//            return;

//        preparedMeshes.Add(mesh);

//        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3))
//            return;

//        if (!mesh.isReadable)
//        {
//            Debug.LogWarning("Outline: mesh '" + mesh.name + "' is not readable, so the outline may have gaps at hard edges. " +
//                             "Select the model asset > Model tab > tick Read/Write.");
//            return;
//        }

//        Vector3[] vertices = mesh.vertices;
//        Vector3[] normals = mesh.normals;
//        if (normals == null || normals.Length != vertices.Length)
//            return;

//        var sums = new Dictionary<Vector3Int, Vector3>();
//        for (int i = 0; i < vertices.Length; i++)
//        {
//            Vector3Int key = Quantize(vertices[i]);
//            sums.TryGetValue(key, out Vector3 sum);
//            sums[key] = sum + normals[i];
//        }

//        var smooth = new List<Vector3>(vertices.Length);
//        for (int i = 0; i < vertices.Length; i++)
//            smooth.Add(sums[Quantize(vertices[i])].normalized);

//        mesh.SetUVs(3, smooth);
//    }

//    private static Vector3Int Quantize(Vector3 p)
//    {
//        return new Vector3Int(
//            Mathf.RoundToInt(p.x * 10000f),
//            Mathf.RoundToInt(p.y * 10000f),
//            Mathf.RoundToInt(p.z * 10000f));
//    }
//}
