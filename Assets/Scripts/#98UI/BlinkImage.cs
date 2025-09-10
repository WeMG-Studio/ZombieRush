using UnityEngine;
using UnityEngine.UI;

public class BlinkImage : MonoBehaviour
{
    public Image targetImage;   // ȿ�� ������ �̹���
    public float delay = 1f;    // �� �ε� �� �� �� �� ��������
    public float duration = 2f; // �� �� ���� ������ ���̰� ����

    private float timer;
    private bool started = false;

    void Start()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();

        // ó���� �� ���̰�
        Color c = targetImage.color;
        c.a = 0f;
        targetImage.color = c;

        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!started && timer >= delay)
        {
            started = true;
            timer = 0f; // fade in ����
        }

        if (started && timer <= duration)
        {
            float alpha = Mathf.Clamp01(timer / duration); // 0 �� 1
            Color c = targetImage.color;
            c.a = alpha;
            targetImage.color = c;
        }
    }
}
