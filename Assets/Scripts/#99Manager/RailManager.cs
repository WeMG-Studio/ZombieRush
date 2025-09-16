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

    // ? Start에서 자동 생성하지 않는다 (중복 방지)
    void Start()
    {
        // 비워둠. 생성은 GameManager에서 명시적으로 호출.
        // 필요 시, 개발 중 미리 보기용 자동 생성이 필요하면
        // 아래 한 줄을 임시로 켜고, 실제 빌드에서는 꺼두자.
        // BuildRailFresh();
    }

    /// <summary>
    /// 큐/자식 모두 정리만 (생성 X)
    /// </summary>
    public void InitRail()
    {
        // 큐의 타일 제거
        foreach (var t in tiles)
            if (t) Destroy(t.gameObject);
        tiles.Clear();

        // laneRoot의 잔여 자식도 제거 (큐에 없던 잔재 방지)
        if (laneRoot != null)
        {
            for (int i = laneRoot.childCount - 1; i >= 0; i--)
                Destroy(laneRoot.GetChild(i).gameObject);
        }

        steps = 0;
        CurrentTile = null;
    }

    /// <summary>
    /// 완전 초기화 후, 새 라인 생성까지 한 번에
    /// </summary>
    public void BuildRailFresh()
    {
        InitRail();
        InitLine(); // 새 라인 구성
    }

    void InitLine()
    {
        for (int i = 0; i < config.visibleTiles; i++)
        {
            RailTile t;
            if (i == 0)
            {
                t = SpawnTileForced(false); // 첫 타일은 직선
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
