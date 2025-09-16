using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSceneBinder : MonoBehaviour
{
    public GameOverPanel gameOverPanel;
    public GameContinuePanel gameContinuePanel;
    public Player player;
    public GameParams config;
    public RailManager rail;

    public Button btnAdvance;
    public Button btnFix;
    public TextMeshProUGUI gameStartCountText;

    public VerticalLoopScroller[] wallScrollers;
    public GameHUD gameHUD;
}
