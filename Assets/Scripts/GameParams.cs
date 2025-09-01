using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameParams")]
public class GameParams : ScriptableObject
{
    [Header("Distance / Zombie")]
    public float maxDistance = 100f;
    public float baseDecay = 4f;          // 초당
    public float decayPerLevel = 1.2f;
    public float maxDecay = 18f;
    public float gainPerStep = 8f;        // 전진 성공 시 회복

    [Header("Progression")]
    public int stepsPerLevel = 10;        // 몇 칸마다 레벨 +1
    public int visibleTiles = 7;

    [Header("Generation")]
    public float bentProbMin = 0.15f;     // 초반 휘어진 타일 확률
    public float bentProbMax = 0.4f;      // 상한

    [Header("Fail FX")]
    public float failSlowmo = 0.15f;
    public float failSlowmoTime = 0.35f;
}