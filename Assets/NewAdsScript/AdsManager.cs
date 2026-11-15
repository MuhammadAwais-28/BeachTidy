using System;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.SceneManagement;


public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Ad Format Toggles (set by Remote Config or manually)")]
    public bool BlockAllAds;
    public bool BlockBannerAd;
    public bool BlockInterstitialAd;
    public bool BlockRewardedAd;
    public bool BlockAppOpenAd;
    public bool BlockNativeAd;
    public bool BlockRewardedInterstitialAd;
    public bool BlockMRecAd;

    [Header("Handlers")]
    public BannerAdHandler BannerHandler;
    public InterstitialAdHandler InterstitialHandler;
    public RewardedAdHandler RewardedHandler;
    public AppOpenAdHandler AppOpenHandler;
    public NativeAdHandler NativeHandler;
    public MRecAdHandler MRecHandler;
    public RewardedInterstitialAdHandler RewardedInterstitialHandler;
    public BannerAdHandler BannerLeftHandler;
    public BannerAdHandler BannerRightHandler;

    public bool IsSdkInitialized { get; private set; }

    [Header("Startup")]
    [Tooltip("If Remote Config has not answered after this many seconds, the SDK starts anyway with the Inspector values.")]
    public float remoteConfigTimeoutSeconds = 8f;

    // True while an interstitial/rewarded-interstitial is on screen. Showing
    // one of these also fires Unity's OnApplicationPause/Focus events, so we
    // use this flag to stop the App Open ad from popping up on top of it.
    private bool _isShowingBlockingAd;

    // Ensures the App Open ad shows exactly once on cold start (the moment
    // the very first ad finishes loading), separate from the normal
    // resume-from-background trigger in OnApplicationPause.
    private bool _hasShownColdStartAppOpenAd;

    [Tooltip("Grace period (seconds) after an interstitial/rewarded interstitial closes before App Open ads are allowed again. Needed because closing a full-screen ad also triggers Unity's own resume event — without this delay, an App Open ad can slip through right as the interstitial closes.")]
    public float blockingAdCooldownSeconds = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("[AdsManager] Awake — instance set, marked DontDestroyOnLoad.");
        }
        else
        {
            Debug.LogWarning("[AdsManager] Duplicate AdsManager found — destroying this one.");
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // A duplicate is already scheduled for destruction; it must not start the SDK
        if (Instance != this)
            return;

        Debug.Log("[AdsManager] Start — waiting for consent and remote config before initializing SDK.");
        StartCoroutine(WaitForConsentThenInit());
    }

    private System.Collections.IEnumerator WaitForConsentThenInit()
    {
#if UNITY_EDITOR
        // Google's consent (UMP) is only a placeholder in the Editor and may never finish
        Debug.Log("[AdsManager] Editor: skipping the consent wait.");
#else
        float consentWaited = 0f;
        float nextLog = 2f;
        while (!ConsentManager.CanRequestAds)
        {
            consentWaited += Time.unscaledDeltaTime;
            if (consentWaited >= nextLog)
            {
                Debug.Log("[AdsManager] Still waiting for consent (ConsentManager.CanRequestAds = false)...");
                nextLog += 2f;
            }
            yield return null;
        }
        Debug.Log("[AdsManager] Consent OK (CanRequestAds = true).");
#endif

        // Remote config: wait for it, but never forever
        float rcWaited = 0f;
        while (!RemoteAdsConfig.IsReady && rcWaited < remoteConfigTimeoutSeconds)
        {
            rcWaited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (RemoteAdsConfig.IsReady && RemoteAdsConfig.Instance != null)
        {
            RemoteAdsConfig.Instance.PushValuesInto(this);
            Debug.Log("[AdsManager] Remote config values applied before SDK init.");
        }
        else
        {
            Debug.LogWarning("[AdsManager] Remote config not ready after " + remoteConfigTimeoutSeconds +
                             "s (or instance missing) — using local/Inspector defaults.");
        }

        Debug.Log("[AdsManager] Proceeding to initialize SDK.");
        InitializeSdk();
    }

    private void InitializeSdk()
    {
        Debug.Log("[AdsManager] Calling MobileAds.Initialize...");

        MobileAds.Initialize(initStatus =>
        {
            IsSdkInitialized = true;
            Debug.Log("[AdsManager] Google Mobile Ads SDK initialized.");

            if (BlockAllAds)
            {
                Debug.Log("[AdsManager] BlockAllAds is true — hiding/destroying all ads instead of loading.");
                HideAllAds();
                return;
            }

            if (!BlockBannerAd)
            {
                Debug.Log("[AdsManager] Loading left/right banner ads.");
                if (BannerLeftHandler == null) Debug.LogWarning("[AdsManager] BannerLeftHandler not assigned!");
                else BannerLeftHandler.LoadAd();

                if (BannerRightHandler == null) Debug.LogWarning("[AdsManager] BannerRightHandler not assigned!");
                else BannerRightHandler.LoadAd();
            }
            else
            {
                Debug.Log("[AdsManager] BlockBannerAd is true — skipping banner load.");
            }

            if (!BlockInterstitialAd)
            {
                Debug.Log("[AdsManager] Loading interstitial ad.");
                if (InterstitialHandler == null) Debug.LogWarning("[AdsManager] InterstitialHandler not assigned!");
                else InterstitialHandler.LoadAd();
            }
            else
            {
                Debug.Log("[AdsManager] BlockInterstitialAd is true — skipping interstitial load.");
            }

            if (!BlockRewardedAd)
            {
                Debug.Log("[AdsManager] Loading rewarded ad.");
                if (RewardedHandler == null) Debug.LogWarning("[AdsManager] RewardedHandler not assigned!");
                else RewardedHandler.LoadAd();
            }
            else
            {
                Debug.Log("[AdsManager] BlockRewardedAd is true — skipping rewarded load.");
            }

            if (!BlockAppOpenAd)
            {
                Debug.Log("[AdsManager] Loading App Open ad and subscribing cold-start show.");
                if (AppOpenHandler == null)
                {
                    Debug.LogWarning("[AdsManager] AppOpenHandler not assigned!");
                }
                else
                {
                    AppOpenHandler.OnAdLoadedEvent += ShowColdStartAppOpenAdOnce;
                    AppOpenHandler.LoadAd();
                }
            }
            else
            {
                Debug.Log("[AdsManager] BlockAppOpenAd is true — skipping App Open load.");
            }

            if (!BlockNativeAd)
            {
                Debug.Log("[AdsManager] Loading native ad.");
                if (NativeHandler == null) Debug.LogError("[AdsManager] NativeHandler not assigned!");
                else NativeHandler.LoadAd();
            }
            else
            {
                Debug.Log("[AdsManager] BlockNativeAd is true — skipping native load.");
            }

            if (!BlockRewardedInterstitialAd)
            {
                Debug.Log("[AdsManager] Loading rewarded interstitial ad.");
                if (RewardedInterstitialHandler == null) Debug.LogWarning("[AdsManager] RewardedInterstitialHandler not assigned!");
                else RewardedInterstitialHandler.LoadAd();
            }
            else
            {
                Debug.Log("[AdsManager] BlockRewardedInterstitialAd is true — skipping rewarded interstitial load.");
            }

            if (!BlockMRecAd)
            {
                Debug.Log("[AdsManager] Loading MREC ad.");
                if (MRecHandler == null) Debug.LogWarning("[AdsManager] MRecHandler not assigned!");
                else MRecHandler.LoadAd();
            }
            else
            {
                Debug.Log("[AdsManager] BlockMRecAd is true — skipping MREC load.");
            }
        });
    }

    // After every scene load, make sure the left/right banners still exist.
    // Editor: the placeholder banner is a scene object and dies with its scene, so it is created again.
    // Device: native banners survive scene loads, so they are only reloaded if they are gone.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsSdkInitialized || BlockAllAds || BlockBannerAd)
            return;

        if (BannerLeftHandler != null) BannerLeftHandler.RefreshAfterSceneChange();
        if (BannerRightHandler != null) BannerRightHandler.RefreshAfterSceneChange();
    }

    /// <summary>
    /// Fired once, the first time the App Open ad finishes loading after app
    /// launch — shows it immediately (the "cold start" App Open ad), then
    /// unsubscribes so later reloads (after each show, or on resume) don't
    /// trigger this again. Subsequent shows are driven entirely by
    /// OnApplicationPause via TryShowAppOpenAd().
    /// </summary>
    private void ShowColdStartAppOpenAdOnce()
    {
        if (_hasShownColdStartAppOpenAd)
        {
            Debug.Log("[AdsManager] Cold-start App Open ad already shown — ignoring this load event.");
            return;
        }
        _hasShownColdStartAppOpenAd = true;

        if (AppOpenHandler != null)
            AppOpenHandler.OnAdLoadedEvent -= ShowColdStartAppOpenAdOnce;

        Debug.Log("[AdsManager] First App Open ad load complete — attempting cold-start show.");
        TryShowAppOpenAd();
    }

    /// <summary>
    /// Central entry point for any button that should show a plain interstitial
    /// before proceeding. onComplete always fires exactly once.
    /// </summary>
    public void ShowInterstitial(Action onComplete)
    {
        if (BlockAllAds || BlockInterstitialAd || InterstitialHandler == null)
        {
            Debug.Log($"[AdsManager] ShowInterstitial skipped (BlockAllAds={BlockAllAds}, BlockInterstitialAd={BlockInterstitialAd}, HandlerNull={InterstitialHandler == null}). Invoking onComplete immediately.");
            onComplete?.Invoke();
            return;
        }

        Debug.Log("[AdsManager] ShowInterstitial — attempting to show.");
        _isShowingBlockingAd = true;
        InterstitialHandler.Show(() =>
        {
            Debug.Log("[AdsManager] Interstitial closed/failed/skip — invoking onComplete, starting cooldown.");
            onComplete?.Invoke();
            StartCoroutine(ClearBlockingAdFlagAfterDelay());
        });
    }

    /// <summary>
    /// Central entry point for any button (e.g. Play) that should show a
    /// plain rewarded ad before proceeding. onComplete always fires exactly
    /// once so the caller's follow-up action never gets stuck. onUserRewarded
    /// fires only if the player watches the ad to completion — pass null if
    /// you don't want to grant anything for watching.
    /// </summary>
    public void ShowRewarded(Action onComplete, Action onUserRewarded = null)
    {
        if (BlockAllAds || BlockRewardedAd || RewardedHandler == null)
        {
            Debug.Log($"[AdsManager] ShowRewarded skipped (BlockAllAds={BlockAllAds}, BlockRewardedAd={BlockRewardedAd}, HandlerNull={RewardedHandler == null}). Invoking onComplete immediately.");
            onComplete?.Invoke();
            return;
        }

        Debug.Log("[AdsManager] ShowRewarded — attempting to show.");
        _isShowingBlockingAd = true;
        RewardedHandler.Show(() =>
        {
            Debug.Log("[AdsManager] Rewarded ad closed/failed/skip — invoking onComplete, starting cooldown.");
            onComplete?.Invoke();
            StartCoroutine(ClearBlockingAdFlagAfterDelay());
        }, onUserRewarded);
    }

    /// <summary>
    /// Central entry point for any button (e.g. Play) that should show a
    /// rewarded interstitial before proceeding. onComplete always fires
    /// exactly once so the caller's follow-up action never gets stuck.
    /// onUserRewarded fires only if the player watches the ad to completion —
    /// pass null if you don't want to grant anything for watching.
    /// </summary>
    public void ShowRewardedInterstitial(Action onComplete, Action onUserRewarded = null)
    {
        if (BlockAllAds || BlockRewardedInterstitialAd || RewardedInterstitialHandler == null)
        {
            Debug.Log($"[AdsManager] ShowRewardedInterstitial skipped (BlockAllAds={BlockAllAds}, BlockRewardedInterstitialAd={BlockRewardedInterstitialAd}, HandlerNull={RewardedInterstitialHandler == null}). Invoking onComplete immediately.");
            onComplete?.Invoke();
            return;
        }

        Debug.Log("[AdsManager] ShowRewardedInterstitial — attempting to show.");
        _isShowingBlockingAd = true;
        RewardedInterstitialHandler.Show(() =>
        {
            Debug.Log("[AdsManager] Rewarded interstitial closed/failed/skip — invoking onComplete, starting cooldown.");
            onComplete?.Invoke();
            StartCoroutine(ClearBlockingAdFlagAfterDelay());
        }, onUserRewarded);
    }

    private System.Collections.IEnumerator ClearBlockingAdFlagAfterDelay()
    {
        yield return new WaitForSeconds(blockingAdCooldownSeconds);
        _isShowingBlockingAd = false;
        Debug.Log("[AdsManager] Blocking-ad cooldown finished — App Open ads allowed again.");
    }

    // Called by Unity whenever the app is backgrounded/foregrounded
    // (e.g. player switches to another app and comes back).
    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[AdsManager] OnApplicationPause({pauseStatus})");
        if (!pauseStatus)
        {
            TryShowAppOpenAd();
        }
    }

    private void TryShowAppOpenAd()
    {
        if (!IsSdkInitialized)
        {
            Debug.Log("[AdsManager] TryShowAppOpenAd skipped — SDK not initialized yet.");
            return;
        }
        if (BlockAllAds || BlockAppOpenAd)
        {
            Debug.Log($"[AdsManager] TryShowAppOpenAd skipped — blocked (BlockAllAds={BlockAllAds}, BlockAppOpenAd={BlockAppOpenAd}).");
            return;
        }
        if (_isShowingBlockingAd)
        {
            Debug.Log("[AdsManager] TryShowAppOpenAd skipped — another blocking ad is currently showing/cooling down.");
            return;
        }
        if (AppOpenHandler == null)
        {
            Debug.LogWarning("[AdsManager] TryShowAppOpenAd skipped — AppOpenHandler not assigned.");
            return;
        }

        Debug.Log("[AdsManager] TryShowAppOpenAd — showing now.");
        _isShowingBlockingAd = true;
        AppOpenHandler.Show(() =>
        {
            _isShowingBlockingAd = false;
            Debug.Log("[AdsManager] App Open ad closed/failed/skip — flag cleared.");
        });
    }

    public void HideAllAds()
    {
        BannerHandler?.DestroyAd();
        if (BannerLeftHandler != null) BannerLeftHandler.DestroyAd();
        if (BannerRightHandler != null) BannerRightHandler.DestroyAd();
        InterstitialHandler?.DestroyAd();
        RewardedHandler?.DestroyAd();
        AppOpenHandler?.DestroyAd();
        NativeHandler?.DestroyAd();
        RewardedInterstitialHandler?.DestroyAd();
        MRecHandler?.DestroyAd();
        Debug.Log("[AdsManager] All ads hidden/destroyed.");
    }

    public void ApplyBlockFlags()
    {
        if (!IsSdkInitialized)
        {
            Debug.Log("[AdsManager] ApplyBlockFlags skipped — SDK not initialized yet.");
            return;
        }

        Debug.Log("[AdsManager] ApplyBlockFlags — re-applying current block toggles.");

        if (BlockAllAds)
        {
            HideAllAds();
            return;
        }

        if (BlockBannerAd) BannerHandler?.DestroyAd(); else BannerHandler?.LoadAd();

        if (BlockBannerAd)
        {
            if (BannerLeftHandler != null) BannerLeftHandler.DestroyAd();
            if (BannerRightHandler != null) BannerRightHandler.DestroyAd();
        }
        else
        {
            if (BannerLeftHandler != null) BannerLeftHandler.LoadAd();
            if (BannerRightHandler != null) BannerRightHandler.LoadAd();
        }

        if (BlockInterstitialAd) InterstitialHandler?.DestroyAd(); else InterstitialHandler?.LoadAd();
        if (BlockRewardedAd) RewardedHandler?.DestroyAd(); else RewardedHandler?.LoadAd();
        if (BlockAppOpenAd) AppOpenHandler?.DestroyAd(); else AppOpenHandler?.LoadAd();
        if (BlockNativeAd) NativeHandler?.DestroyAd(); else NativeHandler?.LoadAd();
        if (BlockRewardedInterstitialAd) RewardedInterstitialHandler?.DestroyAd(); else RewardedInterstitialHandler?.LoadAd();
        if (BlockMRecAd) MRecHandler?.DestroyAd(); else MRecHandler?.LoadAd();
    }
}