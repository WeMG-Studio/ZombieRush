using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameContinuePanel : MonoBehaviour
{
    [SerializeField] GameOverPanel gameOverPanel;
    [SerializeField] TextMeshProUGUI countDownText;

    bool _cancelled = false;
    bool _isCounting = false;

    void OnEnable()
    {
        _cancelled = false;
        _isCounting = false;
    }

    public IEnumerator CountDown(int _score)
    {
        GameManager.instance.isContinued = true;
        _isCounting = true;
        _cancelled = false;

        for (int i = 3; i >= 1; i--)
        {
            if (_cancelled) { _isCounting = false; yield break; }
            countDownText.text = i.ToString();
            // WaitForSecondsRealtime로 타임스케일 무시
            yield return new WaitForSecondsRealtime(1f);
        }

        if (_cancelled) { _isCounting = false; yield break; }

        gameOverPanel.gameObject.SetActive(true);
        gameOverPanel.UpdateUI(_score);
        this.gameObject.SetActive(false);
        _isCounting = false;
    }

    public void ContinueOnClick()
    {
        _cancelled = true;

        if (_isCounting) StopAllCoroutines();

        Debug.Log("리워드 광고 시청");
        GoogleAdMob.instance.ContinueRewardAds();

        this.gameObject.SetActive(false);
    }
}
