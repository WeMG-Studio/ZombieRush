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
    // ����
    [SerializeField] TextMeshProUGUI gameStartCountText;
    public bool isGameStart = false;
    float distance;
    int level;
    bool isDead;

    [Header("Wall Scroll")]
    public VerticalLoopScroller[] wallScrollers; 
    public float scrollBurstBase = 2.5f;         
    public float scrollBurstPerLevel = 0.1f;

    // �ܺο� ����(�б� ����)
    public float Distance => distance;        // 0 ~ maxDistance
    public int Level => level;
    public bool IsDead => isDead;

    // �̺�Ʈ: HUD/����/�� AI ���� ����
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

            // ���� ����(���� ����)
            float decay = Mathf.Min(config.baseDecay + level * config.decayPerLevel, config.maxDecay);
            var prev = distance;
            distance -= decay * Time.deltaTime;

            if (distance <= 0f)
            {
                distance = 0f;
                Die("���񿡰� ������!");
            }

            if (!Mathf.Approximately(prev, distance))
                EmitDistance();
        }
        
    }
    public IEnumerator StartGame()
    {
        Debug.Log("Start Game");
        //todo : 3,2,1�ϰ� ���� ��ŸƮ
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
        isDead = false;                         // �� �ݵ�� false��
        isGameStart = false;                    // ī��Ʈ�ٿ� ���� ��� ����

        // ���൵ ����
        distance = config.maxDistance;          // �� �ٽ� Ǯ�� ����
        level = 0;
        rail.InitRail();

        // ����/�÷��̾� ���� �ʱ�ȭ(�ʿ� ��)
        // player.ResetState(); // �� �÷��̾� ����Ʈ/�ִ�/ü�� �� �ʱ�ȭ �ʿ� ��

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

    // ���� ��ư: ����
    public void Advance()
    {
        if (!isGameStart) return;
        if (isDead) return;

        if (!rail.TryAdvance())
        {
            Die("������ ���Ͽ��� ����!");
            return;
        }

        HealOnStep();
        LevelRecalc();
        EmitAll();
        ApplyScrollBurst();
        if (player) player.PlayStepBounce(1f);
    }

    // ���� ��ư: ���� + ���� (�������� �����ϸ� ���)
    public void FixAndAdvance()
    {
        if (!isGameStart) return;
        if (isDead) return;
        if (rail.CurrentTile == null) return;

        if (rail.CurrentTile.Type == RailType.Straight)
        {
            Die("�������� ���� �� �й�!");
            return;
        }

        rail.FixCurrent();

        if (!rail.TryAdvance())
        {
            Die("���� �� ���� ����!");
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
        //Todo : die Panel ����
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

    // ---------- �̺�Ʈ ���� ----------
    void EmitDistance() => OnDistanceNormalized?.Invoke(distance / config.maxDistance);
    void EmitLevel() => OnLevelChanged?.Invoke(level);
    void EmitSteps() => OnStepsChanged?.Invoke(rail.Steps);
    void EmitAll() { EmitDistance(); EmitLevel(); EmitSteps(); }
}