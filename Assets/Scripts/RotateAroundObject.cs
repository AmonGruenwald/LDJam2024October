using UnityEngine;

public class RotateAroundObject : MonoBehaviour
{
    public float rotationSpeed = 20f; // Degrees per second
    public float distance = 5f; // Distance from the target object
    private GameObject targetObject;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (targetObject != null)
        {
            // Calculate the new position
            Vector3 newPosition = targetObject.transform.position + (transform.position - targetObject.transform.position).normalized * distance;

            // Move to the new position
            transform.position = newPosition;

            // Rotate around the target object
            transform.RotateAround(targetObject.transform.position, Vector3.up, rotationSpeed * Time.fixedDeltaTime);

            // Keep looking at the target object
            transform.LookAt(targetObject.transform);
        }
    }

    public void SetTargetObject(GameObject obj)
    {
        targetObject = obj;
    }
}