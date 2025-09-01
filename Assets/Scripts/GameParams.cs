using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameParams")]
public class GameParams : ScriptableObject
{
    [Header("Distance / Zombie")]
    public float maxDistance = 100f;
    public float baseDecay = 4f;          // �ʴ�
    public float decayPerLevel = 1.2f;
    public float maxDecay = 18f;
    public float gainPerStep = 8f;        // ���� ���� �� ȸ��

    [Header("Progression")]
    public int stepsPerLevel = 10;        // �� ĭ���� ���� +1
    public int visibleTiles = 7;

    [Header("Generation")]
    public float bentProbMin = 0.15f;     // �ʹ� �־��� Ÿ�� Ȯ��
    public float bentProbMax = 0.4f;      // ����

    [Header("Fail FX")]
    public float failSlowmo = 0.15f;
    public float failSlowmoTime = 0.35f;
}