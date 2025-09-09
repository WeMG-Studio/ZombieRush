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
    [SerializeField] GameObject lobbyCanvas;
    [SerializeField] GameObject gameCanvas;
    [SerializeField] GameObject removableSet;


    private void Awake()
    {
        gameStartBtn.onClick.AddListener(GameStartOnClick);
        achievementBtn.onClick.AddListener(AchievementOnClick);
        rankingBtn.onClick.AddListener(RankingOnClick);
    }
    
    private void GameStartOnClick()
    {
        gameCanvas.SetActive(true);
        lobbyCanvas.SetActive(false);
        removableSet.SetActive(false);
        StartCoroutine(GameManager.instance.StartGame());
    }
    private void AchievementOnClick() => achievementPanel.SetActive(true);
    private void RankingOnClick() => rankingPanel.SetActive(true);



}
