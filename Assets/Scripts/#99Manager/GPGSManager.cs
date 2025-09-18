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
    [SerializeField] private GameObject loadingPanel;     // IntroScene�� �ε� �г�(ó���� ��Ȱ��)
    [SerializeField] private UnityEngine.UI.Image progressFill; // Filled Image (fillAmount ���)
    [SerializeField] private TextMeshProUGUI progressText;      // ����: "85%" ǥ�ÿ�

    [Header("Leaderboard UI (����)")]
    [SerializeField] private TextMeshProUGUI dailyHighScoreText;  // �� ���ϸ� ����
    [SerializeField] private TextMeshProUGUI topRanksText;        // Top N ��ŷ �ؽ�Ʈ

    [Header("Leaderboard Ids")]
    [SerializeField] private string dailyLeaderboardId = GPGSIds.leaderboard_dailyhighscore;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";   // �ٲ� ����

    [SerializeField] Button startOnClickBtn;
    [SerializeField] GameObject startOnClickImage;
    [SerializeField] Button googleLoginBtn;
    int dailyHighScore = 0;
    bool loadingStarted = false;    // �ߺ� ����

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.DebugLogEnabled = false;
        PlayGamesPlatform.Activate();
        //�ڵ� �α��� �õ�
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                Log("�ڵ� �α��� ����");
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
                Log($"�ڵ� �α��� ����: {status}");
                // TODO: IntroScene�� �α��� ��ư�� ������ ���� �α��� �õ� ����
                googleLoginBtn.gameObject.SetActive(true);
            }
        });
#endif
        // IntroScene�� ���� �� �ε� UI�� ����
        if (loadingPanel) loadingPanel.SetActive(false);
        if (progressFill) progressFill.fillAmount = 0f;
        if (progressText) progressText.text = "0%";
    }

    // ====== IntroScene���� UI�� �������� ���� �� ��� ======
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
    // ====== ���� daily/top ���� ���ε� ���� ======
    public void BindUI(TextMeshProUGUI daily, TextMeshProUGUI top = null, TextMeshProUGUI log = null)
    {
        if (daily) dailyHighScoreText = daily;
        if (top) topRanksText = top;
    }

    // ====== �α��� Ʈ���� ======
    public void GPGS_Login()
    {
#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#else
         StartCoroutine(LoadGameSceneAsync("GameScene"));
        Log("GPGS ��Ȱ�� ȯ��");
#endif
    }

#if UNITY_ANDROID && !NO_GPGS
    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            string userID = PlayGamesPlatform.Instance.GetUserId();
            Log($"�α��� ���� : {displayName} / {userID}");

            if (loadingStarted) return;      // �ߺ� �ݹ� ����
            loadingStarted = true;

            // IntroScene�� �ε� UI�� ����� GameScene �񵿱� �ε�
            StartCoroutine(LoadGameSceneAsync(gameSceneName));
        }
        else
        {
            Log($"�α��� ���� : {status}");
        }
    }
#endif

    // ====== GameScene �񵿱� �ε�(IntroScene�� progressFill ���) ======
    IEnumerator LoadGameSceneAsync(string sceneName)
    {
        if (loadingPanel) loadingPanel.SetActive(true);
        UpdateProgressUI(0f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // op.progress�� 0.0 ~ 0.9������ ����
        while (op.progress < 0.9f)
        {
            float normalized = Mathf.Clamp01(op.progress / 0.9f); // 0~1 ������
            UpdateProgressUI(normalized);
            yield return null;
        }

        // 0.9 �� 1.0 ������ ����� �ε巴�� ä��
        float t = 0f;
        const float tail = 0.25f; // ���� �ð�
        while (t < tail)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Lerp(0.9f, 1f, t / tail);
            UpdateProgressUI(normalized);
            yield return null;
        }

        UpdateProgressUI(1f);
        yield return new WaitForSecondsRealtime(0.05f);

        op.allowSceneActivation = true; // ���� ��ȯ
    }

    void UpdateProgressUI(float normalized01)
    {
        if (progressFill) progressFill.fillAmount = Mathf.Clamp01(normalized01);
        if (progressText) progressText.text = Mathf.RoundToInt(Mathf.Clamp01(normalized01) * 100f) + "%";
    }

    // ====== �α��� ���� �� �ݹ� ======
    void EnsureLoginThen(System.Action onReady)
    {
#if UNITY_ANDROID && !NO_GPGS
        if (Social.localUser.authenticated) { onReady?.Invoke(); return; }
        PlayGamesPlatform.Instance.Authenticate(s =>
        {
            if (s == SignInStatus.Success) onReady?.Invoke();
            else Log($"�α��� ���� : {s}");
        });
#else
        Log("GPGS ��Ȱ�� ȯ��");
#endif
    }

    // ====== ���� ����/���ε� ======
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
                success => Log($"[���ε�] {(success ? "����" : "����")}"));
#endif
        });
    }
    public void TestStartOnClick()
    {
        SceneManager.LoadScene("GameScene");
    }

    // ====== �� ���� �ε� �� UI ======
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
                        Log($"[Daily] �� ���� {score:n0} / ��ũ #{data.PlayerScore.rank}");
                    }
                    else
                    {
                        dailyHighScore = 0;
                        if (dailyHighScoreText) dailyHighScoreText.text = "0";
                        Log("[Daily] ��� ����");
                    }
                });
#endif
        });
    }

    // ====== Top N ��ŷ �ε� �� UI ======
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
                        Log("[TopN] ��� �ؽ�Ʈ �̹��ε�");
                        return;
                    }
                    if (data == null || !data.Valid || data.Scores == null || data.Scores.Length == 0)
                    {
                        topRanksText.text = "��ŷ ������ ����";
                        Log("[TopN] ������ ����");
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
                        Log($"[TopN] {scores.Length}�� �ε� �Ϸ�({span})");
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

    // ====== ��ƿ ======
    string ShortId(string id) =>
        string.IsNullOrEmpty(id) ? "Unknown" :
        (id.Length <= 6 ? id : id.Substring(0, 3) + "��" + id.Substring(id.Length - 3, 3));

    void Log(string msg)
    {
        Debug.Log(msg);
    }

    public int GetDailyHighScore() => dailyHighScore;
}
