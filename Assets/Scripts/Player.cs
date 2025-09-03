using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Step Sway")]
    public float amplitude = 0.06f;     // 좌우 흔들림 폭(월드 단위)
    public float duration = 0.12f;      // 전체 길이
    public int oscillations = 2;        // 진동 횟수(왕복 수)
    public float tiltDegrees = 6f;      // 흔들림에 따른 Z 회전(도)
    public bool useUnscaledTime = true; // 슬로모 무시

    [Header("Punch (Squash & Stretch)")]
    [Tooltip("수평 확장 비율(0.06 = 6%)")]
    public float punchHorizontal = 0.06f;
    [Tooltip("수직 압축 비율(0.08 = 8%)")]
    public float punchVertical = 0.08f;

    Vector3 _baseLocalPos;
    Quaternion _baseLocalRot;
    Vector3 _baseLocalScale;
    Coroutine _co;

    void Awake() => SnapBaseline();
    void OnDisable() => ResetPose();

    /// 기준 포즈 저장(에디터에서 위치/스케일 바꿨으면 한 번 호출)
    public void SnapBaseline()
    {
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;
        _baseLocalScale = transform.localScale;
    }

    /// 전진/교정 성공 시 호출. strength로 강도 가중(1 = 기본)
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
        float omega = oscillations * Mathf.PI * 2f; // 2π * 진동수

        while (t < dur)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float u = Mathf.Clamp01(t / dur);

            // ---- 좌우 흔들림(+감쇠) ----
            float s = Mathf.Sin(u * omega) * (1f - u); // 끝으로 갈수록 작아짐
            float offX = s * amp;
            transform.localPosition = _baseLocalPos + new Vector3(offX, 0f, 0f);

            // 기울기(좌우 방향에 따라)
            float z = -Mathf.Sign(offX) * Mathf.InverseLerp(0f, amp, Mathf.Abs(offX)) * tiltDegrees;
            transform.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, z);

            // ---- 펀치(스쿼시&스트레치) ----
            // 한 번 튀고 돌아오는 종 모양: 0→1→0
            float punchEnvelope = Mathf.Sin(u * Mathf.PI);  // 0..1..0
            float sx = 1f + punchHorizontal * strength * punchEnvelope; // 가로 늘림
            float sy = 1f - punchVertical * strength * punchEnvelope; // 세로 줄임
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
