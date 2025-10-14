using UnityEngine;

public class LedgeTrigger : MonoBehaviour
{
    public Vector3 climbUpOffset = new Vector3(0, 1.2f, 0.8f);

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 worldOffset = transform.TransformDirection(climbUpOffset);
        Gizmos.DrawWireSphere(transform.position + worldOffset, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + worldOffset);
    }
}
