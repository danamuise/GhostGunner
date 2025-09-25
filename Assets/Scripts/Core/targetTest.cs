using UnityEngine;

public class TargetTest : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"{name} collided with {collision.gameObject.name} (Tag: {collision.gameObject.tag})");

        // If the bullet hits a Target
        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log($"{name} hit target: {collision.gameObject.name}");


            if ( collision.gameObject.CompareTag("Target"))
            {
                TargetBehavior tb = collision.gameObject.GetComponentInParent<TargetBehavior>();
                if (tb != null)
                {
                    tb.TakeDamage(1);
                    Debug.Log($"{name} | Ghost Mode hit: {collision.gameObject.name} — health reduced");
                    Destroy(gameObject);
                }

            }

        }
    }
}                                                                           