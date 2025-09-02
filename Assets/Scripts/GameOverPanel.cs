using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] Button retryBtn,toLobbyBtn;
    [SerializeField] TextMeshProUGUI highScoreText;

    private void Awake()
    {
        if (retryBtn) retryBtn.onClick.AddListener(() => RetryOnClick());
        if (toLobbyBtn) toLobbyBtn.onClick.AddListener(() => ToLobbyOnClick());
    }
    public void UpdateUI(int _score)
    {
        highScoreText.text = _score.ToString();

    }
    private void RetryOnClick()
    {

    }
    private void ToLobbyOnClick()
    {

    }


}
