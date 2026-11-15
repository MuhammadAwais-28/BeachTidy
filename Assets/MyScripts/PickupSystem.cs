using System;
using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform holdPoint;              // no longer used for holding (kept so the Inspector reference isn't lost)
    public GameObject placeUmbrellaButton;
    public InstructionManager instructions;  // optional: restricts pickups to the current stage

    [Header("Distances & Forces")]
    public float pickupDistance = 3f;
    public float placeDistance = 2f;
    public float throwForce = 8f;

    [Header("Held object: x = right, y = up, z = forward (ignores looking up/down)")]
    public Vector3 holdOffset = new Vector3(0.6f, -0.4f, 2.5f);
    public float groundClearance = 0.05f;

    [Header("Hover highlight (glows the object itself, no extra shader)")]
    public bool showHighlight = true;
    public Color highlightColor = new Color(0.1f, 1f, 0.2f, 1f);
    [Range(0f, 1f)] public float tintStrength = 0.35f;     // how much the base colour shifts to green
    public float emissionIntensity = 1.5f;                 // glow brightness
    public bool pulse = true;
    public float pulseSpeed = 2f;

    private static readonly Vector3 ScreenCentre = new Vector3(0.5f, 0.5f, 0f);

    private GameObject heldObject;
    private PlaceableData heldData;
    private Quaternion heldRotationOffset;   // only for normal pickups (tires etc.)

    private GameObject hoveredObject;
    private readonly System.Collections.Generic.List<MatState> litMaterials =
        new System.Collections.Generic.List<MatState>();

    private class MatState
    {
        public Material mat;
        public bool hasEmission;
        public bool hadEmissionKeyword;
        public Color origEmission;
        public int colorId = -1;          // _BaseColor (URP/HDRP) or _Color (Built-in)
        public Color origColor;
    }

    // ---------- UNITY ----------
    private void OnDisable()
    {
        ClearHighlight();
        hoveredObject = null;
    }

    private void Start()
    {
        if (placeUmbrellaButton != null)
            placeUmbrellaButton.SetActive(false);
    }

    private void Update()
    {
        UpdateHover();
    }

    // Runs after the look controller has rotated the camera this frame
    private void LateUpdate()
    {
        if (heldObject == null)
            return;

        Transform cam = playerCamera.transform;
        float yaw = cam.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 offset = holdOffset;
        Quaternion rot;

        if (heldData != null)
        {
            offset += heldData.heldOffset;
            rot = heldData.GetUprightRotation(yaw) * Quaternion.Euler(heldData.heldEuler);
        }
        else
        {
            rot = yawRot * heldRotationOffset;
        }

        Vector3 target = cam.position + yawRot * offset;

        heldObject.transform.rotation = rot;
        heldObject.transform.position = target;

        // Centre the model's visual bounds on the target so the pivot doesn't matter
        if (!PlaceableData.GetBounds(heldObject, out Bounds b))
            return;

        heldObject.transform.position += target - b.center;
        float minY = b.min.y + (target.y - b.center.y);

        // Never let it sink into the terrain
        Vector3 pos = heldObject.transform.position;
        Vector3 rayStart = new Vector3(pos.x, cam.position.y + 1f, pos.z);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 30f,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            float floor = hit.point.y + groundClearance;
            if (minY < floor)
                heldObject.transform.position += Vector3.up * (floor - minY);
        }
    }

    // ---------- HOVER HIGHLIGHT ----------
    private void UpdateHover()
    {
        GameObject target = (showHighlight && heldObject == null)
            ? FindPickupTarget(out _)
            : null;

        if (target != hoveredObject)
        {
            ClearHighlight();
            hoveredObject = target;
            if (hoveredObject != null)
                ApplyHighlight(hoveredObject);
        }

        if (hoveredObject != null)
            UpdatePulse();
    }

    private void ApplyHighlight(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r is LineRenderer || r is ParticleSystemRenderer || r is TrailRenderer)
                continue;

            foreach (Material m in r.materials)   // per-object instances, so other objects aren't affected
            {
                MatState s = new MatState { mat = m };

                if (m.HasProperty("_BaseColor")) s.colorId = Shader.PropertyToID("_BaseColor");
                else if (m.HasProperty("_Color")) s.colorId = Shader.PropertyToID("_Color");

                if (s.colorId != -1)
                {
                    s.origColor = m.GetColor(s.colorId);
                    m.SetColor(s.colorId, Color.Lerp(s.origColor, highlightColor, tintStrength));
                }

                if (m.HasProperty("_EmissionColor"))
                {
                    s.hasEmission = true;
                    s.hadEmissionKeyword = m.IsKeywordEnabled("_EMISSION");
                    s.origEmission = m.GetColor("_EmissionColor");
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", highlightColor * emissionIntensity);
                }

                litMaterials.Add(s);
            }
        }
    }

    private void UpdatePulse()
    {
        float k = pulse ? Mathf.Lerp(0.4f, 1f, Mathf.PingPong(Time.time * pulseSpeed, 1f)) : 1f;
        Color glow = highlightColor * (emissionIntensity * k);

        foreach (MatState s in litMaterials)
            if (s.mat != null && s.hasEmission)
                s.mat.SetColor("_EmissionColor", glow);
    }

    private void ClearHighlight()
    {
        foreach (MatState s in litMaterials)
        {
            if (s.mat == null) continue;

            if (s.colorId != -1)
                s.mat.SetColor(s.colorId, s.origColor);

            if (s.hasEmission)
            {
                s.mat.SetColor("_EmissionColor", s.origEmission);
                if (!s.hadEmissionKeyword)
                    s.mat.DisableKeyword("_EMISSION");
            }
        }
        litMaterials.Clear();
    }

    // ---------- TARGETING (shared by hover and pickup) ----------
    private GameObject FindPickupTarget(out PlaceableData data)
    {
        data = null;

        Ray ray = playerCamera.ViewportPointToRay(ScreenCentre);
        RaycastHit[] hits = Physics.RaycastAll(ray, pickupDistance);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            PlaceableData d = hit.collider.GetComponent<PlaceableData>();

            if (d == null && !hit.collider.CompareTag("Pickupable"))
                continue;

            // Only the current instruction's objects can be picked (and highlighted)
            if (instructions != null && (d == null || !instructions.CanPickup(d.type)))
                continue;

            data = d;
            return hit.collider.gameObject;
        }

        return null;
    }

    // ---------- PICK ----------
    public void PickupButtonPressed()
    {
        if (heldObject != null)
            return;

        GameObject target = FindPickupTarget(out PlaceableData data);
        if (target == null)
        {
            Debug.Log("No Pickupable object found");
            return;
        }

        ClearHighlight();
        hoveredObject = null;

        heldObject = target;
        heldData = data;

        if (instructions != null && heldData != null)
            instructions.NotifyPickedUp(heldObject, heldData.type);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        SetCollidersEnabled(heldObject, false);

        float yaw = playerCamera.transform.eulerAngles.y;

        if (heldData != null)
        {
            heldData.BeginHold(yaw);   // unparent, upright, shrink to fit
            if (placeUmbrellaButton != null)
                placeUmbrellaButton.SetActive(true);
        }
        else
        {
            heldObject.transform.SetParent(null);
            heldRotationOffset = Quaternion.Inverse(Quaternion.Euler(0f, yaw, 0f))
                                 * heldObject.transform.rotation;
        }

        Debug.Log("Picked: " + heldObject.name);
    }

    // ---------- DROP / THROW ----------
    public void DropButtonPressed()
    {
        if (heldObject == null)
            return;

        if (heldData != null)
            heldData.EndHold();   // back to real size

        SetCollidersEnabled(heldObject, true);

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
        }

        ReleaseHeld();
    }

    // ---------- PLACE ----------
    public void PlaceUmbrellaButtonPressed()
    {
        if (heldObject == null || heldData == null)
            return;

        Vector3 placePoint = GetPlacePoint();

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        heldData.PlaceAt(placePoint, playerCamera.transform.eulerAngles.y);

        SetCollidersEnabled(heldObject, true);

        if (instructions != null)
            instructions.NotifyPlaced(heldObject, heldData.type);

        ReleaseHeld();
    }

    private Vector3 GetPlacePoint()
    {
        Vector3 camPos = playerCamera.transform.position;
        Vector3 camFwd = playerCamera.transform.forward;

        if (Physics.Raycast(camPos, camFwd, out RaycastHit hit, placeDistance,
                            ~0, QueryTriggerInteraction.Ignore))
            return hit.point;

        Vector3 ahead = camPos + camFwd * placeDistance;
        if (Physics.Raycast(ahead + Vector3.up * 10f, Vector3.down, out hit, 50f,
                            ~0, QueryTriggerInteraction.Ignore))
            return hit.point;

        return ahead;
    }

    // ---------- HELPERS ----------
    private void ReleaseHeld()
    {
        if (placeUmbrellaButton != null)
            placeUmbrellaButton.SetActive(false);

        heldObject = null;
        heldData = null;
    }

    private static void SetCollidersEnabled(GameObject obj, bool enabled)
    {
        foreach (Collider c in obj.GetComponentsInChildren<Collider>())
            c.enabled = enabled;
    }
}