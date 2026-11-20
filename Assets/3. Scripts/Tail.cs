using Fusion;
using UnityEngine;

// 플레이어를 따라오는 꼬리 (순수 연출)
// FixedUpdateNetwork는 다른 플레이어(프록시)에서는 돌지 않으므로 모든 클라이언트에서 도는 Render에서 갱신
[RequireComponent(typeof(LineRenderer))]
public class Tail : NetworkBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float snapDistance = 3f; // 이보다 멀어지면 순간이동(스폰/부활)으로 보고 꼬리를 바로 붙임

    private LineRenderer lineRenderer;
    private Vector3 tailPos;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    public override void Spawned()
    {
        Snap(); // 스폰 위치에서 시작
    }

    public override void Render()
    {
        Vector3 head = transform.position;

        if ((tailPos - head).sqrMagnitude > snapDistance * snapDistance)
        {
            Snap();
            return;
        }

        if ((tailPos - head).sqrMagnitude >= 0.0001f)
            tailPos = Vector3.SmoothDamp(tailPos, head, ref velocity, speed * Time.deltaTime);

        lineRenderer.SetPosition(0, head);
        lineRenderer.SetPosition(1, tailPos);
    }

    private void Snap()
    {
        tailPos = transform.position;
        velocity = Vector3.zero;
        lineRenderer.SetPosition(0, tailPos);
        lineRenderer.SetPosition(1, tailPos);
    }
}
