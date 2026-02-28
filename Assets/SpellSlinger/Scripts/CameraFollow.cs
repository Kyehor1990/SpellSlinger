using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public Transform target;  // Oyuncumuz        
    public Vector3 offset = new Vector3(0, 0, -10f); // Kameranın durduğu yer

    [Header("Yumuşatma Ayarları")]
    [Range(0, 1)] 
    public float smoothTime = 0.2f;  
    
    private Vector3 currentVelocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // Hedef pozisyon
        Vector3 targetPosition = target.position + offset;

        // Aniden durmasın diye SmoothDamp kullandım
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref currentVelocity, 
            smoothTime
        );
    }
}