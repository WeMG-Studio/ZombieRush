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

    [Header("Characters")]
    public Player player;

    [Header("Core Refs")]
    public GameParams config;       // maxDistance / gainPerStep / stepsPerLevel / failSlowmo 등만 사용
    public RailManager rail;

    public Button btnAdvance;
    public Button btnFix;

    [SerializeField] TextMeshProUGUI gameStartCountText;
    public bool isGameStart = false;

    float distance;
    int level;           // 현재 레벨(= Level.csv의 ID)
    bool isDead;

    [Header("Wall Scroll")]
    public VerticalLoopScroller[] wallScrollers;
    public float scrollBurstBase = 2.5f;
    public float scrollBurstPerLevel = 0.1f;

    // 외부 노출
    public float Distance => distance;
    public int Level => level;
    public bool IsDead => isDead;

    public event Action<float> OnDistanceNormalized; // 0~1
    public event Action<int> OnLevelChanged;
    public event Action<int> OnStepsChanged;
    public event Action<string> OnDied;

    [SerializeField] string gameSceneName = "GameScene";
     GameHUD gameHUD;
    // ---------------- CSV 로드/보관 ----------------
    [Serializable]
    public class LevelRow
    {
        public int ID;
        public int Score;
        public float Decay;           // 초당 감소량
        public int Straight_MIN;
        public int Straight_MAX;
        public List<int> PatternIDs;  // PatternID_Array
    }

    [Serializable]
    public class PatternRow
    {
        public int ID;
        public int[] PatternArray;    // 0=회전, 1=전진
    }

    Dictionary<int, LevelRow> levelTable = new Dictionary<int, LevelRow>();
    Dictionary<int, PatternRow> patternTable = new Dictionary<int, PatternRow>();
    System.Random rng = new System.Random();

    // 현재 구간 제어 상태
    int pendingStraightLeft = 0;         // 남은 "무조건 전진" 수
    int[] pendingPattern = null;         // 이어서 적용할 패턴(0/1)
    int patternIndex = 0;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        //DontDestroyOnLoad(gameObject);

        // 버튼 핸들러
        if (btnAdvance) btnAdvance.onClick.AddListener(() => Advance());
        if (btnFix) btnFix.onClick.AddListener(() => FixAndAdvance());

        // CSV 로드
        LoadLevelCsv();   // csv/Level.csv
        LoadPatternCsv(); // csv/Pattern.csv

        // 기본 세팅
        distance = config.maxDistance;   // GameParams의 나머지 값은 그대로 사용 (maxDistance 등). :contentReference[oaicite:3]{index=3}
        EmitAll();
    }

    void Update()
    {
        if (!isGameStart) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) Advance();
        if (Input.GetKeyDown(KeyCode.Space)) FixAndAdvance();

        if (isDead) return;

        // --- decay를 CSV의 Decay로 대체 ---
        float decay = GetDecayForLevel(level); // 레벨=ID 기준
        float prev = distance;
        distance -= decay * Time.deltaTime;    // 초당 감소량 적용 (CSV). :contentReference[oaicite:4]{index=4}

        if (distance <= 0f)
        {
            distance = 0f;
            Die("좀비에게 잡혔어!");
        }

        if (!Mathf.Approximately(prev, distance))
            EmitDistance();
    }
    void OnEnable()
    {
        // DDOL은 루트여야 함. (혹시 부모가 있으면 끊기)
        transform.SetParent(null);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        //RebindFromScene(); 

        if (s.name == gameSceneName)
        {
            InitGame();             // ★ 여기서 distance = config.maxDistance 로 리셋
            EmitAll();              // ★ HUD 등이 초기값을 확실히 받게 브로드캐스트
        }
    }

    void RebindFromScene()
    {
        // 1) 씬에서 바인더 찾기
        var binder = FindObjectOfType<GameSceneBinder>();
        if (!binder)
        {
            Debug.LogWarning("GameSceneBinder를 찾지 못함(이 씬에서 GameManager 참조 불필요한 경우일 수 있음).");
            return;
        }

        // 2) 기존 버튼 리스너 정리 (중복 호출 방지)
        btnAdvance?.onClick.RemoveAllListeners();
        btnFix?.onClick.RemoveAllListeners();

        // 3) 새 참조로 모두 재바인딩
        gameOverPanel = binder.gameOverPanel;
        player = binder.player;
        config = binder.config;
        rail = binder.rail;
        btnAdvance = binder.btnAdvance;
        btnFix = binder.btnFix;
        gameStartCountText = binder.gameStartCountText;
        wallScrollers = binder.wallScrollers;
        binder.gameHUD.game = this;

        // 4) 버튼 리스너 다시 연결
        if (btnAdvance) btnAdvance.onClick.AddListener(Advance);
        if (btnFix) btnFix.onClick.AddListener(FixAndAdvance);

        // 5) 씬 기준 초기화가 필요하면 여기서
        EmitAll();
    }
    public IEnumerator StartGame()
    {
        gameStartCountText.gameObject.SetActive(true);
        gameStartCountText.text = "3";
        yield return new WaitForSeconds(1.0f);
        gameStartCountText.text = "2";
        yield return new WaitForSeconds(1.0f);
        gameStartCountText.text = "1";
        yield return new WaitForSeconds(1.0f);
        gameStartCountText.gameObject.SetActive(false);

        isGameStart = true;
        isDead = false;

        // 첫 구간 준비: Straight 구간부터 시작
        PrepareNextSegment();

        yield return null;
    }

    public void InitGame()
    {
        gameOverPanel.gameObject.SetActive(false);
        isDead = false;
        isGameStart = false;

        distance = config.maxDistance;   // 풀 세팅. :contentReference[oaicite:5]{index=5}
        level = 0;
        rail.InitRail();

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

    void ApplyScrollBurst()
    {
        float amount = scrollBurstBase + level * scrollBurstPerLevel;
        if (wallScrollers != null)
            foreach (var sc in wallScrollers)
                if (sc) sc.AddBurst(amount);
    }

    // ---------------- 진행 버튼들 ----------------

    // 우측 버튼: 전진
    public void Advance()
    {
        if (!isGameStart || isDead) return;

        // 현재 타일이 직선이어야만 전진 가능
        if (rail.CurrentTile.Type != RailType.Straight)
        {
            Die("비직선 레일에서 전진!");
            return;
        }

        // 전진 동작은 ProgressSegmentState()에서 처리
        OnAdvancedOneStep();
    }

    // 좌측 버튼: 교정 + 전진 (직선에서 교정하면 즉사)
    public void FixAndAdvance()
    {
        if (!isGameStart || isDead) return;
        if (rail.CurrentTile == null) return;

        if (rail.CurrentTile.Type == RailType.Straight)
        {
            Die("직선에서 교정 시 패배!");
            return;
        }

        rail.FixCurrent();

        // 전진 동작은 ProgressSegmentState()에서 처리
        OnAdvancedOneStep();
    }

    void OnAdvancedOneStep()
    {
        ProgressSegmentState();
        distance = Mathf.Min(config.maxDistance, distance + config.gainPerStep);

        LevelRecalc();
        EmitAll();
        ApplyScrollBurst();
        if (player) player.PlayStepBounce(1f);
    }

    void LevelRecalc()
    {
        // rail.Steps를 이용해 레벨 계산(=ID). score=rail.Steps와 동기. :contentReference[oaicite:8]{index=8}
        level = rail.Steps / config.stepsPerLevel;
    }

    void Die(string reason)
    {
        if (isDead) return;
        isDead = true;
        OnDied?.Invoke(reason);
        StartCoroutine(FailFx());
        gameOverPanel.gameObject.SetActive(true);
        gameOverPanel.UpdateUI(rail.Steps);
        isGameStart = false;
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
    // Straight 구간(무조건 전진) → Pattern 구간(0/1 시퀀스) → 반복
    // ===========================================================

    void PrepareNextSegment()
    {
        var lv = GetLevelRow(level);

        pendingStraightLeft = UnityEngine.Random.Range(lv.Straight_MIN, lv.Straight_MAX + 1);

        // 패턴 재시도(최대 5회) ? 비어있는 패턴을 피함
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
            int cmd = pendingPattern[patternIndex++]; // 0=회전, 1=직선

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

    float GetDecayForLevel(int lv)
    {
        var row = GetLevelRow(lv);
        return row.Decay;
    }

    LevelRow GetLevelRow(int lv)
    {
        if (levelTable.TryGetValue(lv, out var row))
            return row;

        // 가장 낮은 ID로 fallback
        int minKey = int.MaxValue;
        LevelRow minRow = null;
        foreach (var kv in levelTable)
        {
            if (kv.Key < minKey)
            {
                minKey = kv.Key;
                minRow = kv.Value;
            }
        }

        if (minRow != null)
            return minRow;

        Debug.LogError("Level CSV가 비어있음!");
        return new LevelRow { ID = 0, Decay = 1f, Straight_MIN = 1, Straight_MAX = 1, PatternIDs = new List<int>() };
    }

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

        // 헤더
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
            item.Score = ToInt(r, idxScore);   // 참고용: 실제 점수는 rail.Steps
            item.Decay = ToFloat(r, idxDecay);
            item.Straight_MIN = ToInt(r, idxStraightMin);
            item.Straight_MAX = ToInt(r, idxStraightMax);
            item.PatternIDs = ParseIntTuple(Get(r, idxPatternArr)); // "(1,2,3)" → [1,2,3]

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

            // 디버그: 로딩시 무엇이 들어오는지 확인
            Debug.Log($"[LoadPatternCsv] ID={id}, Raw='{raw}', Parsed=[{string.Join(",", seq)}]");

            patternTable[id] = new PatternRow { ID = id, PatternArray = seq };
        }
    }
    static int[] ParsePatternSeq(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<int>();
        string s = raw.Trim().Trim('"').Trim();
        if (s == "-1") return Array.Empty<int>();                 // 특수값 처리
        if (s.StartsWith("(") && s.EndsWith(")"))                  // 괄호 제거
            s = s.Substring(1, s.Length - 2).Trim();

        if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();

        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        List<int> seq = new List<int>();
        foreach (var p in parts)
            if (int.TryParse(p.Trim(), out int v))
                seq.Add(v);
        return seq.ToArray();
    }

    // ---- CSV 파싱 유틸 ----
    static bool IsEmptyRow(string[] row)
    {
        if (row == null || row.Length == 0) return true;
        foreach (var s in row) if (!string.IsNullOrWhiteSpace(s)) return false;
        return true;
    }

    static string Get(string[] arr, int i) => (i >= 0 && i < arr.Length) ? arr[i] : "";

    static int ToInt(string[] arr, int i)
    {
        int v; return int.TryParse(Get(arr, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
    }

    static float ToFloat(string[] arr, int i)
    {
        float v; return float.TryParse(Get(arr, i), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f;
    }

    // "(1,2,3)" → List<int> {1,2,3}
    static List<int> ParseIntTuple(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new List<int>();
        s = s.Trim().Trim('"');
        if (s.StartsWith("(") && s.EndsWith(")")) s = s.Substring(1, s.Length - 2);
        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        List<int> list = new List<int>();
        foreach (var p in parts)
            if (int.TryParse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                list.Add(v);
        return list;
    }

    // 따옴표 보존 CSV 파서
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
