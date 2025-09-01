using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public GameParams config;
    public RailManager rail;
    public Button btnAdvance;
    public Button btnFix;
    public Slider distanceBar;
    public TMP_Text levelText;
    public TMP_Text stepsText;

    float distance;    // 현재 거리 (0~maxDistance)
    int level;
    bool isDead;

    void Awake()
    {
        distance = config.maxDistance;
        UpdateUI();
        btnAdvance.onClick.AddListener(OnAdvance);
        btnFix.onClick.AddListener(OnFix);
    }

    void Update()
    {
        if (isDead) return;

        // 초당 감소량 = base + level*k (상한 캡)
        float decay = Mathf.Min(config.baseDecay + level * config.decayPerLevel, config.maxDecay);
        distance -= decay * Time.deltaTime;
        if (distance <= 0f)
        {
            distance = 0f;
            Die("좀비에게 잡혔어!");
        }
        UpdateUI();

        //KeyboardTest
        if(Input.GetKeyDown(KeyCode.Space)) OnFix();
        if(Input.GetKeyDown(KeyCode.UpArrow)) OnAdvance();

    }
    void OnFix()
    {
        if (isDead) return;
        if (rail.CurrentTile == null) return;

        // 1) 현재 칸이 이미 '직선'이면 즉시 패배
        if (rail.CurrentTile.Type == RailType.Straight)
        {
            Die("직선에서 교정 시 패배!");
            return;
        }

        // 2) 굽은 칸이면 교정 후 전진
        rail.FixCurrent();

        if (!rail.TryAdvance())
        {
            // 이론상 여기 도달하기 어렵지만 안전망 유지
            Die("교정 후 전진 실패!");
            return;
        }

        // 3) 전진 성공 보상 및 레벨/UI 갱신
        distance = Mathf.Min(config.maxDistance, distance + config.gainPerStep);
        level = rail.Steps / config.stepsPerLevel;  
    }
    void OnAdvance()
    {
        if (isDead) return;

        if (!rail.TryAdvance())
        {
            Die("비직선 레일에서 전진!");
            return;
        }
        // 성공 보상: 거리 회복
        distance = Mathf.Min(config.maxDistance, distance + config.gainPerStep);

        // 레벨 갱신
        level = rail.Steps / config.stepsPerLevel;

        UpdateUI();
    }
    void UpdateUI()
    {
        if (distanceBar) distanceBar.value = distance / config.maxDistance;
        if (levelText) levelText.text = $"Lv {level}";
        if (stepsText) stepsText.text = $"{rail.Steps} steps";
    }

    void Die(string reason)
    {
        isDead = true;
        StartCoroutine(FailFx());
        Debug.Log($"DEAD: {reason}");
        // 여기서 리트라이 패널/점수 기록 호출
    }

    IEnumerator FailFx()
    {
        float original = Time.timeScale;
        Time.timeScale = config.failSlowmo;
        yield return new WaitForSecondsRealtime(config.failSlowmoTime);
        Time.timeScale = original;
    }
}