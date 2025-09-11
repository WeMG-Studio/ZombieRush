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
    [SerializeField] CanvasGroup lobbyGroup; // ��ü �κ� ���̵�� (�ɼ�)
    [SerializeField] GameObject gameCanvas;

    private void Awake()
    {
        gameStartBtn.onClick.AddListener(GameStartOnClick);
    }

    private void GameStartOnClick()
    {
        // �ִϸ��̼� ������ ����
        Debug.Log("StartClick");
        StartCoroutine(PlayLobbyOutAnimation());
    }

    private IEnumerator PlayLobbyOutAnimation()
    {
        // DOTween ���� �� ����
        float duration = 0.5f;

        foreach(RectTransform rectTs in topPanels)
        {
            rectTs.DOAnchorPosY(rectTs.anchoredPosition.y + 700f, duration);
        }
        foreach (RectTransform rectTs in bottomPanels)
        {
            rectTs.DOAnchorPosY(rectTs.anchoredPosition.y - 700f, duration);
        }

        // ���̵� �ƿ� �ɼ�
        if (lobbyGroup) lobbyGroup.DOFade(0, duration);
        yield return new WaitForSeconds(duration);

        // ���� ���� ����
        StartCoroutine(GameManager.instance.StartGame());
        gameCanvas.SetActive(true);
    }
}
