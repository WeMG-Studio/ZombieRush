using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

[System.Serializable]
public class LevelRow
{
    public int ID;
    public int Score;
    public int Decay;
    public int Straight_MIN;
    public int Straight_MAX;
    public List<int> PatternID_Array;
}
public class LevelCsvLoader : MonoBehaviour
{
    public List<LevelRow> data = new List<LevelRow>();

    void Awake()
    {
        LoadFromResources();
        Debug.Log(data.Count);
        // 테스트 출력
        for (int i = 0; i < data.Count; i++)
        {
            var r = data[i];
            Debug.Log(
                $"ID={r.ID}, Score={r.Score}, Decay={r.Decay}, " +
                $"Straight=[{r.Straight_MIN}-{r.Straight_MAX}], " +
                $"Patterns=({string.Join(",", r.PatternID_Array)})"
            );
        }
    }

    void LoadFromResources()
    {
        // Assets/Resources/csv/Level.csv  →  Resources.Load("csv/Level")
        TextAsset csv = Resources.Load<TextAsset>("csv/Level");
        if (csv == null)
        {
            Debug.LogError("csv/Level.csv 를 찾을 수 없음");
            return;
        }

        var rows = ParseCsv(csv.text);    // List<string[]>로 파싱
        if (rows.Count == 0) return;

        // --- 헤더 인덱스 매핑 ---
        var header = rows[0];
        int idxID = Array.IndexOf(header, "ID");
        int idxScore = Array.IndexOf(header, "Score");
        int idxDecay = Array.IndexOf(header, "Decay");
        int idxStraightMin = Array.IndexOf(header, "Straight_MIN");
        int idxStraightMax = Array.IndexOf(header, "Straight_MAX");
        int idxPatternArr = Array.IndexOf(header, "PatternID_Array");

        // 필수 컬럼 체크
        if (idxID < 0 || idxScore < 0 || idxDecay < 0 ||
            idxStraightMin < 0 || idxStraightMax < 0 || idxPatternArr < 0)
        {
            Debug.LogError("CSV 헤더가 예상과 다름");
            return;
        }

        // --- 데이터 행 파싱 ---
        data.Clear();
        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Length == 0) continue;
            // 빈 줄 방지
            bool allEmpty = true;
            for (int k = 0; k < row.Length; k++) if (!string.IsNullOrWhiteSpace(row[k])) { allEmpty = false; break; }
            if (allEmpty) continue;

            LevelRow item = new LevelRow();
            item.ID = ToInt(row, idxID);
            item.Score = ToInt(row, idxScore);
            item.Decay = ToInt(row, idxDecay);
            item.Straight_MIN = ToInt(row, idxStraightMin);
            item.Straight_MAX = ToInt(row, idxStraightMax);
            item.PatternID_Array = ParsePatternArray(Get(row, idxPatternArr));

            data.Add(item);
        }
    }

    // --------- 유틸들 ---------
    static string Get(string[] arr, int i) => (i >= 0 && i < arr.Length) ? arr[i]?.Trim() : "";

    static int ToInt(string[] arr, int i)
    {
        int v;
        return int.TryParse(Get(arr, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
    }

    // "(1,2,3)" 또는 "\"(1,2)\"" 같이 따옴표가 있을 수 있음
    static List<int> ParsePatternArray(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new List<int>();
        s = s.Trim().Trim('"').Trim();     // 양쪽 따옴표 제거
        if (s.StartsWith("(") && s.EndsWith(")"))
            s = s.Substring(1, s.Length - 2);

        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(s)) return list;

        var parts = s.Split(',');
        foreach (var p in parts)
        {
            if (int.TryParse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                list.Add(id);
        }
        return list;
    }

    // 따옴표 보존한 CSV 스플리터 (쉼표가 따옴표 내부에 있으면 분리하지 않음)
    public static List<string[]> ParseCsv(string text)
    {
        var result = new List<string[]>();
        using (var reader = new StringReader(text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                result.Add(SplitCsvLine(line));
            }
        }
        return result;
    }

    // 한 줄 파싱
    public static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        if (line == null) return fields.ToArray();

        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // "" → " (escape)
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur.Append('"');
                    i++; // 하나 더 소비
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(cur.ToString());
                cur.Length = 0;
            }
            else
            {
                cur.Append(c);
            }
        }

        fields.Add(cur.ToString());
        return fields.ToArray();
    }
}
