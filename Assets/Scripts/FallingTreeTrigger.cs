using UnityEngine;

public class FallingTreeTrigger : MonoBehaviour
{
    [Header("Tree Object to Fall")]
    public Transform treeTransform;
    public float fallSpeed = 90f; // degrees per second
    public Vector3 rotationAxis = Vector3.right;
    public float targetAngle = 75f;

    [Header("Storm Sound")]
    public AudioSource crackWoodAudio;

    private bool isTriggered = false;
    private float currentAngle = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            if (crackWoodAudio != null)
            {
                crackWoodAudio.Play();
            }
            Debug.Log("Storm: Tree is falling!");
        }
    }

    private void Update()
    {
        if (!isTriggered) return;

        if (currentAngle < targetAngle && treeTransform != null)
        {
            float step = fallSpeed * Time.deltaTime;
            currentAngle += step;
            treeTransform.Rotate(rotationAxis, step, Space.Self);
        }
    }

    public void ResetObstacle()
    {
        if (!isTriggered) return;
        
        if (treeTransform != null)
        {
            treeTransform.Rotate(rotationAxis, -currentAngle, Space.Self);
        }
        currentAngle = 0f;
        isTriggered = false;
    }
}
