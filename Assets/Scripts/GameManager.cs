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

    float distance;    // ���� �Ÿ� (0~maxDistance)
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

        // �ʴ� ���ҷ� = base + level*k (���� ĸ)
        float decay = Mathf.Min(config.baseDecay + level * config.decayPerLevel, config.maxDecay);
        distance -= decay * Time.deltaTime;
        if (distance <= 0f)
        {
            distance = 0f;
            Die("���񿡰� ������!");
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

        // 1) ���� ĭ�� �̹� '����'�̸� ��� �й�
        if (rail.CurrentTile.Type == RailType.Straight)
        {
            Die("�������� ���� �� �й�!");
            return;
        }

        // 2) ���� ĭ�̸� ���� �� ����
        rail.FixCurrent();

        if (!rail.TryAdvance())
        {
            // �̷л� ���� �����ϱ� ������� ������ ����
            Die("���� �� ���� ����!");
            return;
        }

        // 3) ���� ���� ���� �� ����/UI ����
        distance = Mathf.Min(config.maxDistance, distance + config.gainPerStep);
        level = rail.Steps / config.stepsPerLevel;  
    }
    void OnAdvance()
    {
        if (isDead) return;

        if (!rail.TryAdvance())
        {
            Die("������ ���Ͽ��� ����!");
            return;
        }
        // ���� ����: �Ÿ� ȸ��
        distance = Mathf.Min(config.maxDistance, distance + config.gainPerStep);

        // ���� ����
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
        // ���⼭ ��Ʈ���� �г�/���� ��� ȣ��
    }

    IEnumerator FailFx()
    {
        float original = Time.timeScale;
        Time.timeScale = config.failSlowmo;
        yield return new WaitForSecondsRealtime(config.failSlowmoTime);
        Time.timeScale = original;
    }
}