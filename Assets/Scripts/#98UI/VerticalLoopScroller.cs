using UnityEngine;

public class VerticalLoopScroller : MonoBehaviour
{
    [Tooltip("같은 스프라이트 조각 2장 이상(위↔아래로 배치)")]
    public Transform[] segments;

    [Header("기본 속도")]
    public float baseSpeed = 2.0f;        // 유휴 상태 속도(유닛/초)
    public bool useUnscaledTime = false;

    [Header("버스트 옵션")]
    public float burstDecay = 3.0f;       // 초당 보너스 감소량
    public float accel = 10f;             // 속도 변화 부드럽게(가속도)

    float segH;
    int count;

    // 내부 상태
    float bonus;           // 일시적인 보너스 속도
    float currentSpeed;    // 실제 적용 속도(부드럽게 추종)

    public void AddBurst(float amount) => bonus += amount;        // 외부에서 호출
    public float CurrentSpeed => currentSpeed;

    void Awake()
    {
        if (segments == null || segments.Length == 0)
        {
            segments = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                segments[i] = transform.GetChild(i);
        }
        count = segments.Length;
        if (count < 2) { enabled = false; return; }

        segH = GetWorldHeight(segments[0]);

        for (int i = 0; i < count; i++)
        {
            var p = segments[i].localPosition;
            p.y = i * segH;
            segments[i].localPosition = p;
        }
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 목표 속도 = 기본 + 보너스
        float target = Mathf.Max(0f, baseSpeed + bonus);
        // 부드럽게 추종(가속)
        currentSpeed = Mathf.Lerp(currentSpeed, target, 1f - Mathf.Exp(-accel * dt));
        // 보너스 감쇠
        bonus = Mathf.MoveTowards(bonus, 0f, burstDecay * dt);

        float dy = currentSpeed * dt;

        for (int i = 0; i < count; i++)
        {
            var p = segments[i].localPosition;
            p.y -= dy;
            if (p.y <= -segH) p.y += segH * count;  // 루프
            segments[i].localPosition = p;
        }
    }

    float GetWorldHeight(Transform t)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr && sr.sprite) return sr.bounds.size.y;
        var rt = t as RectTransform;
        if (rt) return rt.rect.height * rt.lossyScale.y;
        return 1f;
    }
}
