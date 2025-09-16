using System.Collections.Generic;
using UnityEngine;

public class RailManager : MonoBehaviour
{
    public GameParams config;
    public Transform laneRoot;
    public RailTile straightPrefab;
    public RailTile bentPrefab;

    readonly Queue<RailTile> tiles = new();
    public int steps;
    public int Steps => steps;

    public RailTile CurrentTile { get; private set; }

    float tileSpacing = 1.2f;

    // ? Start���� �ڵ� �������� �ʴ´� (�ߺ� ����)
    void Start()
    {
        // �����. ������ GameManager���� ��������� ȣ��.
        // �ʿ� ��, ���� �� �̸� ����� �ڵ� ������ �ʿ��ϸ�
        // �Ʒ� �� ���� �ӽ÷� �Ѱ�, ���� ���忡���� ������.
        // BuildRailFresh();
    }

    /// <summary>
    /// ť/�ڽ� ��� ������ (���� X)
    /// </summary>
    public void InitRail()
    {
        // ť�� Ÿ�� ����
        foreach (var t in tiles)
            if (t) Destroy(t.gameObject);
        tiles.Clear();

        // laneRoot�� �ܿ� �ڽĵ� ���� (ť�� ���� ���� ����)
        if (laneRoot != null)
        {
            for (int i = laneRoot.childCount - 1; i >= 0; i--)
                Destroy(laneRoot.GetChild(i).gameObject);
        }

        steps = 0;
        CurrentTile = null;
    }

    /// <summary>
    /// ���� �ʱ�ȭ ��, �� ���� �������� �� ����
    /// </summary>
    public void BuildRailFresh()
    {
        InitRail();
        InitLine(); // �� ���� ����
    }

    void InitLine()
    {
        for (int i = 0; i < config.visibleTiles; i++)
        {
            RailTile t;
            if (i == 0)
            {
                t = SpawnTileForced(false); // ù Ÿ���� ����
            }
            else
            {
                t = SpawnRandomTile();
            }

            t.transform.SetParent(laneRoot, false);
            t.transform.localPosition = new Vector3(0, i * tileSpacing, 0);
            tiles.Enqueue(t);
            if (i == 0) CurrentTile = t;
        }
    }

    RailTile SpawnRandomTile()
    {
        var level = Mathf.FloorToInt(steps / (float)config.stepsPerLevel);
        float p = Mathf.Lerp(config.bentProbMin, config.bentProbMax, Mathf.InverseLerp(0, 10, level));
        bool bent = Random.value < p;
        var prefab = bent ? bentPrefab : straightPrefab;
        var inst = Instantiate(prefab);
        inst.SetType(bent ? RailType.Bent : RailType.Straight);
        return inst;
    }

    RailTile SpawnTileForced(bool bent)
    {
        var prefab = bent ? bentPrefab : straightPrefab;
        var inst = Instantiate(prefab);
        inst.SetType(bent ? RailType.Bent : RailType.Straight);
        return inst;
    }

    public void FixCurrent() => CurrentTile?.FixToStraight();

    public bool TryAdvance()
    {
        if (CurrentTile.Type != RailType.Straight) return false;
        AdvanceCommon(SpawnRandomTile());
        return true;
    }

    public bool TryAdvanceForced(bool bent)
    {
        if (CurrentTile.Type != RailType.Straight) return false;
        AdvanceCommon(SpawnTileForced(bent));
        return true;
    }

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
