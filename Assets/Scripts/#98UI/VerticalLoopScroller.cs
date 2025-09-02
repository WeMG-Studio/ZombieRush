using UnityEngine;

public class VerticalLoopScroller : MonoBehaviour
{
    [Tooltip("���� ��������Ʈ ���� 2�� �̻�(����Ʒ��� ��ġ)")]
    public Transform[] segments;

    [Header("�⺻ �ӵ�")]
    public float baseSpeed = 2.0f;        // ���� ���� �ӵ�(����/��)
    public bool useUnscaledTime = false;

    [Header("����Ʈ �ɼ�")]
    public float burstDecay = 3.0f;       // �ʴ� ���ʽ� ���ҷ�
    public float accel = 10f;             // �ӵ� ��ȭ �ε巴��(���ӵ�)

    float segH;
    int count;

    // ���� ����
    float bonus;           // �Ͻ����� ���ʽ� �ӵ�
    float currentSpeed;    // ���� ���� �ӵ�(�ε巴�� ����)

    public void AddBurst(float amount) => bonus += amount;        // �ܺο��� ȣ��
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

        // ��ǥ �ӵ� = �⺻ + ���ʽ�
        float target = Mathf.Max(0f, baseSpeed + bonus);
        // �ε巴�� ����(����)
        currentSpeed = Mathf.Lerp(currentSpeed, target, 1f - Mathf.Exp(-accel * dt));
        // ���ʽ� ����
        bonus = Mathf.MoveTowards(bonus, 0f, burstDecay * dt);

        float dy = currentSpeed * dt;

        for (int i = 0; i < count; i++)
        {
            var p = segments[i].localPosition;
            p.y -= dy;
            if (p.y <= -segH) p.y += segH * count;  // ����
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
