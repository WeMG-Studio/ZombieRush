using UnityEngine;
using UnityEngine.UI;

public class GaugeFX : MonoBehaviour
{
    [Header("Bindings")]
    public Image fill;                 // Slider�� Fill Image
    public RectTransform barRect;      // Slider�� RectTransform
    public CanvasGroup dangerFlash;    // ȭ�� ���� �÷���(�ɼ�)

    [Header("Colors")]
    public Color gaugeHigh = new Color32(0x2A, 0xD3, 0x6E, 255);
    public Color gaugeMid = new Color32(0xFF, 0xC1, 0x3B, 255);
    public Color gaugeLow = new Color32(0xE6, 0x3B, 0x2E, 255);

    [Header("Thresholds & Motion")]
    [Range(0f, 1f)] public float warnThreshold = 0.35f;
    [Range(0f, 1f)] public float dangerThreshold = 0.15f;
    public float pulseSpeed = 6f;
    public float pulseScale = 1.06f;
    public float shakeAmpPx = 6f;
    public float shakeFreq = 18f;

    Vector2 basePos;
    Vector3 baseScale;
    float tNorm; // 0~1, GameHUD�� �־���

    void Awake()
    {
        if (barRect)
        {
            basePos = barRect.anchoredPosition;
            baseScale = barRect.localScale;
        }
    }

    public void SetNormalized(float t)
    {
        tNorm = Mathf.Clamp01(t);

        // �� ����(�� ���� ����)
        if (fill)
        {
            Color c = (tNorm >= 0.5f)
                ? Color.Lerp(gaugeMid, gaugeHigh, (tNorm - 0.5f) / 0.5f)
                : Color.Lerp(gaugeLow, gaugeMid, tNorm / 0.5f);
            fill.color = c;
        }
    }
    void OnEnable()
    {
        if (dangerFlash) dangerFlash.alpha = 0f;
        if (barRect)
        {
            barRect.localScale = baseScale;
            barRect.anchoredPosition = basePos;
        }
    }

    void Update()
    {
        if (!GameManager.instance.isGameStart) return;
        // �޽�/��鸲/�÷��ô� �ǽð� �ִϸ��̼� �ʿ� �� �� ������ ó��
        if (!barRect) return;

        if (tNorm < warnThreshold)
        {
            // �޽�
            float p = (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f);
            float s = Mathf.Lerp(1f, pulseScale, p);
            barRect.localScale = baseScale * s;

            // ���� ��鸲
            if (tNorm < dangerThreshold)
            {
                float k = Mathf.InverseLerp(dangerThreshold, 0f, tNorm);
                float amp = shakeAmpPx * k;
                float offX = (Mathf.PerlinNoise(Time.unscaledTime * shakeFreq, 0f) - 0.5f) * 2f * amp;
                float offY = (Mathf.PerlinNoise(0f, Time.unscaledTime * shakeFreq) - 0.5f) * 2f * amp;
                barRect.anchoredPosition = basePos + new Vector2(offX, offY);
            }
            else
            {
                barRect.anchoredPosition = basePos;
            }
        }
        else
        {
            barRect.localScale = baseScale;
            barRect.anchoredPosition = basePos;
        }

        if (dangerFlash)
        {
            if (tNorm < dangerThreshold)
            {
                float k = Mathf.InverseLerp(dangerThreshold, 0f, tNorm);
                dangerFlash.alpha = Mathf.PingPong(Time.unscaledTime * 4f, 1f) * 0.35f * k;
            }
            else
            {
                dangerFlash.alpha = 0f;
            }
        }
    }
}