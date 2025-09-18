using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] GameOverPanel gameOverPanel;
    [SerializeField] GameContinuePanel gameContinuePanel;

    [Header("Characters")]
    public Player player;

    [Header("Core Refs")]
    public GameParams config;
    public RailManager rail;

    public Button btnAdvance;
    public Button btnFix;

    [SerializeField] TextMeshProUGUI gameStartCountText;
    public bool isGameStart = false;

    float distance;
    int level;
    bool isDead;
    public bool isContinued = false;

    [Header("Wall Scroll")]
    public float scrollBurstBase = 2.5f;
    public float scrollBurstPerLevel = 0.1f;
    [SerializeField] AudioClip forwardClip;
    [SerializeField] AudioClip rotateClip;
    [SerializeField] AudioClip[] readyClips;
    [SerializeField] AudioClip[] inGameBgms;

    // �ܺ� ����
    public float Distance => distance;
    public int Level => level;
    public bool IsDead => isDead;

    public event Action<float> OnDistanceNormalized;
    public event Action<int> OnLevelChanged;
    public event Action<int> OnStepsChanged;
    public event Action<string> OnDied;

    [SerializeField] string gameSceneName = "GameScene";
    GameHUD gameHUD;

    // CSV �ε�/����
    [Serializable]
    public class LevelRow { public int ID; public int Score; public float Decay; public int Straight_MIN; public int Straight_MAX; public List<int> PatternIDs; }
    [Serializable]
    public class PatternRow { public int ID; public int[] PatternArray; }

    Dictionary<int, LevelRow> levelTable = new Dictionary<int, LevelRow>();
    Dictionary<int, PatternRow> patternTable = new Dictionary<int, PatternRow>();
    System.Random rng = new System.Random();

    int pendingStraightLeft = 0;
    int[] pendingPattern = null;
    int patternIndex = 0;

    // ?? �̾��ϱ� ����/���� ��ŵ �ð�
    float _resumeGraceUntil = 0f;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        if (btnAdvance) btnAdvance.onClick.AddListener(() => Advance());
        if (btnFix) btnFix.onClick.AddListener(() => FixAndAdvance());

        LoadLevelCsv();
        LoadPatternCsv();
        
        distance = config.maxDistance;
        EmitAll();
    }

    void Update()
    {
        if (!isGameStart) return;
        if (isDead) return;

        // ?? �̾��ϱ� ���� ���� �ð��� distance ����/���� ���� ��ŵ
        if (Time.unscaledTime < _resumeGraceUntil) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) Advance();
        if (Input.GetKeyDown(KeyCode.Space)) FixAndAdvance();

        float decay = GetDecayForLevel(level);
        float prev = distance;
        distance -= decay * Time.deltaTime;

        if (distance <= 0f)
        {
            Debug.LogError($"[Die@Update] distance<=0 | prev={prev:F3}, now={distance:F3}, decay={decay:F3}, level={level}");
            distance = 0f;
            Die("���񿡰� ������!");
        }

        if (!Mathf.Approximately(prev, distance))
            EmitDistance();
    }

    void OnEnable()
    {
        transform.SetParent(null);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (s.name == gameSceneName)
        {
            InitGame();
            EmitAll();
        }
    }

    // ?? ������ ContinueGame
    public void ContinueGame()
    {
        if (!isDead) return;
        Debug.Log("���� �̾��ϱ� ����");

        distance = Mathf.Max(distance, config.maxDistance * 0.5f); // ������ �Ÿ� ����
        StopCoroutine("FailFx");
        Time.timeScale = 1f;

        isDead = false;
        isGameStart = true;

        gameContinuePanel.gameObject.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);

        if (player != null) player.Revive();

        // rail ���� �������� ��� �ʱ�ȭ
        if (rail != null && rail.CurrentTile == null)
        {
            rail.InitRail();
            PrepareNextSegment();
            Debug.Log("[Continue] rail ����ε� �� ���׸�Ʈ �غ�");
        }

        // ?? ���� ���� ��ġ �ܻ� ����: ��ư ��� ��Ȱ��ȭ
        if (btnAdvance) btnAdvance.interactable = false;
        if (btnFix) btnFix.interactable = false;

        // ?? �̾��ϱ� �� 1.5�� ����/������� ��ŵ
        _resumeGraceUntil = Time.unscaledTime + 1.5f;

        EmitAll();
        StartCoroutine(CoReEnableButtons(1.5f));
    }

    IEnumerator CoReEnableButtons(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (btnAdvance) btnAdvance.interactable = true;
        if (btnFix) btnFix.interactable = true;
        Debug.Log("Resume Game");
    }

    public IEnumerator StartGame()
    {
        SoundManager.instance.StopAllBGM();
        gameStartCountText.gameObject.SetActive(true);
        gameStartCountText.text = "3";
        SoundManager.instance.PlaySound(readyClips[0]);
        yield return new WaitForSeconds(1.0f);
        gameStartCountText.text = "2";
        SoundManager.instance.PlaySound(readyClips[1]);
        yield return new WaitForSeconds(1.0f);
        gameStartCountText.text = "1";
        SoundManager.instance.PlaySound(readyClips[2]);
        yield return new WaitForSeconds(1.0f);
        SoundManager.instance.PlaySound(readyClips[3]);
        gameStartCountText.gameObject.SetActive(false);

        int a = UnityEngine.Random.Range(0, 2);
        SoundManager.instance.PlayBGM(inGameBgms[a]);
        isGameStart = true;
        isDead = false;
        PrepareNextSegment();
        yield return null;
    }

    public void InitGame()
    {
        gameOverPanel.gameObject.SetActive(false);
        gameContinuePanel.gameObject.SetActive(false);
        isContinued = false;
        isDead = false;
        isGameStart = false;

        distance = config.maxDistance;
        level = 0;

        // ����: rail.InitRail();
        rail.BuildRailFresh();   // ? ���� ���� ��������

        pendingStraightLeft = 0;
        pendingPattern = null;
        patternIndex = 0;

        if (btnAdvance) btnAdvance.interactable = true;
        if (btnFix) btnFix.interactable = true;

        EmitAll();
    }

    public IEnumerator RetryGame()
    {
        InitGame();
        StartCoroutine(StartGame());
        yield return null;
    }

    // ---------------- ���� ��ư�� ----------------

    public void Advance()
    {
        if (!isGameStart || isDead) return;
        SoundManager.instance.PlaySound(forwardClip);
        if (rail.CurrentTile.Type != RailType.Straight)
        {
            Debug.LogError($"[Die@Advance] ������ ���� | tile={rail.CurrentTile.Type}, steps={rail.Steps}");
            Die("������ ���Ͽ��� ����!");
            return;
        }
        OnAdvancedOneStep();
    }

    public void FixAndAdvance()
    {
        if (!isGameStart || isDead) return;
        if (rail.CurrentTile == null) return;

        SoundManager.instance.PlaySound(rotateClip);
        if (rail.CurrentTile.Type == RailType.Straight)
        {
            Debug.LogError($"[Die@Fix] �������� ���� | tile={rail.CurrentTile.Type}, steps={rail.Steps}");
            Die("�������� ���� �� �й�!");
            return;
        }

        rail.FixCurrent();
        OnAdvancedOneStep();
    }

    void OnAdvancedOneStep()
    {
        ProgressSegmentState();
        distance = Mathf.Min(config.maxDistance, distance + config.gainPerStep);
        LevelRecalc();
        EmitAll();
        if (player) player.PlayStepBounce(1f);
    }

    void LevelRecalc() => level = rail.Steps / config.stepsPerLevel;

    // ?? �α� ��ȭ�� Die
    void Die(string reason)
    {
        if (isDead) return;
        isDead = true;

        Debug.LogError($"[Die] reason={reason}\n{System.Environment.StackTrace}");

        OnDied?.Invoke(reason);
        StartCoroutine(FailFx());
        if (!isContinued)
        {
            gameContinuePanel.gameObject.SetActive(true);
            StartCoroutine(gameContinuePanel.CountDown(rail.Steps));
        }
        else
        {
            gameOverPanel.gameObject.SetActive(true);
            gameOverPanel.UpdateUI(rail.Steps);
        }
        
        isGameStart = false;
    }
    public void GameOverPanelActive(bool type)
    {
        gameOverPanel.gameObject.SetActive(type);
        gameContinuePanel.gameObject.SetActive(!type);
    }

    IEnumerator FailFx()
    {
        float original = Time.timeScale;
        Time.timeScale = config.failSlowmo;
        yield return new WaitForSecondsRealtime(config.failSlowmoTime);
        Time.timeScale = original;
    }

    void EmitDistance() => OnDistanceNormalized?.Invoke(distance / config.maxDistance);
    void EmitLevel() => OnLevelChanged?.Invoke(level);
    void EmitSteps() => OnStepsChanged?.Invoke(rail.Steps);
    void EmitAll() { EmitDistance(); EmitLevel(); EmitSteps(); }

    // ===========================================================
    // =============== CSV ��� ���� ���׸�Ʈ ���� ===============
    // ===========================================================

    void PrepareNextSegment()
    {
        var lv = GetLevelRow(level);
        pendingStraightLeft = UnityEngine.Random.Range(lv.Straight_MIN, lv.Straight_MAX + 1);
        pendingPattern = Array.Empty<int>();
        patternIndex = 0;

        int pickedId = -1;
        for (int tries = 0; tries < 5; tries++)
        {
            if (lv.PatternIDs == null || lv.PatternIDs.Count == 0) break;
            int patternId = lv.PatternIDs[rng.Next(0, lv.PatternIDs.Count)];
            var arr = GetPatternArray(patternId);
            if (arr != null && arr.Length > 0)
            {
                pendingPattern = arr;
                pickedId = patternId;
                break;
            }
        }
        Debug.Log($"[GameManager] �� New Segment �غ�� | Level={level}, Straight={pendingStraightLeft}, " +
                  $"PatternID={(pickedId == -1 ? "NONE" : pickedId.ToString())}, Pattern=[{string.Join(",", pendingPattern)}]");
    }

    void ProgressSegmentState()
    {
        if (pendingStraightLeft > 0)
        {
            pendingStraightLeft--;
            Debug.Log($"[GameManager] �� Straight ���� (���� {pendingStraightLeft})");
            rail.TryAdvanceForced(false);

            if (pendingStraightLeft == 0 && (pendingPattern == null || pendingPattern.Length == 0))
            {
                Debug.Log("[GameManager] �� Straight �� �� ���� ���׸�Ʈ �غ�");
                PrepareNextSegment();
            }
            return;
        }

        if (pendingPattern != null && patternIndex < pendingPattern.Length)
        {
            int cmd = pendingPattern[patternIndex++];
            if (cmd == 1)
            {
                Debug.Log($"[GameManager] �� Pattern[{patternIndex - 1}] = 0 �� ȸ��");
                rail.TryAdvanceForced(true);
            }
            else
            {
                Debug.Log($"[GameManager] �� Pattern[{patternIndex - 1}] = 1 �� ����");
                rail.TryAdvanceForced(false);
            }
            if (patternIndex >= pendingPattern.Length)
            {
                Debug.Log("[GameManager] �� Pattern ���� �� ���� ���׸�Ʈ �غ�");
                PrepareNextSegment();
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] �� ���� ����� �� ���� ���׸�Ʈ ���� �غ�");
            PrepareNextSegment();
        }
    }

    // ===========================================================
    // ===================== CSV ��ƿ & �ļ� =====================
    // ===========================================================

    float GetDecayForLevel(int lv) { var row = GetLevelRow(lv); return row.Decay; }

    LevelRow GetLevelRow(int lv)
    {
        // ��Ȯ�� �����ϴ� �����̸� �״�� ��ȯ
        if (levelTable.TryGetValue(lv, out var row))
            return row;

        // �ּ� / �ִ� Ű ã��
        int minKey = int.MaxValue;
        int maxKey = int.MinValue;
        LevelRow minRow = null;
        LevelRow maxRow = null;

        foreach (var kv in levelTable)
        {
            if (kv.Key < minKey) { minKey = kv.Key; minRow = kv.Value; }
            if (kv.Key > maxKey) { maxKey = kv.Key; maxRow = kv.Value; }
        }

        // CSV ��ü�� ����ִ� ���
        if (minRow == null || maxRow == null)
        {
            Debug.LogError("Level CSV�� �������!");
            return new LevelRow { ID = 0, Decay = 1f, Straight_MIN = 1, Straight_MAX = 1, PatternIDs = new List<int>() };
        }

        // lv�� �ִ밪�� ���� ��� �� �׻� ������(�ִ�) ���� ������ ��ȯ
        if (lv > maxKey)
        {
            return maxRow;
        }

        // lv�� �ּҺ��� ���� ��� �� �ּ� ���� ��ȯ
        if (lv < minKey)
        {
            return minRow;
        }
        return minRow;
    }
    /*LevelRow GetLevelRow(int lv)
    {
        if (levelTable.TryGetValue(lv, out var row)) return row;
        int minKey = int.MaxValue; LevelRow minRow = null;
        foreach (var kv in levelTable) if (kv.Key < minKey) { minKey = kv.Key; minRow = kv.Value; }
        if (minRow != null) return minRow;
        Debug.LogError("Level CSV�� �������!");
        return new LevelRow { ID = 0, Decay = 1f, Straight_MIN = 1, Straight_MAX = 1, PatternIDs = new List<int>() };
    }*/

    int[] GetPatternArray(int patternId)
    {
        if (!patternTable.TryGetValue(patternId, out var row) || row.PatternArray == null)
            return Array.Empty<int>();
        return row.PatternArray;
    }

    void LoadLevelCsv()
    {
        TextAsset csv = Resources.Load<TextAsset>("csv/Level");
        if (csv == null) { Debug.LogError("csv/Level.csv �� ã�� �� ����"); return; }
        var rows = ParseCsv(csv.text);
        if (rows.Count == 0) return;

        var header = rows[0];
        int idxID = Array.IndexOf(header, "ID");
        int idxScore = Array.IndexOf(header, "Score");
        int idxDecay = Array.IndexOf(header, "Decay");
        int idxStraightMin = Array.IndexOf(header, "Straight_MIN");
        int idxStraightMax = Array.IndexOf(header, "Straight_MAX");
        int idxPatternArr = Array.IndexOf(header, "PatternID_Array");

        levelTable.Clear();
        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (IsEmptyRow(r)) continue;
            LevelRow item = new LevelRow();
            item.ID = ToInt(r, idxID);
            item.Score = ToInt(r, idxScore);
            item.Decay = ToFloat(r, idxDecay);
            item.Straight_MIN = ToInt(r, idxStraightMin);
            item.Straight_MAX = ToInt(r, idxStraightMax);
            item.PatternIDs = ParseIntTuple(Get(r, idxPatternArr));
            levelTable[item.ID] = item;
        }
    }

    void LoadPatternCsv()
    {
        TextAsset csv = Resources.Load<TextAsset>("csv/Pattern");
        if (csv == null) { Debug.LogError("csv/Pattern.csv �� ã�� �� ����"); return; }
        var rows = ParseCsv(csv.text);
        if (rows.Count == 0) return;

        var header = rows[0];
        int idxID = Array.IndexOf(header, "ID");
        int idxArr = Array.IndexOf(header, "Pattern_Array");

        patternTable.Clear();
        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (IsEmptyRow(r)) continue;
            int id = ToInt(r, idxID);
            string raw = Get(r, idxArr);
            int[] seq = ParsePatternSeq(raw);
            Debug.Log($"[LoadPatternCsv] ID={id}, Raw='{raw}', Parsed=[{string.Join(",", seq)}]");
            patternTable[id] = new PatternRow { ID = id, PatternArray = seq };
        }
    }

    static int[] ParsePatternSeq(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<int>();
        string s = raw.Trim().Trim('"').Trim();
        if (s == "-1") return Array.Empty<int>();
        if (s.StartsWith("(") && s.EndsWith(")")) s = s.Substring(1, s.Length - 2).Trim();
        if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();
        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        List<int> seq = new List<int>();
        foreach (var p in parts) if (int.TryParse(p.Trim(), out int v)) seq.Add(v);
        return seq.ToArray();
    }

    static bool IsEmptyRow(string[] row)
    {
        if (row == null || row.Length == 0) return true;
        foreach (var s in row) if (!string.IsNullOrWhiteSpace(s)) return false;
        return true;
    }
    static string Get(string[] arr, int i) => (i >= 0 && i < arr.Length) ? arr[i] : "";
    static int ToInt(string[] arr, int i) { int v; return int.TryParse(Get(arr, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0; }
    static float ToFloat(string[] arr, int i) { float v; return float.TryParse(Get(arr, i), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f; }
    static List<int> ParseIntTuple(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new List<int>();
        s = s.Trim().Trim('"');
        if (s.StartsWith("(") && s.EndsWith(")")) s = s.Substring(1, s.Length - 2);
        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        List<int> list = new List<int>();
        foreach (var p in parts) if (int.TryParse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) list.Add(v);
        return list;
    }

    public static List<string[]> ParseCsv(string text)
    {
        var result = new List<string[]>();
        using (var reader = new StringReader(text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                result.Add(SplitCsvLine(line));
        }
        return result;
    }

    public static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        if (line == null) return fields.ToArray();
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(cur.ToString());
                cur.Length = 0;
            }
            else cur.Append(c);
        }
        fields.Add(cur.ToString());
        return fields.ToArray();
    }
}
