using DG.Tweening;
using System;
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
    private float randomOffset;
    private Vector3 basePosition;
    private bool shrinking = false;
    void Start()
    {
        lineRenderer = this.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 3;
        randomOffset = UnityEngine.Random.Range(0.0f, 180.0f);
        basePosition = Foot.transform.localPosition;
    }

    public void StartFootTween()
    {
        if (shrinking)
        {
            return;
        }
        float randomRightFactor = UnityEngine.Random.Range(-1.0f, 1.0f);
        float randomForwardFactor = UnityEngine.Random.Range(-1.0f, 1.0f);
        float scale = 0.2f;
        Vector3 prevPosition = Foot.transform.localPosition;
        Vector3 newFootPosition = basePosition + this.Foot.right * randomRightFactor * scale + this.Foot.forward * randomForwardFactor * scale;
        Foot.localPosition = newFootPosition;
        Vector3 targetPosition = Foot.position;
        Foot.localPosition = prevPosition;
        Foot.DOMove(targetPosition, UnityEngine.Random.Range(0.35f, 0.45f))
            .SetEase(Ease.InElastic)
            .SetEase(Ease.OutElastic)
            .OnComplete(() => { 
            StartFootTween();});
    }

    // Update is called once per frame
    void Update()
    {
        if (!shrinking)
        {

            Vector3 footDifference = Foot.localPosition - basePosition;
            Vector3 kneeOffset = new Vector3(0, Mathf.Sin(Time.time * 7 + randomOffset) * 0.05f, Mathf.Sin(Time.time * 5 + randomOffset) * 0.01f);
            Vector3[] position = new Vector3[] { Base.localPosition, Knee.localPosition + kneeOffset + footDifference * 0.25f, Foot.localPosition };
            lineRenderer.SetPositions(position);
        }
        else
        {
            Vector3[] position = new Vector3[] { Base.localPosition, Knee.localPosition, Foot.localPosition };
            lineRenderer.SetPositions(position);
        }
    }

    public void Shrink()
    {
        shrinking = true;
        Vector3 goal = Base.localPosition;
        goal.y = -1;
        Base.DOLocalMove(goal, 0.95f);
        Knee.DOLocalMove(goal, 0.95f);
        Foot.DOLocalMove(goal, 0.95f);
    }
}
