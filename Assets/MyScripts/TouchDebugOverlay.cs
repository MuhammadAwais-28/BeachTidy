using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shows live touch info on screen (works in a device build) and also writes Debug.Log lines.
/// Add to any GameObject, assign the joystick and the touch field. Remove after testing.
/// </summary>
public class TouchDebugOverlay : MonoBehaviour
{
    public FixedJoystick joystick;
    public FixedTouchField touchField;

    public bool show = true;
    [Range(0.5f, 3f)] public float textScale = 1f;
    [Tooltip("A single-frame camera movement bigger than this (in pixels) is logged as a SPIKE")]
    public float spikeThreshold = 150f;

    private readonly StringBuilder sb = new StringBuilder();
    private readonly List<RaycastResult> results = new List<RaycastResult>();
    private PointerEventData pointerData;
    private GUIStyle style;

    private float maxLookDelta;
    private bool idMismatchSeen;
    private int prevTouchCount;
    private string lastEvent = "-";
    private string lastSpike = "-";

    private void Update()
    {
        // ---- log every finger down / up with what UI element it hit ----
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            if (t.fingerId != i)
                idMismatchSeen = true;   // fingerId is NOT the same as the array index

            if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                lastEvent = $"{t.phase} idx={i} fingerId={t.fingerId} ui={UiUnder(t.position)}";
                Debug.Log("[TouchDebug] " + lastEvent);
            }
        }

        // ---- log the moment the finger count changes, with the state at that instant ----
        if (Input.touchCount != prevTouchCount)
        {
            Debug.Log($"[TouchDebug] touchCount {prevTouchCount} -> {Input.touchCount} | " +
                      $"joy=({Hor():F2},{Ver():F2}) lookDelta={LookDelta():F1}");
            prevTouchCount = Input.touchCount;
        }

        // ---- watch for camera jumps ----
        float d = LookDelta();
        if (d > maxLookDelta) maxLookDelta = d;

        if (d > spikeThreshold)
        {
            lastSpike = $"{Time.time:F1}s delta={d:F0}px touches={Input.touchCount}";
            Debug.LogWarning("[TouchDebug] SPIKE " + lastSpike);
        }
    }

    private void OnGUI()
    {
        if (!show) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            style.normal.textColor = Color.white;
        }
        style.fontSize = Mathf.RoundToInt(Screen.height * 0.024f * textScale);

        sb.Length = 0;
        sb.AppendLine($"touchCount: <b>{Input.touchCount}</b>   multiTouch: {Input.multiTouchEnabled}");

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            sb.AppendLine($"[{i}] fingerId={t.fingerId} {t.phase} pos=({t.position.x:F0},{t.position.y:F0}) ui={UiUnder(t.position)}");
        }

        sb.AppendLine($"mousePos (touch sim): ({Input.mousePosition.x:F0},{Input.mousePosition.y:F0})");
        sb.AppendLine($"joystick: ({Hor():F2}, {Ver():F2})");
        sb.AppendLine($"lookDelta: {LookDelta():F1}   max: {maxLookDelta:F0}");
        sb.AppendLine($"fingerId != index seen: <b>{idMismatchSeen}</b>");
        sb.AppendLine($"last event: {lastEvent}");
        sb.AppendLine($"last SPIKE: {lastSpike}");

        Rect r = new Rect(10, 10, Screen.width * 0.55f, Screen.height * 0.5f);
        GUI.Box(r, GUIContent.none);
        GUI.Label(r, sb.ToString(), style);
    }

    // ---------- helpers ----------
    private float Hor() { return joystick != null ? joystick.Horizontal : 0f; }
    private float Ver() { return joystick != null ? joystick.Vertical : 0f; }
    private float LookDelta() { return touchField != null ? touchField.TouchDist.magnitude : 0f; }

    private string UiUnder(Vector2 pos)
    {
        if (EventSystem.current == null) return "no EventSystem";

        if (pointerData == null)
            pointerData = new PointerEventData(EventSystem.current);

        pointerData.position = pos;
        results.Clear();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0 ? results[0].gameObject.name : "nothing";
    }
}
