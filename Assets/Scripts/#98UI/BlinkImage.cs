using UnityEngine;
using UnityEngine.UI;

public class BlinkImage : MonoBehaviour
{
    public Image targetImage;   // 효과 적용할 이미지
    public float delay = 1f;    // 씬 로드 후 몇 초 뒤 시작할지
    public float duration = 2f; // 몇 초 동안 서서히 보이게 할지

    private float timer;
    private bool started = false;

    void Start()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();

        // 처음엔 안 보이게
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
            timer = 0f; // fade in 시작
        }

        if (started && timer <= duration)
        {
            float alpha = Mathf.Clamp01(timer / duration); // 0 → 1
            Color c = targetImage.color;
            c.a = alpha;
            targetImage.color = c;
        }
    }
}
