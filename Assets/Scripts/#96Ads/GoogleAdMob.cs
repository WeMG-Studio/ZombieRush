using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public class GoogleAdMob : MonoBehaviour
{
#if UNITY_EDITOR
    private string _adUnitId = "ca-app-pub-3940256099942544/5224354917";
    private string _adFrontId = "ca-app-pub-3490273194196393/1933586979";
#elif UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3490273194196393/6228055205";
    private string _adFrontId = "ca-app-pub-3940256099942544/1033173712";
#else
  private string _adUnitId = "unused";
  private string _adFrontId = "unused";
#endif
    private RewardedAd _rewardedAd;
    private InterstitialAd _interstitial;
    public static GoogleAdMob instance;

    public float buttonClickTime = 0.0f;
    public int gamePlayAdsCount = 0;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        DontDestroyOnLoad(this);
    }
    public void Start()
    {
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            Debug.Log("Initialized Google AdMob");
        });
        LoadFrontAd();
    }
    private void Update()
    {
        if (buttonClickTime >= -1.0f) buttonClickTime -= Time.deltaTime;
    }
    public void ContinueRewardAds()
    {
        if (buttonClickTime <= 0.0f)
        {
            buttonClickTime = 3.0f;
            LoadRewardedAd();
        }
    }
    public void ShowFrontAds()
    {
        ShowInterstitial();
    }
    public void LoadFrontAd()
    {
        if (_interstitial != null)
        {
            _interstitial.Destroy();
            _interstitial = null;
        }

        Debug.Log("전면 광고 로드 시작");

        var adRequest = new AdRequest();

        InterstitialAd.Load(_adFrontId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("전면 광고 로드 실패: " + error);
                    return;
                }

                Debug.Log("전면 광고 로드 성공");
                _interstitial = ad;

                // 광고 이벤트 핸들러 등록
                _interstitial.OnAdFullScreenContentClosed += () =>
                {
                    Debug.Log("전면 광고 닫힘 → 다시 로드");
                    LoadFrontAd();
                };

                _interstitial.OnAdFullScreenContentFailed += (AdError err) =>
                {
                    Debug.LogWarning("전면 광고 표시 실패: " + err);
                };
            });
    }

    // 광고 보여주기
    public void ShowInterstitial()
    {
        if (_interstitial != null && _interstitial.CanShowAd())
        {
            Debug.Log("전면광고 보여줌");
            gamePlayAdsCount = 0;
            _interstitial.Show();
        }
        else
        {
            Debug.Log("전면광고 준비 안됨, 새로 로드");
            LoadFrontAd();
        }
    }
    //보상형 광고 로드
    public void LoadRewardedAd()
    {
        // Clean up the old ad before loading a new one.
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }
        Debug.Log("Loading the rewarded ad.");
        var adRequest = new AdRequest();
        RewardedAd.Load(_adUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    GameManager.instance.GameOverPanelActive(true);
                    return;
                }

                Debug.Log("Rewarded ad loaded with response : "
                          + ad.GetResponseInfo());

                _rewardedAd = ad;
                ShowRewardedAd();
            });
    }
    // 이벤트 핸들러
    private void HandleOnAdLoaded(object sender, EventArgs args)
    {
        Debug.Log("전면광고 로드 완료");
    }

    private void HandleOnAdClosed(object sender, EventArgs args)
    {
        Debug.Log("전면광고 닫힘 → 다시 로드");
        LoadFrontAd();
    }

    void OnDestroy()
    {
        if (_interstitial != null)
        {
            _interstitial.Destroy();
        }
    }
    //리워드 콜백으로 보상형 광고 표시
    public void ShowRewardedAd()
    {
        const string rewardMsg =
            "Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                // TODO: 이어하기 시작
                Debug.Log("이어하기 시작");
                GameManager.instance.ContinueGame();
            });
        }
    }

    //보상형 광고 미리 로드
    private void RegisterReloadHandler(RewardedAd ad)
    {
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded Ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedAd();
        };
    }

    //보상형 광고 이벤트 수신
    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(string.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
        };
    }
}
