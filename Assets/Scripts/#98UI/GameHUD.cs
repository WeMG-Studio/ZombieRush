using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public GameManager game;

    [Header("UI")]
    public Slider distanceBar;
    public TMP_Text levelText;
    public TMP_Text stepsText;

    [Header("FX")]
    public GaugeFX gaugeFx;            // 게이지 연출 전용 컴포넌트(아래 파일)

    void OnEnable()
    {
        if (!game) return;
        game.OnDistanceNormalized += OnDistance;
        game.OnLevelChanged += OnLevel;
        game.OnStepsChanged += OnSteps;
        game.OnDied += OnDied;

        // 초기 1회 강제 반영
        float init = (game.isGameStart && game.config.maxDistance > 0f)
       ? Mathf.Clamp01(game.Distance / game.config.maxDistance)
       : 1f;
        OnDistance(init);
    }

    void OnDisable()
    {
        if (!game) return;
        game.OnDistanceNormalized -= OnDistance;
        game.OnLevelChanged -= OnLevel;
        game.OnStepsChanged -= OnSteps;
        game.OnDied -= OnDied;
    }
    void TryBindGame()
    {
        if (game == null)
            game = GameManager.instance ?? FindObjectOfType<GameManager>();
    }

    void OnDistance(float t)
    {
        if (distanceBar) distanceBar.value = t;
        if (gaugeFx) gaugeFx.SetNormalized(t); // 애니메이션은 GaugeFX에서 처리
    }

    void OnLevel(int lv) { if (levelText) levelText.text = $"Lv {lv}"; }
    void OnSteps(int s) { if (stepsText) stepsText.text = $"{s} steps"; }

    void OnDied(string _) { /* 필요하면 게임오버 패널 보여주기 */ }
}