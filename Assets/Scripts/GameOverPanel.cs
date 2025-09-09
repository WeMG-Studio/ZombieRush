using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] Button retryBtn,toLobbyBtn;
    [SerializeField] TextMeshProUGUI highScoreText;
    [SerializeField] TextMeshProUGUI highScoreAlarmText;
    [SerializeField] TextMeshProUGUI scoreText;

    private void Awake()
    {
        if (retryBtn) retryBtn.onClick.AddListener(() => RetryOnClick());
        if (toLobbyBtn) toLobbyBtn.onClick.AddListener(() => ToLobbyOnClick());
    }
    public void UpdateUI(int _score)
    {
        scoreText.text = _score.ToString();
        int highScore = PlayerPrefs.GetInt("HighScore");
        if(highScore < _score)
        {
            highScoreText.text = _score.ToString();
            highScoreAlarmText.gameObject.SetActive(true);
        }else highScoreAlarmText.gameObject.SetActive(false);
    }
    private void RetryOnClick()
    {
        // 초기화 후 게임 시작
        StartCoroutine(GameManager.instance.RetryGame());
    }
    private void ToLobbyOnClick()
    {
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }


}
