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
}
