using UnityEngine;
using TMPro;
using System.Collections;
#if UNITY_ANDROID && !NO_GPGS
using GooglePlayGames.BasicApi;
#endif

public class LobbyLeaderboardUI : MonoBehaviour
{
    public TextMeshProUGUI dailyText;
    public TextMeshProUGUI topText;
    public TextMeshProUGUI logText;

    public int topN = 10;
#if UNITY_ANDROID && !NO_GPGS
    public LeaderboardTimeSpan span = LeaderboardTimeSpan.Daily;
#endif

    IEnumerator Start()
    {
        yield return null; // DDOL 매니저 준비 대기
        if (GPGSManager.instance == null) yield break;

        GPGSManager.instance.BindUI(dailyText, topText, logText);
        GPGSManager.instance.GPGS_Login();
#if UNITY_ANDROID && !NO_GPGS
        GPGSManager.instance.RefreshAll(topN, span);
#else
        GPGSManager.instance.RefreshAll(topN);
#endif
    }
    public void DailyLeaderboardOnClick()
    {
        GPGSManager.instance.UpdateDailyHighScore();
    }
}
