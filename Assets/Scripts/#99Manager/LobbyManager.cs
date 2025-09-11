using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class LobbyManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Button gameStartBtn;

    [Header("Anim Targets")]
    [SerializeField] RectTransform[] topPanels;
    [SerializeField] RectTransform[] bottomPanels;
    [SerializeField] CanvasGroup lobbyGroup; // 전체 로비 페이드용 (옵션)
    [SerializeField] GameObject gameCanvas;

    private void Awake()
    {
        gameStartBtn.onClick.AddListener(GameStartOnClick);
    }

    private void GameStartOnClick()
    {
        // 애니메이션 시퀀스 실행
        Debug.Log("StartClick");
        StartCoroutine(PlayLobbyOutAnimation());
    }

    private IEnumerator PlayLobbyOutAnimation()
    {
        // DOTween 쓰면 더 간단
        float duration = 0.5f;

        foreach(RectTransform rectTs in topPanels)
        {
            rectTs.DOAnchorPosY(rectTs.anchoredPosition.y + 700f, duration);
        }
        foreach (RectTransform rectTs in bottomPanels)
        {
            rectTs.DOAnchorPosY(rectTs.anchoredPosition.y - 700f, duration);
        }

        // 페이드 아웃 옵션
        if (lobbyGroup) lobbyGroup.DOFade(0, duration);
        yield return new WaitForSeconds(duration);

        // 이제 게임 시작
        StartCoroutine(GameManager.instance.StartGame());
        gameCanvas.SetActive(true);
    }
}
