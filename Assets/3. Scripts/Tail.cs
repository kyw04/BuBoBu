using Fusion;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Tail : NetworkBehaviour
{
    [SerializeField] private float speed;
    
    private LineRenderer lineRenderer;
    private Vector3[] linePoints;
    private Vector3 tailPos;

    private Vector3 curPos;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position);
    }
    
    void Start()
    {
        tailPos = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        if (Mathf.Abs(tailPos.x - transform.position.x) < 0.01f &&
            Mathf.Abs(tailPos.y - transform.position.y) < 0.01f)
            return;
        
        tailPos = Vector3.SmoothDamp(tailPos, transform.position, ref velocity, speed * Time.deltaTime);
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, tailPos);
    }
}
