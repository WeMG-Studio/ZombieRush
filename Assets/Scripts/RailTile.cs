using UnityEngine;

public enum RailType { Straight, Bent }

public class RailTile : MonoBehaviour
{
    [SerializeField] GameObject straightVisual;
    [SerializeField] GameObject bentVisual;

    public RailType Type { get; private set; }

    public void SetType(RailType t)
    {
        Type = t;
        if (straightVisual) straightVisual.SetActive(t == RailType.Straight);
        if (bentVisual) bentVisual.SetActive(t == RailType.Bent);
    }
    public void FixToStraight() => SetType(RailType.Straight);
}