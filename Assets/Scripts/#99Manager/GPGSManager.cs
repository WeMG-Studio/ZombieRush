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
    [SerializeField] private TextMeshProUGUI dailyHighScoreText;  // �� ���ϸ� ���� ǥ��
    [SerializeField] private TextMeshProUGUI topRanksText;        // Top N ��ŷ �ؽ�Ʈ ǥ��

    [Header("Leaderboard Ids")]
    [SerializeField] private string dailyLeaderboardId = GPGSIds.leaderboard_dailyhighscore;

    int dailyHighScore = 0;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // GPGS Ȱ��ȭ(�������: config ���� Activate��)
#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.DebugLogEnabled = false;
        PlayGamesPlatform.Activate();
#endif
    }

    // ====== ���� UI ���ε�======
    public void BindUI(TextMeshProUGUI daily, TextMeshProUGUI top = null, TextMeshProUGUI log = null)
    {
        if (daily) dailyHighScoreText = daily;
        if (top) topRanksText = top;
        if (log) logText = log;
    }

    // ====== �α��� ======
    public void GPGS_Login()
    {
#if UNITY_ANDROID && !NO_GPGS
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#else
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
        }
        else
        {
            Log($"�α��� ���� : {status}");
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

    // ====== �� ���� �ε� �� UI ======
    public void LoadDailyHighScoreToUI()
    {
        EnsureLoginThen(() =>
        {
#if UNITY_ANDROID && !NO_GPGS
            PlayGamesPlatform.Instance.LoadScores(
                dailyLeaderboardId,
                LeaderboardStart.PlayerCentered,   // �� ��ġ ����
                1,                                  // PlayerScore�� �ʿ�
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
    public void LoadTopRanksToUI(int count = 10,
#if UNITY_ANDROID && !NO_GPGS
                                 GooglePlayGames.BasicApi.LeaderboardTimeSpan span = GooglePlayGames.BasicApi.LeaderboardTimeSpan.Daily
#else
                                 int span = 0 // ����(�����Ͽ�)
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
                        Log($"[TopN] {scores.Length}�� �ε� �Ϸ�({span})");
                    });
                });
#endif
        });
    }

    // ====== ���� ��������(�κ� ��ư�� �����ϱ� ����) ======
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

    // ====== ����: �⺻ UI ���� ======
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
        if (logText) logText.text = msg;
        Debug.Log(msg);
    }

    // ������(���ϸ� �ܺο��� ���� �б�)
    public int GetDailyHighScore() => dailyHighScore;
}
