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
    public GameParams config;       // maxDistance / gainPerStep / stepsPerLevel / failSlowmo � ���
    public RailManager rail;

    public Button btnAdvance;
    public Button btnFix;

    [SerializeField] TextMeshProUGUI gameStartCountText;
    public bool isGameStart = false;

    float distance;
    int level;           // ���� ����(= Level.csv�� ID)
    bool isDead;

    [Header("Wall Scroll")]
    public VerticalLoopScroller[] wallScrollers;
    public float scrollBurstBase = 2.5f;
    public float scrollBurstPerLevel = 0.1f;

    // �ܺ� ����
    public float Distance => distance;
    public int Level => level;
    public bool IsDead => isDead;

    public event Action<float> OnDistanceNormalized; // 0~1
    public event Action<int> OnLevelChanged;
    public event Action<int> OnStepsChanged;
    public event Action<string> OnDied;

    [SerializeField] string gameSceneName = "GameScene";
     GameHUD gameHUD;
    // ---------------- CSV �ε�/���� ----------------
    [Serializable]
    public class LevelRow
    {
        public int ID;
        public int Score;
        public float Decay;           // �ʴ� ���ҷ�
        public int Straight_MIN;
        public int Straight_MAX;
        public List<int> PatternIDs;  // PatternID_Array
    }

    [Serializable]
    public class PatternRow
    {
        public int ID;
        public int[] PatternArray;    // 0=ȸ��, 1=����
    }

    Dictionary<int, LevelRow> levelTable = new Dictionary<int, LevelRow>();
    Dictionary<int, PatternRow> patternTable = new Dictionary<int, PatternRow>();
    System.Random rng = new System.Random();

    // ���� ���� ���� ����
    int pendingStraightLeft = 0;         // ���� "������ ����" ��
    int[] pendingPattern = null;         // �̾ ������ ����(0/1)
    int patternIndex = 0;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        //DontDestroyOnLoad(gameObject);

        // ��ư �ڵ鷯
        if (btnAdvance) btnAdvance.onClick.AddListener(() => Advance());
        if (btnFix) btnFix.onClick.AddListener(() => FixAndAdvance());

        // CSV �ε�
        LoadLevelCsv();   // csv/Level.csv
        LoadPatternCsv(); // csv/Pattern.csv

        // �⺻ ����
        distance = config.maxDistance;   // GameParams�� ������ ���� �״�� ��� (maxDistance ��). :contentReference[oaicite:3]{index=3}
        EmitAll();
    }

    void Update()
    {
        if (!isGameStart) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) Advance();
        if (Input.GetKeyDown(KeyCode.Space)) FixAndAdvance();

        if (isDead) return;

        // --- decay�� CSV�� Decay�� ��ü ---
        float decay = GetDecayForLevel(level); // ����=ID ����
        float prev = distance;
        distance -= decay * Time.deltaTime;    // �ʴ� ���ҷ� ���� (CSV). :contentReference[oaicite:4]{index=4}

        if (distance <= 0f)
        {
            distance = 0f;
            Die("���񿡰� ������!");
        }

        if (!Mathf.Approximately(prev, distance))
            EmitDistance();
    }
    void OnEnable()
    {
        // DDOL�� ��Ʈ���� ��. (Ȥ�� �θ� ������ ����)
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
            InitGame();             // �� ���⼭ distance = config.maxDistance �� ����
            EmitAll();              // �� HUD ���� �ʱⰪ�� Ȯ���� �ް� ��ε�ĳ��Ʈ
        }
    }

    void RebindFromScene()
    {
        // 1) ������ ���δ� ã��
        var binder = FindObjectOfType<GameSceneBinder>();
        if (!binder)
        {
            Debug.LogWarning("GameSceneBinder�� ã�� ����(�� ������ GameManager ���� ���ʿ��� ����� �� ����).");
            return;
        }

        // 2) ���� ��ư ������ ���� (�ߺ� ȣ�� ����)
        btnAdvance?.onClick.RemoveAllListeners();
        btnFix?.onClick.RemoveAllListeners();

        // 3) �� ������ ��� ����ε�
        gameOverPanel = binder.gameOverPanel;
        player = binder.player;
        config = binder.config;
        rail = binder.rail;
        btnAdvance = binder.btnAdvance;
        btnFix = binder.btnFix;
        gameStartCountText = binder.gameStartCountText;
        wallScrollers = binder.wallScrollers;
        binder.gameHUD.game = this;

        // 4) ��ư ������ �ٽ� ����
        if (btnAdvance) btnAdvance.onClick.AddListener(Advance);
        if (btnFix) btnFix.onClick.AddListener(FixAndAdvance);

        // 5) �� ���� �ʱ�ȭ�� �ʿ��ϸ� ���⼭
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

        // ù ���� �غ�: Straight �������� ����
        PrepareNextSegment();

        yield return null;
    }

    public void InitGame()
    {
        gameOverPanel.gameObject.SetActive(false);
        isDead = false;
        isGameStart = false;

        distance = config.maxDistance;   // Ǯ ����. :contentReference[oaicite:5]{index=5}
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

    // ---------------- ���� ��ư�� ----------------

    // ���� ��ư: ����
    public void Advance()
    {
        if (!isGameStart || isDead) return;

        // ���� Ÿ���� �����̾�߸� ���� ����
        if (rail.CurrentTile.Type != RailType.Straight)
        {
            Die("������ ���Ͽ��� ����!");
            return;
        }

        // ���� ������ ProgressSegmentState()���� ó��
        OnAdvancedOneStep();
    }

    // ���� ��ư: ���� + ���� (�������� �����ϸ� ���)
    public void FixAndAdvance()
    {
        if (!isGameStart || isDead) return;
        if (rail.CurrentTile == null) return;

        if (rail.CurrentTile.Type == RailType.Straight)
        {
            Die("�������� ���� �� �й�!");
            return;
        }

        rail.FixCurrent();

        // ���� ������ ProgressSegmentState()���� ó��
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
        // rail.Steps�� �̿��� ���� ���(=ID). score=rail.Steps�� ����. :contentReference[oaicite:8]{index=8}
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
    // =============== CSV ��� ���� ���׸�Ʈ ���� ===============
    // Straight ����(������ ����) �� Pattern ����(0/1 ������) �� �ݺ�
    // ===========================================================

    void PrepareNextSegment()
    {
        var lv = GetLevelRow(level);

        pendingStraightLeft = UnityEngine.Random.Range(lv.Straight_MIN, lv.Straight_MAX + 1);

        // ���� ��õ�(�ִ� 5ȸ) ? ����ִ� ������ ����
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
            int cmd = pendingPattern[patternIndex++]; // 0=ȸ��, 1=����

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

    float GetDecayForLevel(int lv)
    {
        var row = GetLevelRow(lv);
        return row.Decay;
    }

    LevelRow GetLevelRow(int lv)
    {
        if (levelTable.TryGetValue(lv, out var row))
            return row;

        // ���� ���� ID�� fallback
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

        Debug.LogError("Level CSV�� �������!");
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
        if (csv == null) { Debug.LogError("csv/Level.csv �� ã�� �� ����"); return; }

        var rows = ParseCsv(csv.text);
        if (rows.Count == 0) return;

        // ���
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
            item.Score = ToInt(r, idxScore);   // �����: ���� ������ rail.Steps
            item.Decay = ToFloat(r, idxDecay);
            item.Straight_MIN = ToInt(r, idxStraightMin);
            item.Straight_MAX = ToInt(r, idxStraightMax);
            item.PatternIDs = ParseIntTuple(Get(r, idxPatternArr)); // "(1,2,3)" �� [1,2,3]

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

            // �����: �ε��� ������ �������� Ȯ��
            Debug.Log($"[LoadPatternCsv] ID={id}, Raw='{raw}', Parsed=[{string.Join(",", seq)}]");

            patternTable[id] = new PatternRow { ID = id, PatternArray = seq };
        }
    }
    static int[] ParsePatternSeq(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<int>();
        string s = raw.Trim().Trim('"').Trim();
        if (s == "-1") return Array.Empty<int>();                 // Ư���� ó��
        if (s.StartsWith("(") && s.EndsWith(")"))                  // ��ȣ ����
            s = s.Substring(1, s.Length - 2).Trim();

        if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();

        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        List<int> seq = new List<int>();
        foreach (var p in parts)
            if (int.TryParse(p.Trim(), out int v))
                seq.Add(v);
        return seq.ToArray();
    }

    // ---- CSV �Ľ� ��ƿ ----
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

    // "(1,2,3)" �� List<int> {1,2,3}
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

    // ����ǥ ���� CSV �ļ�
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
