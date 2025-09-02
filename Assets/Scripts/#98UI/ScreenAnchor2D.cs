using UnityEngine;

[ExecuteAlways]
public class ScreenAnchor2D : MonoBehaviour
{
    public Camera cam;

    public enum H { Left, Center, Right }
    public enum V { Bottom, Center, Top }
    public H horizontal = H.Center;   // 가로 앵커
    public V vertical = V.Bottom;   // 세로 앵커

    [Tooltip("가장자리에서 안쪽으로 밀어넣을 여백(뷰포트 비율, 0~1)")]
    public Vector2 viewportMargin = new Vector2(0f, 0.05f);

    [Tooltip("가장자리에서 안쪽으로 밀어넣을 여백(픽셀 단위)")]
    public Vector2 pixelMargin = Vector2.zero;

    [Tooltip("매 프레임 고정(카메라 이동/해상도 변경 대응)")]
    public bool stickEveryFrame = true;

    void Reset() { cam = Camera.main; }
    void Awake() { if (!cam) cam = Camera.main; Reposition(); }
    void LateUpdate() { if (stickEveryFrame) Reposition(); }
    void OnValidate() { if (!Application.isPlaying) Reposition(); }

    public void Reposition()
    {
        if (!cam) return;

        // 1) 기준 앵커(Left/Center/Right, Bottom/Center/Top)를 뷰포트로 계산
        float vx = horizontal == H.Left ? 0f : horizontal == H.Center ? 0.5f : 1f;
        float vy = vertical == V.Bottom ? 0f : vertical == V.Center ? 0.5f : 1f;

        // 2) 뷰포트 여백 적용(모서리에서 살짝 안쪽)
        vx += (horizontal == H.Left ? 1 : horizontal == H.Right ? -1 : 0) * viewportMargin.x;
        vy += (vertical == V.Bottom ? 1 : vertical == V.Top ? -1 : 0) * viewportMargin.y;
        vx = Mathf.Clamp01(vx);
        vy = Mathf.Clamp01(vy);

        // 3) 깊이(카메라와 오브젝트 z 거리) 맞춰 월드 좌표 변환
        float depth = Mathf.Abs(transform.position.z - cam.transform.position.z);
        if (cam.orthographic) depth = Mathf.Max(depth, 0.001f);

        Vector3 world = cam.ViewportToWorldPoint(new Vector3(vx, vy, depth));

        // 4) 픽셀 여백을 월드로 환산해서 더하기(옵션)
        if (pixelMargin != Vector2.zero)
        {
            // 앵커 방향에 따라 안쪽(+)으로 밀기
            float px = (horizontal == H.Left ? 1 : horizontal == H.Right ? -1 : 0) * pixelMargin.x;
            float py = (vertical == V.Bottom ? 1 : vertical == V.Top ? -1 : 0) * pixelMargin.y;

            Vector3 w0 = cam.ScreenToWorldPoint(new Vector3(0, 0, depth));
            Vector3 w1 = cam.ScreenToWorldPoint(new Vector3(px, py, depth));
            world += (w1 - w0);
        }

        world.z = transform.position.z;   // 현재 z는 유지(2D면 0 근처)
        transform.position = world;
    }
}