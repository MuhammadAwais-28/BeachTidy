using UnityEngine;
using UnityEngine.UI;

public class PageSnap : MonoBehaviour
{
    public ScrollRect scrollRect;

    private float[] pages;

    void Start()
    {
        pages = new float[]
        {
            0f,
            1f
        };
    }

    void Update()
    {
        if (!Input.GetMouseButton(0))
        {
            float pos = scrollRect.horizontalNormalizedPosition;

            float nearest = pages[0];

            foreach (float page in pages)
            {
                if (Mathf.Abs(pos - page) < Mathf.Abs(pos - nearest))
                    nearest = page;
            }

            scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(scrollRect.horizontalNormalizedPosition,
                nearest,
                Time.deltaTime * 10f);
        }
    }
}