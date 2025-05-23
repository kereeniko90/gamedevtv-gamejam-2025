using UnityEngine;

public class PlaceableSurface : MonoBehaviour
{
    [SerializeField] private bool showDebugBounds = true;
    
    private void OnDrawGizmos()
    {
        if (showDebugBounds)
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
        }
    }
}