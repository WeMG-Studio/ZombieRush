using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Button gameStartBtn;
    [SerializeField] Button achievementBtn;
    [SerializeField] Button rankingBtn;


    [SerializeField] GameObject achievementPanel;
    [SerializeField] GameObject rankingPanel;


    private void Awake()
    {
        gameStartBtn.onClick.AddListener(GameStartOnClick);
        achievementBtn.onClick.AddListener(AchievementOnClick);
        rankingBtn.onClick.AddListener(RankingOnClick);
    }
    
    private void GameStartOnClick() => SceneManager.LoadScene("GameScene");
    private void AchievementOnClick() => achievementPanel.SetActive(true);
    private void RankingOnClick() => rankingPanel.SetActive(true);



}
