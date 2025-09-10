// ZombieChaser.cs
using UnityEngine;

public class ZombieChaser : MonoBehaviour
{
    public GameManager game;
    public Transform zombie;
    public Transform nearPoint;
    public Transform farPoint;
    public float smooth = 12f; // 클수록 빨리 따라옴

    float t; // 0~1

    void OnEnable()
    {
        if (!game) return;

        // 1) 무조건 구독
        game.OnDistanceNormalized += SetT;

        // 2) 현재 상태로 즉시 1회 갱신(초기에 near로 튀는 현상 방지)
        float init = (game.isGameStart && game.config.maxDistance > 0f)
        ? Mathf.Clamp01(game.Distance / game.config.maxDistance)
        : 1f;

        SetT(init);
    }

    void OnDisable()
    {
        if (game) game.OnDistanceNormalized -= SetT;
    }

    void SetT(float normalized) { t = normalized; }

    void LateUpdate()
    {
        if (!game || !game.isGameStart) return;
        if (!zombie || !nearPoint || !farPoint) return;

        // t=0 → nearPoint, t=1 → farPoint
        Vector3 target = Vector3.Lerp(nearPoint.position, farPoint.position, t);

        // 지수 보간으로 부드럽게 따라오기
        float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        zombie.position = Vector3.Lerp(zombie.position, target, k);
    }
}
