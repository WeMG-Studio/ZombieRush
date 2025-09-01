using UnityEngine;

[ExecuteAlways]
public class ScreenAnchor2D : MonoBehaviour
{
    public Camera cam;

    public enum H { Left, Center, Right }
    public enum V { Bottom, Center, Top }
    public H horizontal = H.Center;   // ���� ��Ŀ
    public V vertical = V.Bottom;   // ���� ��Ŀ

    [Tooltip("�����ڸ����� �������� �о���� ����(����Ʈ ����, 0~1)")]
    public Vector2 viewportMargin = new Vector2(0f, 0.05f);

    [Tooltip("�����ڸ����� �������� �о���� ����(�ȼ� ����)")]
    public Vector2 pixelMargin = Vector2.zero;

    [Tooltip("�� ������ ����(ī�޶� �̵�/�ػ� ���� ����)")]
    public bool stickEveryFrame = true;

    void Reset() { cam = Camera.main; }
    void Awake() { if (!cam) cam = Camera.main; Reposition(); }
    void LateUpdate() { if (stickEveryFrame) Reposition(); }
    void OnValidate() { if (!Application.isPlaying) Reposition(); }

    public void Reposition()
    {
        if (!cam) return;

        // 1) ���� ��Ŀ(Left/Center/Right, Bottom/Center/Top)�� ����Ʈ�� ���
        float vx = horizontal == H.Left ? 0f : horizontal == H.Center ? 0.5f : 1f;
        float vy = vertical == V.Bottom ? 0f : vertical == V.Center ? 0.5f : 1f;

        // 2) ����Ʈ ���� ����(�𼭸����� ��¦ ����)
        vx += (horizontal == H.Left ? 1 : horizontal == H.Right ? -1 : 0) * viewportMargin.x;
        vy += (vertical == V.Bottom ? 1 : vertical == V.Top ? -1 : 0) * viewportMargin.y;
        vx = Mathf.Clamp01(vx);
        vy = Mathf.Clamp01(vy);

        // 3) ����(ī�޶�� ������Ʈ z �Ÿ�) ���� ���� ��ǥ ��ȯ
        float depth = Mathf.Abs(transform.position.z - cam.transform.position.z);
        if (cam.orthographic) depth = Mathf.Max(depth, 0.001f);

        Vector3 world = cam.ViewportToWorldPoint(new Vector3(vx, vy, depth));

        // 4) �ȼ� ������ ����� ȯ���ؼ� ���ϱ�(�ɼ�)
        if (pixelMargin != Vector2.zero)
        {
            // ��Ŀ ���⿡ ���� ����(+)���� �б�
            float px = (horizontal == H.Left ? 1 : horizontal == H.Right ? -1 : 0) * pixelMargin.x;
            float py = (vertical == V.Bottom ? 1 : vertical == V.Top ? -1 : 0) * pixelMargin.y;

            Vector3 w0 = cam.ScreenToWorldPoint(new Vector3(0, 0, depth));
            Vector3 w1 = cam.ScreenToWorldPoint(new Vector3(px, py, depth));
            world += (w1 - w0);
        }

        world.z = transform.position.z;   // ���� z�� ����(2D�� 0 ��ó)
        transform.position = world;
    }
}