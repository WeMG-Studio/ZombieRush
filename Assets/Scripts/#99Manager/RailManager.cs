using System.Collections.Generic;
using UnityEngine;

public class RailManager : MonoBehaviour
{
    public GameParams config;
    public Transform laneRoot;           // 타일들을 줄맞춤으로 배치할 부모
    public RailTile straightPrefab;
    public RailTile bentPrefab;

    readonly Queue<RailTile> tiles = new();
    int steps;                           // 총 전진 칸
    public int Steps => steps;

    public RailTile CurrentTile { get; private set; }

    float tileSpacing = 1.2f;            // 타일 간격(월드)

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
                             Mathf.InverseLerp(0, 10, level)); // 레벨 0~10 구간 스무딩
        bool bent = Random.value < p;
        var prefab = bent ? bentPrefab : straightPrefab;
        var inst = Instantiate(prefab);
        inst.SetType(bent ? RailType.Bent : RailType.Straight);
        return inst;
    }

    // 새로 추가: 직선/회전을 강제로 지정해 생성
    RailTile SpawnTileForced(bool bent)
    {
        var prefab = bent ? bentPrefab : straightPrefab;
        var inst = Instantiate(prefab);
        inst.SetType(bent ? RailType.Bent : RailType.Straight);
        return inst;
    }

    public void FixCurrent() => CurrentTile?.FixToStraight();

    // ▶ 기존 버튼: 전진 (랜덤)
    public bool TryAdvance()
    {
        if (CurrentTile.Type != RailType.Straight) return false;

        AdvanceCommon(SpawnRandomTile());
        return true;
    }

    // ▶ 새로 추가: 전진 (강제 패턴)
    // bent = true → 회전 타일, false → 직선 타일
    public bool TryAdvanceForced(bool bent)
    {
        if (CurrentTile.Type != RailType.Straight) return false;

        AdvanceCommon(SpawnTileForced(bent));
        return true;
    }

    // 타일 큐 갱신 로직 공통화
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
