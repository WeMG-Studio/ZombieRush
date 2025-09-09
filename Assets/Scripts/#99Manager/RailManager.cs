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

    public void FixCurrent() => CurrentTile?.FixToStraight();

    // ▶ 버튼: 전진. 성공 시 true, 실패 시 false
    public bool TryAdvance()
    {
        if (CurrentTile.Type != RailType.Straight) return false;

        // 한 칸 앞으로: 모든 타일을 아래로 내리고 맨 뒤 새 타일 추가
        foreach (var t in tiles)
            t.transform.localPosition += new Vector3(0, -tileSpacing, 0);

        var old = tiles.Dequeue();
        Destroy(old.gameObject);

        var newTile = SpawnRandomTile();
        newTile.transform.SetParent(laneRoot, false);
        newTile.transform.localPosition = new Vector3(0, (config.visibleTiles - 1) * tileSpacing, 0);
        tiles.Enqueue(newTile);

        CurrentTile = tiles.Peek();
        steps++;
        return true;
    }
}