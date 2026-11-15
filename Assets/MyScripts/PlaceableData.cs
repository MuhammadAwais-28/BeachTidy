using System.Collections.Generic;
using UnityEngine;

public class PlaceableData : MonoBehaviour
{
    // ---------- registry used by the save system ----------
    private static readonly Dictionary<string, PlaceableData> Registry =
        new Dictionary<string, PlaceableData>();

    public static bool TryGet(string id, out PlaceableData data)
    {
        return Registry.TryGetValue(id, out data);
    }

    // Stable id built from the object's hierarchy path at scene start
    public string Id { get; private set; }

    [Header("Instruction")]
    public PlaceableType type;

    [Header("Placed tilt (LOCAL euler, same numbers as the Transform inspector)")]
    public Vector3 placedLocalEuler = Vector3.zero;
    public bool useCustomPlacedRotation = false;

    [Header("Planting")]
    [Tooltip("Sinks the object into the ground. Negative = lifts it")]
    public float sinkDepth = 0.05f;
    [Tooltip("Tick for benches: lowest point of the model sits exactly on the ground")]
    public bool alignBottomToGround = false;
    [Tooltip("Extra turn around Y. Used both while held AND when placed")]
    public float yawOffset = 0f;

    [Header("While held")]
    [Tooltip("Shrinks the object while held so its biggest dimension is at most this. 0 = no shrinking")]
    public float heldMaxSize = 1.5f;
    [Tooltip("Extra offset while held: x right, y up, z forward")]
    public Vector3 heldOffset = Vector3.zero;
    [Tooltip("Extra rotation while held (0,0,0 = upright)")]
    public Vector3 heldEuler = Vector3.zero;

    private Transform originalParent;
    private Vector3 originalLocalScale;
    private Vector3 originalWorldScale;
    private Quaternion originalLocalRotation;

    private void Awake()
    {
        originalParent = transform.parent;
        originalLocalScale = transform.localScale;
        originalWorldScale = transform.lossyScale;
        originalLocalRotation = transform.localRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;   // stays planted until picked up
        }

        Id = BuildId(transform);
        if (Registry.ContainsKey(Id))
            Debug.LogWarning("PlaceableData: duplicate id '" + Id + "'. Rename one of the objects.");
        Registry[Id] = this;
    }

    private void OnDestroy()
    {
        if (Id != null && Registry.TryGetValue(Id, out PlaceableData current) && current == this)
            Registry.Remove(Id);
    }

    private static string BuildId(Transform t)
    {
        string path = t.name + "[" + t.GetSiblingIndex() + "]";
        for (Transform p = t.parent; p != null; p = p.parent)
            path = p.name + "[" + p.GetSiblingIndex() + "]/" + path;

        return t.gameObject.scene.name + ":" + path;
    }

    // Puts the object back exactly where the player placed it earlier in this session.
    // Called by InstructionManager right after the scene loads (object is still under its original parent).
    public void RestorePlaced(Vector3 worldPosition, Quaternion worldRotation)
    {
        transform.SetPositionAndRotation(worldPosition, worldRotation);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    // The upright pose, facing the given yaw. Used for holding AND placing.
    public Quaternion GetUprightRotation(float yaw)
    {
        Quaternion localRot = useCustomPlacedRotation
            ? Quaternion.Euler(placedLocalEuler)
            : originalLocalRotation;

        Quaternion parentRot = originalParent != null
            ? originalParent.rotation
            : Quaternion.identity;

        return Quaternion.AngleAxis(yaw + yawOffset, Vector3.up) * parentRot * localRot;
    }

    public void BeginHold(float yaw)
    {
        transform.SetParent(null);
        transform.localScale = originalWorldScale;
        transform.rotation = GetUprightRotation(yaw);

        float factor = 1f;
        if (heldMaxSize > 0f && GetBounds(gameObject, out Bounds b))
        {
            float biggest = Mathf.Max(b.size.y, new Vector2(b.size.x, b.size.z).magnitude);
            if (biggest > heldMaxSize)
                factor = heldMaxSize / biggest;
        }
        transform.localScale = originalWorldScale * factor;
    }

    // Called when dropped/thrown (object has no parent here)
    public void EndHold()
    {
        transform.localScale = originalWorldScale;
    }

    public void PlaceAt(Vector3 worldPoint, float playerYaw)
    {
        transform.SetParent(originalParent, false);   // restores the parent's scale logic
        transform.localScale = originalLocalScale;

        transform.rotation = GetUprightRotation(playerYaw);
        transform.position = worldPoint;

        if (alignBottomToGround && GetBounds(gameObject, out Bounds b))
        {
            float lift = worldPoint.y - b.min.y;
            transform.position += Vector3.up * (lift - sinkDepth);
        }
        else
        {
            transform.position = worldPoint + Vector3.down * sinkDepth;
        }
    }

    public static bool GetBounds(GameObject go, out Bounds bounds)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(go.transform.position, Vector3.zero);
        if (rends.Length == 0) return false;

        bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Capture Placed Pose")]
    private void CapturePlacedPose()
    {
        placedLocalEuler = transform.localEulerAngles;
        useCustomPlacedRotation = true;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}