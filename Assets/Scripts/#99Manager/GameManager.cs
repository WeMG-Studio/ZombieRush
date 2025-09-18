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

    // 외부 노출
    public float Distance => distance;
    public int Level => level;
    public bool IsDead => isDead;

    public event Action<float> OnDistanceNormalized;
    public event Action<int> OnLevelChanged;
    public event Action<int> OnStepsChanged;
    public event Action<string> OnDied;

    [SerializeField] string gameSceneName = "GameScene";
    GameHUD gameHUD;

    // CSV 로드/보관
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

    // ?? 이어하기 무적/판정 스킵 시간
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

        // ?? 이어하기 직후 일정 시간은 distance 감소/죽음 판정 스킵
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
            Die("좀비에게 잡혔어!");
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

    // ?? 수정된 ContinueGame
    public void ContinueGame()
    {
        if (!isDead) return;
        Debug.Log("게임 이어하기 시작");

        distance = Mathf.Max(distance, config.maxDistance * 0.5f); // 안전한 거리 보정
        StopCoroutine("FailFx");
        Time.timeScale = 1f;

        isDead = false;
        isGameStart = true;

        gameContinuePanel.gameObject.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);

        if (player != null) player.Revive();

        // rail 상태 비정상일 경우 초기화
        if (rail != null && rail.CurrentTile == null)
        {
            rail.InitRail();
            PrepareNextSegment();
            Debug.Log("[Continue] rail 재바인딩 및 세그먼트 준비");
        }

        // ?? 광고 닫힘 터치 잔상 방지: 버튼 잠깐 비활성화
        if (btnAdvance) btnAdvance.interactable = false;
        if (btnFix) btnFix.interactable = false;

        // ?? 이어하기 후 1.5초 무적/사망판정 스킵
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

        // 기존: rail.InitRail();
        rail.BuildRailFresh();   // ? 비우고 새로 생성까지

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

    // ---------------- 진행 버튼들 ----------------

    public void Advance()
    {
        if (!isGameStart || isDead) return;
        SoundManager.instance.PlaySound(forwardClip);
        if (rail.CurrentTile.Type != RailType.Straight)
        {
            Debug.LogError($"[Die@Advance] 비직선 전진 | tile={rail.CurrentTile.Type}, steps={rail.Steps}");
            Die("비직선 레일에서 전진!");
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
            Debug.LogError($"[Die@Fix] 직선에서 교정 | tile={rail.CurrentTile.Type}, steps={rail.Steps}");
            Die("직선에서 교정 시 패배!");
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

    // ?? 로그 강화된 Die
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
    // =============== CSV 기반 진행 세그먼트 로직 ===============
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
        Debug.Log($"[GameManager] ▶ New Segment 준비됨 | Level={level}, Straight={pendingStraightLeft}, " +
                  $"PatternID={(pickedId == -1 ? "NONE" : pickedId.ToString())}, Pattern=[{string.Join(",", pendingPattern)}]");
    }

    void ProgressSegmentState()
    {
        if (pendingStraightLeft > 0)
        {
            pendingStraightLeft--;
            Debug.Log($"[GameManager] ▶ Straight 진행 (남은 {pendingStraightLeft})");
            rail.TryAdvanceForced(false);

            if (pendingStraightLeft == 0 && (pendingPattern == null || pendingPattern.Length == 0))
            {
                Debug.Log("[GameManager] ▶ Straight 끝 → 다음 세그먼트 준비");
                PrepareNextSegment();
            }
            return;
        }

        if (pendingPattern != null && patternIndex < pendingPattern.Length)
        {
            int cmd = pendingPattern[patternIndex++];
            if (cmd == 1)
            {
                Debug.Log($"[GameManager] ▶ Pattern[{patternIndex - 1}] = 0 → 회전");
                rail.TryAdvanceForced(true);
            }
            else
            {
                Debug.Log($"[GameManager] ▶ Pattern[{patternIndex - 1}] = 1 → 직선");
                rail.TryAdvanceForced(false);
            }
            if (patternIndex >= pendingPattern.Length)
            {
                Debug.Log("[GameManager] ▶ Pattern 종료 → 다음 세그먼트 준비");
                PrepareNextSegment();
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] ▶ 패턴 비었음 → 다음 세그먼트 강제 준비");
            PrepareNextSegment();
        }
    }

    // ===========================================================
    // ===================== CSV 유틸 & 파서 =====================
    // ===========================================================

    float GetDecayForLevel(int lv) { var row = GetLevelRow(lv); return row.Decay; }

    LevelRow GetLevelRow(int lv)
    {
        // 정확히 존재하는 레벨이면 그대로 반환
        if (levelTable.TryGetValue(lv, out var row))
            return row;

        // 최소 / 최대 키 찾기
        int minKey = int.MaxValue;
        int maxKey = int.MinValue;
        LevelRow minRow = null;
        LevelRow maxRow = null;

        foreach (var kv in levelTable)
        {
            if (kv.Key < minKey) { minKey = kv.Key; minRow = kv.Value; }
            if (kv.Key > maxKey) { maxKey = kv.Key; maxRow = kv.Value; }
        }

        // CSV 자체가 비어있는 경우
        if (minRow == null || maxRow == null)
        {
            Debug.LogError("Level CSV가 비어있음!");
            return new LevelRow { ID = 0, Decay = 1f, Straight_MIN = 1, Straight_MAX = 1, PatternIDs = new List<int>() };
        }

        // lv가 최대값을 넘은 경우 → 항상 마지막(최대) 레벨 데이터 반환
        if (lv > maxKey)
        {
            return maxRow;
        }

        // lv가 최소보다 작은 경우 → 최소 레벨 반환
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
        Debug.LogError("Level CSV가 비어있음!");
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
        if (csv == null) { Debug.LogError("csv/Level.csv 를 찾을 수 없음"); return; }
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
        if (csv == null) { Debug.LogError("csv/Pattern.csv 를 찾을 수 없음"); return; }
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
