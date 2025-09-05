using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms;
using System.Linq;
using System.Text;

#if UNITY_ANDROID && !NO_GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class GPGSManager : MonoBehaviour
{
    public static GPGSManager instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private TextMeshProUGUI dailyHighScoreText;  // 내 데일리 점수 표시
    [SerializeField] private TextMeshProUGUI topRanksText;        // Top N 랭킹 텍스트 표시

    [Header("Leaderboard Ids")]
    [SerializeField] private string dailyLeaderboardId = GPGSIds.leaderboard_dailyhighscore;

    int dailyHighScore = 0;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // GPGS 활성화(안전모드: config 없이 Activate만)
#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.DebugLogEnabled = false;
        PlayGamesPlatform.Activate();
#endif
    }

    // ====== 동적 UI 바인딩======
    public void BindUI(TextMeshProUGUI daily, TextMeshProUGUI top = null, TextMeshProUGUI log = null)
    {
        if (daily) dailyHighScoreText = daily;
        if (top) topRanksText = top;
        if (log) logText = log;
    }

    // ====== 로그인 ======
    public void GPGS_Login()
    {
#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#else
        Log("GPGS 비활성 환경");
#endif
    }

#if UNITY_ANDROID && !NO_GPGS
    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            string userID = PlayGamesPlatform.Instance.GetUserId();
            Log($"로그인 성공 : {displayName} / {userID}");
        }
        else
        {
            Log($"로그인 실패 : {status}");
        }
    }
#endif

    void EnsureLoginThen(System.Action onReady)
    {
#if UNITY_ANDROID && !NO_GPGS
        if (Social.localUser.authenticated) { onReady?.Invoke(); return; }
        PlayGamesPlatform.Instance.Authenticate(s =>
        {
            if (s == SignInStatus.Success) onReady?.Invoke();
            else Log($"로그인 실패 : {s}");
        });
#else
        Log("GPGS 비활성 환경");
#endif
    }

    // ====== 점수 세팅/업로드 ======
    public void SetDailyHighScore(int score, bool uploadNow = true)
    {
        dailyHighScore = Mathf.Max(0, score);
        if (dailyHighScoreText) dailyHighScoreText.text = dailyHighScore.ToString("n0");
        if (uploadNow) UpdateDailyHighScore();
    }

    public void UpdateDailyHighScore()
    {
        if (dailyHighScoreText) dailyHighScoreText.text = dailyHighScore.ToString("n0");

        EnsureLoginThen(() =>
        {
#if UNITY_ANDROID && !NO_GPGS
            PlayGamesPlatform.Instance.ReportScore(
                dailyHighScore,
                dailyLeaderboardId,
                success => Log($"[업로드] {(success ? "성공" : "실패")}"));
#endif
        });
    }

    // ====== 내 점수 로드 → UI ======
    public void LoadDailyHighScoreToUI()
    {
        EnsureLoginThen(() =>
        {
#if UNITY_ANDROID && !NO_GPGS
            PlayGamesPlatform.Instance.LoadScores(
                dailyLeaderboardId,
                LeaderboardStart.PlayerCentered,   // 내 위치 기준
                1,                                  // PlayerScore만 필요
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.Daily,
                data =>
                {
                    if (data != null && data.Valid && data.PlayerScore != null)
                    {
                        long score = data.PlayerScore.value;
                        dailyHighScore = (int)score;
                        if (dailyHighScoreText) dailyHighScoreText.text = $"{score:n0}";
                        Log($"[Daily] 내 점수 {score:n0} / 랭크 #{data.PlayerScore.rank}");
                    }
                    else
                    {
                        dailyHighScore = 0;
                        if (dailyHighScoreText) dailyHighScoreText.text = "0";
                        Log("[Daily] 기록 없음");
                    }
                });
#endif
        });
    }

    // ====== Top N 랭킹 로드 → UI ======
    public void LoadTopRanksToUI(int count = 10,
#if UNITY_ANDROID && !NO_GPGS
                                 GooglePlayGames.BasicApi.LeaderboardTimeSpan span = GooglePlayGames.BasicApi.LeaderboardTimeSpan.Daily
#else
                                 int span = 0 // 더미(컴파일용)
#endif
                                 )
    {
        EnsureLoginThen(() =>
        {
#if UNITY_ANDROID && !NO_GPGS
            PlayGamesPlatform.Instance.LoadScores(
                dailyLeaderboardId,
                LeaderboardStart.TopScores,
                Mathf.Max(1, count),
                LeaderboardCollection.Public,
                span,
                data =>
                {
                    if (topRanksText == null)
                    {
                        Log("[TopN] 대상 텍스트 미바인딩");
                        return;
                    }
                    if (data == null || !data.Valid || data.Scores == null || data.Scores.Length == 0)
                    {
                        topRanksText.text = "랭킹 데이터 없음";
                        Log("[TopN] 데이터 없음");
                        return;
                    }

                    var scores = data.Scores;        // IScore[]
                    var ids = scores.Select(s => s.userID).Distinct().ToArray();

                    Social.LoadUsers(ids, profiles =>
                    {
                        var nameMap = profiles?.ToDictionary(p => p.id, p => p.userName)
                                      ?? new System.Collections.Generic.Dictionary<string, string>();

                        string myId = Social.localUser?.id ?? "";
                        var sb = new StringBuilder();

                        for (int i = 0; i < scores.Length; i++)
                        {
                            var s = scores[i];
                            string name = nameMap.TryGetValue(s.userID, out var n) ? n : ShortId(s.userID);
                            int rank = s.rank > 0 ? s.rank : (i + 1);
                            bool isMe = s.userID == myId;

                            if (isMe) sb.Append("<color=#FFD54F>");
                            sb.AppendFormat("{0,2}. {1}  -  {2:n0}\n", rank, name, s.value);
                            if (isMe) sb.Append("</color>");
                        }

                        topRanksText.text = sb.ToString().TrimEnd();
                        Log($"[TopN] {scores.Length}개 로드 완료({span})");
                    });
                });
#endif
        });
    }

    // ====== 원샷 리프레시(로비 버튼에 연결하기 좋음) ======
    public void RefreshAll(int topN = 10,
#if UNITY_ANDROID && !NO_GPGS
                           GooglePlayGames.BasicApi.LeaderboardTimeSpan span = GooglePlayGames.BasicApi.LeaderboardTimeSpan.Daily
#else
                           int span = 0
#endif
                           )
    {
        LoadDailyHighScoreToUI();
        LoadTopRanksToUI(topN, span);
    }

    // ====== 선택: 기본 UI 열기 ======
    public void LeaderBoardUIUpdate()
    {
        EnsureLoginThen(() =>
        {
#if UNITY_ANDROID && !NO_GPGS
            PlayGamesPlatform.Instance.ShowLeaderboardUI(dailyLeaderboardId);
#endif
        });
    }

    public void ShowAchievementsUI()
    {
        EnsureLoginThen(() =>
        {
#if UNITY_ANDROID && !NO_GPGS
            PlayGamesPlatform.Instance.ShowAchievementsUI();
#endif
        });
    }

    // ====== 유틸 ======
    string ShortId(string id) =>
        string.IsNullOrEmpty(id) ? "Unknown" :
        (id.Length <= 6 ? id : id.Substring(0, 3) + "…" + id.Substring(id.Length - 3, 3));

    void Log(string msg)
    {
        if (logText) logText.text = msg;
        Debug.Log(msg);
    }

    // 접근자(원하면 외부에서 점수 읽기)
    public int GetDailyHighScore() => dailyHighScore;
}
