using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Leg : MonoBehaviour
{
    public Transform Base;
    public Transform Knee;
    public Transform Foot;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = this.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 3;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 kneeOffset = new Vector3(0, Mathf.Sin(Time.time * 7) * 0.05f, Mathf.Sin(Time.time * 5) * 0.01f);
        Vector3[] position = new Vector3[] { Base.localPosition, Knee.localPosition + kneeOffset, Foot.localPosition };
        lineRenderer.SetPositions(position);
    }
}
