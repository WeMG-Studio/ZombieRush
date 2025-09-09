using UnityEngine;

public class ZombieChaser : MonoBehaviour
{
    public GameManager game;
    public Transform zombie;
    public Transform nearPoint;
    public Transform farPoint;
    public float smooth = 12f; // 값이 클수록 따라붙기 빠름

    float t; // 0~1

    void OnEnable()
    {
        if (game && game.isGameStart) game.OnDistanceNormalized += SetT;
    }
    void OnDisable()
    {
        if (game) game.OnDistanceNormalized -= SetT;
    }

    void SetT(float normalized) { t = normalized; }

    void LateUpdate()
    {
        if(game && game.isGameStart)
        {
            if (!zombie || !nearPoint || !farPoint) return;

            Vector3 target = Vector3.Lerp(nearPoint.position, farPoint.position, t);
            zombie.position = Vector3.Lerp(zombie.position, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        }
        
    }
}