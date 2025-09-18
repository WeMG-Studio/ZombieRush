using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.SocialPlatforms;
using System.Linq;
using System.Text;
using System.Collections;

#if UNITY_ANDROID && !NO_GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class GPGSManager : MonoBehaviour
{
    public static GPGSManager instance;

    [Header("IntroScene UI")]
    [SerializeField] private GameObject loadingPanel;     // IntroScene의 로딩 패널(처음엔 비활성)
    [SerializeField] private UnityEngine.UI.Image progressFill; // Filled Image (fillAmount 사용)
    [SerializeField] private TextMeshProUGUI progressText;      // 선택: "85%" 표시용

    [Header("Leaderboard UI (선택)")]
    [SerializeField] private TextMeshProUGUI dailyHighScoreText;  // 내 데일리 점수
    [SerializeField] private TextMeshProUGUI topRanksText;        // Top N 랭킹 텍스트

    [Header("Leaderboard Ids")]
    [SerializeField] private string dailyLeaderboardId = GPGSIds.leaderboard_dailyhighscore;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";   // 바꿔 쓰기

    [SerializeField] Button startOnClickBtn;
    [SerializeField] GameObject startOnClickImage;
    [SerializeField] Button googleLoginBtn;
    int dailyHighScore = 0;
    bool loadingStarted = false;    // 중복 방지

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.DebugLogEnabled = false;
        PlayGamesPlatform.Activate();
        //자동 로그인 시도
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                Log("자동 로그인 성공");
                if (!loadingStarted)
                {
                    loadingStarted = true;
                    startOnClickBtn.gameObject.SetActive(true);
                    startOnClickImage.gameObject.SetActive(true);
                    //StartCoroutine(LoadGameSceneAsync(gameSceneName));
                }
            }
            else
            {
                Log($"자동 로그인 실패: {status}");
                // TODO: IntroScene에 로그인 버튼을 노출해 수동 로그인 시도 가능
                googleLoginBtn.gameObject.SetActive(true);
            }
        });
#endif
        // IntroScene에 있을 때 로딩 UI는 꺼둠
        if (loadingPanel) loadingPanel.SetActive(false);
        if (progressFill) progressFill.fillAmount = 0f;
        if (progressText) progressText.text = "0%";
    }

    // ====== IntroScene에서 UI를 동적으로 물릴 때 사용 ======
    public void BindIntroUI(
        TextMeshProUGUI _log = null,
        GameObject _loadingPanel = null,
        UnityEngine.UI.Image _progressFill = null,
        TextMeshProUGUI _progressText = null,
        TextMeshProUGUI _daily = null,
        TextMeshProUGUI _top = null)
    {
        if (_loadingPanel) loadingPanel = _loadingPanel;
        if (_progressFill) progressFill = _progressFill;
        if (_progressText) progressText = _progressText;
        if (_daily) dailyHighScoreText = _daily;
        if (_top) topRanksText = _top;
    }

    public void StartOnClick()
    {
        StartCoroutine(LoadGameSceneAsync(gameSceneName));
    }
    // ====== 기존 daily/top 전용 바인딩 유지 ======
    public void BindUI(TextMeshProUGUI daily, TextMeshProUGUI top = null, TextMeshProUGUI log = null)
    {
        if (daily) dailyHighScoreText = daily;
        if (top) topRanksText = top;
    }

    // ====== 로그인 트리거 ======
    public void GPGS_Login()
    {
#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#else
         StartCoroutine(LoadGameSceneAsync("GameScene"));
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

            if (loadingStarted) return;      // 중복 콜백 방지
            loadingStarted = true;

            // IntroScene의 로딩 UI를 사용해 GameScene 비동기 로드
            StartCoroutine(LoadGameSceneAsync(gameSceneName));
        }
        else
        {
            Log($"로그인 실패 : {status}");
        }
    }
#endif

    // ====== GameScene 비동기 로드(IntroScene의 progressFill 사용) ======
    IEnumerator LoadGameSceneAsync(string sceneName)
    {
        if (loadingPanel) loadingPanel.SetActive(true);
        UpdateProgressUI(0f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // op.progress는 0.0 ~ 0.9까지만 증가
        while (op.progress < 0.9f)
        {
            float normalized = Mathf.Clamp01(op.progress / 0.9f); // 0~1 스케일
            UpdateProgressUI(normalized);
            yield return null;
        }

        // 0.9 → 1.0 구간은 연출로 부드럽게 채움
        float t = 0f;
        const float tail = 0.25f; // 연출 시간
        while (t < tail)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Lerp(0.9f, 1f, t / tail);
            UpdateProgressUI(normalized);
            yield return null;
        }

        UpdateProgressUI(1f);
        yield return new WaitForSecondsRealtime(0.05f);

        op.allowSceneActivation = true; // 실제 전환
    }

    void UpdateProgressUI(float normalized01)
    {
        if (progressFill) progressFill.fillAmount = Mathf.Clamp01(normalized01);
        if (progressText) progressText.text = Mathf.RoundToInt(Mathf.Clamp01(normalized01) * 100f) + "%";
    }

    // ====== 로그인 보장 후 콜백 ======
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
    public void TestStartOnClick()
    {
        SceneManager.LoadScene("GameScene");
    }

    // ====== 내 점수 로드 → UI ======
    public void LoadDailyHighScoreToUI()
    {
        EnsureLoginThen(() =>
        {
#if UNITY_ANDROID && !NO_GPGS
            PlayGamesPlatform.Instance.LoadScores(
                dailyLeaderboardId,
                LeaderboardStart.PlayerCentered,
                1,
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
    public void LoadTopRanksToUI(
        int count = 10,
#if UNITY_ANDROID && !NO_GPGS
        GooglePlayGames.BasicApi.LeaderboardTimeSpan span = GooglePlayGames.BasicApi.LeaderboardTimeSpan.Daily
#else
        int span = 0
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

                    var scores = data.Scores;
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

    public void RefreshAll(
        int topN = 10,
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
        Debug.Log(msg);
    }

    public int GetDailyHighScore() => dailyHighScore;
}
