using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public GameManager game;

    [Header("UI")]
    public Slider distanceBar;
    public TMP_Text levelText;
    public TMP_Text stepsText;

    [Header("FX")]
    public GaugeFX gaugeFx;            // ������ ���� ���� ������Ʈ(�Ʒ� ����)

    void OnEnable()
    {
        if (!game) return;
        game.OnDistanceNormalized += OnDistance;
        game.OnLevelChanged += OnLevel;
        game.OnStepsChanged += OnSteps;
        game.OnDied += OnDied;
    }

    void OnDisable()
    {
        if (!game) return;
        game.OnDistanceNormalized -= OnDistance;
        game.OnLevelChanged -= OnLevel;
        game.OnStepsChanged -= OnSteps;
        game.OnDied -= OnDied;
    }

    void OnDistance(float t)
    {
        if (distanceBar) distanceBar.value = t;
        if (gaugeFx) gaugeFx.SetNormalized(t); // �ִϸ��̼��� GaugeFX���� ó��
    }

    void OnLevel(int lv) { if (levelText) levelText.text = $"Lv {lv}"; }
    void OnSteps(int s) { if (stepsText) stepsText.text = $"{s} steps"; }

    void OnDied(string _) { /* �ʿ��ϸ� ���ӿ��� �г� �����ֱ� */ }
}