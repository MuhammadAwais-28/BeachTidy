using GoogleMobileAds.Api;
using UnityEngine;

public class BannerAdHandler : MonoBehaviour
{
    public enum Side
    {
        [Tooltip("Right if Banner Position is TopRight/BottomRight, otherwise Left")]
        Auto,
        Left,    // uses AdUnitIds.Banner
        Right    // uses AdUnitIds.BannerRight
    }

    [Header("Placement")]
    public Side side = Side.Auto;
    public AdPosition bannerPosition = AdPosition.TopLeft;   // set RightBannerAd to TopRight in the Inspector

    [Header("Debug")]
    [Tooltip("Logs the unit id used and, when a banner is destroyed, WHO destroyed it")]
    public bool debugLog = true;

    private BannerView _bannerView;

    private bool IsRight
    {
        get
        {
            if (side == Side.Right) return true;
            if (side == Side.Left) return false;
            return bannerPosition == AdPosition.TopRight || bannerPosition == AdPosition.BottomRight;
        }
    }

    private string UnitId => IsRight ? AdUnitIds.BannerRight : AdUnitIds.Banner;

    public void LoadAd()
    {
        DestroyAd();

        string unitId = UnitId;
        if (debugLog)
            Debug.Log("[BannerAdHandler:" + name + "] Loading " + (IsRight ? "RIGHT" : "LEFT") +
                      " banner, unit " + unitId + ", position " + bannerPosition);

        _bannerView = new BannerView(unitId, AdSize.Banner, bannerPosition);

        _bannerView.OnBannerAdLoaded += () => Debug.Log("[BannerAdHandler:" + name + "] Ad loaded.");
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            Debug.LogWarning("[BannerAdHandler:" + name + "] Load failed: " + error);
        _bannerView.OnAdPaid += (AdValue value) =>
            Debug.Log($"[BannerAdHandler:{name}] Paid {value.Value} {value.CurrencyCode}");

        _bannerView.LoadAd(new AdRequest());
    }

    // Called by AdsManager after every scene load.
    // Editor: the placeholder banner is a GameObject inside the scene that was active when it was
    //         created, so it is destroyed with that scene -> create it again.
    // Device: the banner is a native overlay and survives scene loads -> only reload if it's gone.
    public void RefreshAfterSceneChange()
    {
#if UNITY_EDITOR
        LoadAd();
#else
        if (_bannerView == null)
            LoadAd();
#endif
    }

    public void Show()
    {
        _bannerView?.Show();
    }

    public void Hide()
    {
        _bannerView?.Hide();
    }

    public void DestroyAd()
    {
        if (_bannerView == null)
            return;

        if (debugLog)
            Debug.Log("[BannerAdHandler:" + name + "] DestroyAd called from:\n" + StackTraceUtility.ExtractStackTrace());

        try
        {
            _bannerView.Destroy();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[BannerAdHandler:" + name + "] Destroy threw: " + e.Message);
        }

        _bannerView = null;
    }

    private void OnDestroy()
    {
        if (debugLog && _bannerView != null)
            Debug.Log("[BannerAdHandler:" + name + "] The handler object itself is being destroyed (scene unload or duplicate).");

        DestroyAd();
    }
}