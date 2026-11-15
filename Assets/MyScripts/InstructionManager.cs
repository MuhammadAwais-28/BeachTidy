using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public enum PlaceableType
{
    None,
    Umbrella,
    BeachChair,
    WoodenBench
}

public class InstructionManager : MonoBehaviour
{
    [Serializable]
    public class Stage
    {
        public string instruction = "Place the umbrella";
        public PlaceableType type = PlaceableType.Umbrella;
        public int total = 9;
        [Tooltip("Text inside this item's card in the ProgressPanel")]
        public TMP_Text countText;

        [NonSerialized] public HashSet<GameObject> placed;
        [NonSerialized] public Color baseColor = Color.white;

        public int PlacedCount => placed != null ? placed.Count : 0;
        public int Remaining => Mathf.Max(0, total - PlacedCount);
    }

    [Header("Instruction panel (text only)")]
    public TMP_Text instructionText;
    public string completedMessage = "All done!";

    [Header("Progress panel counts")]
    [Tooltip("{0} = placed, {1} = total, {2} = remaining.   Examples: \"{0}/{1}\"   \"{2} left\"")]
    public string countFormat = "{0}/{1}";
    public Color completedColor = new Color(0.2f, 1f, 0.3f);

    [Header("Stages (in order)")]
    public List<Stage> stages = new List<Stage>
    {
        new Stage { instruction = "Place the umbrella",       type = PlaceableType.Umbrella,    total = 9  },
        new Stage { instruction = "Place the beach chairs",   type = PlaceableType.BeachChair,  total = 10 },
        new Stage { instruction = "Place the wooden benches", type = PlaceableType.WoodenBench, total = 10 },
    };

    [Header("Saving")]
    [Tooltip("OFF: progress is kept until the app is closed (survives scene changes).\nON: progress is also kept after the app is closed and reopened.")]
    public bool persistToDisk = false;

    [Header("Events")]
    public UnityEvent<int> onStageChanged;   // passes the new stage index
    public UnityEvent onAllCompleted;

    private int current;
    private bool complete;

    public bool IsComplete => complete;

    private void Awake()
    {
        foreach (Stage s in stages)
        {
            s.placed = new HashSet<GameObject>();
            if (s.countText != null)
                s.baseColor = s.countText.color;
        }
    }

    private void Start()
    {
        current = 0;
        complete = stages.Count == 0;

        if (persistToDisk && !GameSession.hasData)
            GameSession.LoadFromDisk();

        if (GameSession.hasData)
            RestoreFromSession();

        Refresh();
        if (!complete)
            onStageChanged.Invoke(current);
    }

    // ---------- SAVE / RESTORE ----------
    private void RestoreFromSession()
    {
        if (stages.Count == 0)
            return;

        SessionData d = GameSession.data;

        complete = d.complete;
        current = complete ? stages.Count : Mathf.Clamp(d.stageIndex, 0, stages.Count - 1);

        // Put every object the player placed earlier back where it was
        foreach (PlacedRecord rec in d.placed)
        {
            if (!PlaceableData.TryGet(rec.id, out PlaceableData pd) || pd == null)
                continue;

            Stage stage = stages.Find(s => s.type == rec.type);
            if (stage == null)
                continue;

            pd.RestorePlaced(rec.position, rec.rotation);
            stage.placed.Add(pd.gameObject);
        }

        // Safety: if stage totals changed since the save, skip finished stages
        while (!complete && stages[current].Remaining == 0)
        {
            current++;
            if (current >= stages.Count)
                complete = true;
        }
    }

    private void SaveSession()
    {
        SessionData d = GameSession.data;
        d.stageIndex = current;
        d.complete = complete;
        d.placed.Clear();

        foreach (Stage s in stages)
        {
            foreach (GameObject go in s.placed)
            {
                PlaceableData pd = go != null ? go.GetComponent<PlaceableData>() : null;
                if (pd == null)
                    continue;

                d.placed.Add(new PlacedRecord
                {
                    id = pd.Id,
                    type = s.type,
                    position = go.transform.position,
                    rotation = go.transform.rotation
                });
            }
        }

        GameSession.hasData = true;

        if (persistToDisk)
            GameSession.SaveToDisk();
    }

    // Hook this to a "New game" / "Restart" button, then reload the scene
    public void ResetProgress()
    {
        GameSession.Clear();
    }

    // ---------- GAMEPLAY ----------
    // Called by PickupSystem: may the player pick up this type right now?
    public bool CanPickup(PlaceableType type)
    {
        return !complete && type != PlaceableType.None && type == stages[current].type;
    }

    // Player picked up an object that was already placed -> it no longer counts
    public void NotifyPickedUp(GameObject obj, PlaceableType type)
    {
        if (complete || type != stages[current].type)
            return;

        if (stages[current].placed.Remove(obj))
        {
            Refresh();
            SaveSession();
        }
    }

    // Player placed an object -> count it
    public void NotifyPlaced(GameObject obj, PlaceableType type)
    {
        if (complete || type != stages[current].type)
            return;

        stages[current].placed.Add(obj);
        Refresh();
        SaveSession();

        if (stages[current].Remaining == 0)
            NextStage();
    }

    private void NextStage()
    {
        current++;

        if (current >= stages.Count)
        {
            complete = true;
            Refresh();
            SaveSession();               // save BEFORE events, in case an event loads another scene
            onAllCompleted.Invoke();
            return;
        }

        Refresh();
        SaveSession();
        onStageChanged.Invoke(current);
    }

    private void Refresh()
    {
        // Instruction panel: text only
        if (instructionText != null)
            instructionText.text = complete ? completedMessage : stages[current].instruction;

        // Progress panel: a count for every item
        foreach (Stage s in stages)
        {
            if (s.countText == null)
                continue;

            s.countText.text = string.Format(countFormat, s.PlacedCount, s.total, s.Remaining);
            s.countText.color = s.Remaining == 0 ? completedColor : s.baseColor;
        }
    }
}