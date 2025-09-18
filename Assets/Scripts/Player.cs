using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Step Sway")]
    public float amplitude = 0.06f;     // �¿� ��鸲 ��(���� ����)
    public float duration = 0.12f;      // ��ü ����
    public int oscillations = 2;        // ���� Ƚ��(�պ� ��)
    public float tiltDegrees = 6f;      // ��鸲�� ���� Z ȸ��(��)
    public bool useUnscaledTime = true; // ���θ� ����

    [Header("Punch (Squash & Stretch)")]
    [Tooltip("���� Ȯ�� ����(0.06 = 6%)")]
    public float punchHorizontal = 0.06f;
    [Tooltip("���� ���� ����(0.08 = 8%)")]
    public float punchVertical = 0.08f;

    Vector3 _baseLocalPos;
    Quaternion _baseLocalRot;
    Vector3 _baseLocalScale;
    Coroutine _co;

    void Awake() => SnapBaseline();
    void OnDisable() => ResetPose();

    /// ���� ���� ����(�����Ϳ��� ��ġ/������ �ٲ����� �� �� ȣ��)
    public void SnapBaseline()
    {
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;
        _baseLocalScale = transform.localScale;
    }

    /// ����/���� ���� �� ȣ��. strength�� ���� ����(1 = �⺻)
    public void PlayStepBounce(float strength = 1f)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoStepBounce(Mathf.Max(0.01f, strength)));
    }

    IEnumerator CoStepBounce(float strength)
    {
        float amp = amplitude * strength;
        float dur = Mathf.Max(0.01f, duration);
        float t = 0f;
        float omega = oscillations * Mathf.PI * 2f; // 2�� * ������

        while (t < dur)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float u = Mathf.Clamp01(t / dur);

            // ---- �¿� ��鸲(+����) ----
            float s = Mathf.Sin(u * omega) * (1f - u); // ������ ������ �۾���
            float offX = s * amp;
            transform.localPosition = _baseLocalPos + new Vector3(offX, 0f, 0f);

            // ����(�¿� ���⿡ ����)
            float z = -Mathf.Sign(offX) * Mathf.InverseLerp(0f, amp, Mathf.Abs(offX)) * tiltDegrees;
            transform.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, z);

            // ---- ��ġ(������&��Ʈ��ġ) ----
            // �� �� Ƣ�� ���ƿ��� �� ���: 0��1��0
            float punchEnvelope = Mathf.Sin(u * Mathf.PI);  // 0..1..0
            float sx = 1f + punchHorizontal * strength * punchEnvelope; // ���� �ø�
            float sy = 1f - punchVertical * strength * punchEnvelope; // ���� ����
            transform.localScale = Vector3.Scale(_baseLocalScale, new Vector3(sx, sy, 1f));

            yield return null;
        }

        ResetPose();
        _co = null;
    }

    public void ResetPose()
    {
        transform.localPosition = _baseLocalPos;
        transform.localRotation = _baseLocalRot;
        transform.localScale = _baseLocalScale;
    }
    public void Revive(Vector3? localSpawn = null, float invulnDuration = 1.2f, float blinkInterval = 0.1f)
    {
        // 1) ���� ���� ��ǡ��ڷ�ƾ ����
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        // 2) Ȱ��ȭ ����
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // 3) ����/�̵� ���� ����(���� ���)
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        var rb3 = GetComponent<Rigidbody>();
        if (rb3 != null) { rb3.linearVelocity = Vector3.zero; rb3.angularVelocity = Vector3.zero; }

        // 4) ��ġ/�ڼ�/������ ����
        if (localSpawn.HasValue)
        {
            // ��û�� ���� ���� ��ġ�� �̵� (ȸ��/�������� ���̽� ��)
            transform.localPosition = localSpawn.Value;
            transform.localRotation = _baseLocalRot;
            transform.localScale = _baseLocalScale;
        }
        else
        {
            // �⺻ ����� ����
            ResetPose();
        }

        // 5) ��� ���� ����(������ ������). 0�̸� ��ŵ
        if (invulnDuration > 0f && blinkInterval > 0f)
            StartCoroutine(CoReviveBlink(invulnDuration, blinkInterval));
    }
    IEnumerator CoReviveBlink(float duration, float interval)
    {
        float t = 0f;
        // ���� ��� Renderer ���
        var renderers = GetComponentsInChildren<Renderer>(true);
        // Ȥ�� ĵ���� UI�� Graphic�� ó��
        var graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        var tmpros = GetComponentsInChildren<TMPro.TMP_Text>(true);

        // ������ ���
        var rEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) rEnabled[i] = renderers[i].enabled;

        // �׷���/�ؽ�Ʈ�� ���� ��ۿ�
        System.Func<float> getDelta = () => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        while (t < duration)
        {
            // ���
            bool on = ((int)(t / interval) % 2) == 0;
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = on;

            // UI �׷���/�ؽ�Ʈ�� ��¦ ���� ������
            float alpha = on ? 1f : 0.25f;
            for (int i = 0; i < graphics.Length; i++)
            {
                var c = graphics[i].color; c.a = alpha; graphics[i].color = c;
            }
            for (int i = 0; i < tmpros.Length; i++)
            {
                var c = tmpros[i].color; c.a = alpha; tmpros[i].color = c;
            }

            t += getDelta();
            yield return null;
        }

        // ���� ����
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = rEnabled[i];

        for (int i = 0; i < graphics.Length; i++)
        {
            var c = graphics[i].color; c.a = 1f; graphics[i].color = c;
        }
        for (int i = 0; i < tmpros.Length; i++)
        {
            var c = tmpros[i].color; c.a = 1f; tmpros[i].color = c;
        }
    }
}

