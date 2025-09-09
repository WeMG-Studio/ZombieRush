using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] GameOverPanel gameOverPanel;

    [Header("Characters")]
    public Player player;

    [Header("Core Refs")]
    public GameParams config;
    public RailManager rail;

    public Button btnAdvance;
    public Button btnFix;
    // 상태
    [SerializeField] TextMeshProUGUI gameStartCountText;
    public bool isGameStart = false;
    float distance;
    int level;
    bool isDead;

    [Header("Wall Scroll")]
    public VerticalLoopScroller[] wallScrollers; 
    public float scrollBurstBase = 2.5f;         
    public float scrollBurstPerLevel = 0.1f;

    // 외부에 노출(읽기 전용)
    public float Distance => distance;        // 0 ~ maxDistance
    public int Level => level;
    public bool IsDead => isDead;

    // 이벤트: HUD/연출/적 AI 등이 구독
    public event Action<float> OnDistanceNormalized; // 0~1
    public event Action<int> OnLevelChanged;
    public event Action<int> OnStepsChanged;
    public event Action<string> OnDied;

    void Awake()
    {
        if(instance == null) instance = this;

        if (btnAdvance) btnAdvance.onClick.AddListener(() => Advance());
        if (btnFix) btnFix.onClick.AddListener(() => FixAndAdvance());
        distance = config.maxDistance;
        EmitAll();
    }

    void Update()
    {
        //keyboard input test
        if (isGameStart)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) Advance();
            if (Input.GetKeyDown(KeyCode.Space)) FixAndAdvance();


            if (isDead) return;

            // 지속 감소(좀비 접근)
            float decay = Mathf.Min(config.baseDecay + level * config.decayPerLevel, config.maxDecay);
            var prev = distance;
            distance -= decay * Time.deltaTime;

            if (distance <= 0f)
            {
                distance = 0f;
                Die("좀비에게 잡혔어!");
            }

            if (!Mathf.Approximately(prev, distance))
                EmitDistance();
        }
        
    }
    public IEnumerator StartGame()
    {
        Debug.Log("Start Game");
        //todo : 3,2,1하고 게임 스타트
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
        Debug.Log(isGameStart);
        yield return null;
    }
    public void InitGame()
    {
        gameOverPanel.gameObject.SetActive(false);
        isDead = false;                         // ★ 반드시 false로
        isGameStart = false;                    // 카운트다운 전에 잠깐 막기

        // 진행도 리셋
        distance = config.maxDistance;          // ★ 다시 풀로 세팅
        level = 0;
        rail.InitRail();

        // 레일/플레이어 상태 초기화(필요 시)
        // player.ResetState(); // ← 플레이어 이펙트/애니/체력 등 초기화 필요 시

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

    // 우측 버튼: 전진
    public void Advance()
    {
        if (!isGameStart) return;
        if (isDead) return;

        if (!rail.TryAdvance())
        {
            Die("비직선 레일에서 전진!");
            return;
        }

        HealOnStep();
        LevelRecalc();
        EmitAll();
        ApplyScrollBurst();
        if (player) player.PlayStepBounce(1f);
    }

    // 좌측 버튼: 교정 + 전진 (직선에서 교정하면 즉사)
    public void FixAndAdvance()
    {
        if (!isGameStart) return;
        if (isDead) return;
        if (rail.CurrentTile == null) return;

        if (rail.CurrentTile.Type == RailType.Straight)
        {
            Die("직선에서 교정 시 패배!");
            return;
        }

        rail.FixCurrent();

        if (!rail.TryAdvance())
        {
            Die("교정 후 전진 실패!");
            return;
        }

        HealOnStep();
        LevelRecalc();
        EmitAll();
        ApplyScrollBurst();
        if (player) player.PlayStepBounce(1f);
    }

    void HealOnStep()
    {
        distance = Mathf.Min(config.maxDistance, distance + config.gainPerStep);
    }

    void LevelRecalc()
    {
        level = rail.Steps / config.stepsPerLevel;
    }

    void Die(string reason)
    {
        if (isDead) return;
        isDead = true;
        OnDied?.Invoke(reason);
        StartCoroutine(FailFx());
        Debug.Log($"DEAD: {reason}");
        //Todo : die Panel 띄우기
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

    // ---------- 이벤트 발행 ----------
    void EmitDistance() => OnDistanceNormalized?.Invoke(distance / config.maxDistance);
    void EmitLevel() => OnLevelChanged?.Invoke(level);
    void EmitSteps() => OnStepsChanged?.Invoke(rail.Steps);
    void EmitAll() { EmitDistance(); EmitLevel(); EmitSteps(); }
}