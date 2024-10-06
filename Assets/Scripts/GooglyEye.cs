using UnityEngine;

public class GooglyEyeEffect : MonoBehaviour
{
    public Transform pupil;
    private Transform eye;
    public float maxOffset = 0.3f;
    public float followSpeed = 5f;
    public float gravity = 9.8f;

    private Vector3 velocity;
    private Vector3 pupilBasePosition;
    private Vector3 lastEyePosition;
    private Vector3 pupilTarget; 
    private float radius = 0.128f;

    void Start()
    {
        pupilBasePosition = pupil.localPosition;
        pupilTarget = new Vector3(0,0, radius);
        Debug.Log(pupilTarget);
        eye = this.transform;
        lastEyePosition = eye.position;
    }

    void Update()
    {
        // Calculate eye movement
        Vector3 eyeMovement = eye.position - lastEyePosition;
        lastEyePosition = eye.position;

        pupilTarget += eyeMovement * Time.deltaTime * 50;
        pupilTarget = Vector3.Lerp(pupilTarget, pupilBasePosition + Random.insideUnitSphere * 0.3f, Time.deltaTime * 5);
        Vector3 direction = pupilTarget.normalized;
        pupil.localPosition = direction * radius;
    }
}
