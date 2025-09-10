using System.Collections.Generic;
using UnityEngine;

public class RailManager : MonoBehaviour
{
    public GameParams config;
    public Transform laneRoot;           // Ÿ�ϵ��� �ٸ������� ��ġ�� �θ�
    public RailTile straightPrefab;
    public RailTile bentPrefab;

    readonly Queue<RailTile> tiles = new();
    int steps;                           // �� ���� ĭ
    public int Steps => steps;

    public RailTile CurrentTile { get; private set; }

    float tileSpacing = 1.2f;            // Ÿ�� ����(����)

    void Start()
    {
        InitLine();
    }

    public void InitRail()
    {
        steps = 0;
    }

    void InitLine()
    {
        for (int i = 0; i < config.visibleTiles; i++)
        {
            var t = SpawnRandomTile();
            t.transform.SetParent(laneRoot, false);
            t.transform.localPosition = new Vector3(0, i * tileSpacing, 0);
            tiles.Enqueue(t);
            if (i == 0) CurrentTile = t;
        }
    }

    RailTile SpawnRandomTile()
    {
        var level = Mathf.FloorToInt(steps / (float)config.stepsPerLevel);
        float p = Mathf.Lerp(config.bentProbMin, config.bentProbMax,
                             Mathf.InverseLerp(0, 10, level)); // ���� 0~10 ���� ������
        bool bent = Random.value < p;
        var prefab = bent ? bentPrefab : straightPrefab;
        var inst = Instantiate(prefab);
        inst.SetType(bent ? RailType.Bent : RailType.Straight);
        return inst;
    }

    // ���� �߰�: ����/ȸ���� ������ ������ ����
    RailTile SpawnTileForced(bool bent)
    {
        var prefab = bent ? bentPrefab : straightPrefab;
        var inst = Instantiate(prefab);
        inst.SetType(bent ? RailType.Bent : RailType.Straight);
        return inst;
    }

    public void FixCurrent() => CurrentTile?.FixToStraight();

    // �� ���� ��ư: ���� (����)
    public bool TryAdvance()
    {
        if (CurrentTile.Type != RailType.Straight) return false;

        AdvanceCommon(SpawnRandomTile());
        return true;
    }

    // �� ���� �߰�: ���� (���� ����)
    // bent = true �� ȸ�� Ÿ��, false �� ���� Ÿ��
    public bool TryAdvanceForced(bool bent)
    {
        if (CurrentTile.Type != RailType.Straight) return false;

        AdvanceCommon(SpawnTileForced(bent));
        return true;
    }

    // Ÿ�� ť ���� ���� ����ȭ
    void AdvanceCommon(RailTile newTile)
    {
        foreach (var t in tiles)
            t.transform.localPosition += new Vector3(0, -tileSpacing, 0);

        var old = tiles.Dequeue();
        Destroy(old.gameObject);

        newTile.transform.SetParent(laneRoot, false);
        newTile.transform.localPosition = new Vector3(0, (config.visibleTiles - 1) * tileSpacing, 0);
        tiles.Enqueue(newTile);

        CurrentTile = tiles.Peek();
        steps++;
    }
}
