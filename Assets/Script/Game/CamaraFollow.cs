using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Transform cameraPivot;
    public Camera cameraObject;
    
    [Header("Configuración de Cámara")]
    public float cameraSmoothTime = 0.1f;
    public float lookSpeed = 2f;
    public float pivotSpeed = 2f;
    public float minimumPivot = -35f;
    public float maximumPivot = 35f;
    
    [Header("Configuración de Colisiones")]
    public bool enableCameraCollision = true;
    public float cameraCollisionRadius = 0.2f;
    public LayerMask collisionLayers = -1;
    public float collisionOffset = 0.2f;
    
    [Header("Distancias")]
    public float defaultDistance = 3f;
    public float minDistance = 1f;
    public float maxDistance = 6f;
    
    private Vector3 cameraVelocity = Vector3.zero;
    private float lookAngle;
    private float pivotAngle;
    private float targetDistance;
    private float actualDistance;
    
    private void Awake()
    {
        // Buscar referencias automáticamente si no están asignadas
        FindReferences();
        
        // Inicializar distancia
        targetDistance = defaultDistance;
        actualDistance = defaultDistance;
    }
    
    private void Start()
    {
        // Posicionar cámara detrás del jugador
        if (player != null)
        {
            transform.position = player.position;
        }
        
        // Ocultar y bloquear cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void LateUpdate()
    {
        if (player != null)
        {
            HandleCameraRotation();
            HandleCameraPosition();
            HandleCameraCollision();
        }
        else
        {
            FindReferences();
        }
    }
    
    void FindReferences()
    {
        // Buscar jugador si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("Jugador encontrado automáticamente para la cámara");
            }
        }
        
        // Buscar cámara principal si no está asignada
        if (cameraObject == null)
        {
            cameraObject = Camera.main;
            if (cameraObject != null)
            {
                Debug.Log("Cámara principal encontrada automáticamente");
            }
        }
        
        // Crear pivot de cámara si no existe
        if (cameraPivot == null)
        {
            CreateCameraPivot();
        }
    }
    
    void CreateCameraPivot()
    {
        // Crear un GameObject para el pivot de la cámara
        GameObject pivotObject = new GameObject("CameraPivot");
        cameraPivot = pivotObject.transform;
        
        // Si tenemos jugador, hacer el pivot hijo del jugador
        if (player != null)
        {
            cameraPivot.SetParent(player);
            cameraPivot.localPosition = Vector3.zero;
            cameraPivot.localRotation = Quaternion.identity;
        }
        else
        {
            // Si no hay jugador, hacerlo hijo de la cámara principal
            cameraPivot.SetParent(transform);
            cameraPivot.localPosition = Vector3.zero;
        }
        
        Debug.Log("Pivot de cámara creado automáticamente");
    }
    
    void HandleCameraRotation()
    {
        // Obtener input del mouse
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * pivotSpeed;
        
        // Rotar la cámara horizontalmente (alrededor del eje Y)
        lookAngle += mouseX;
        
        // Rotar la cámara verticalmente (alrededor del eje X)
        pivotAngle -= mouseY;
        pivotAngle = Mathf.Clamp(pivotAngle, minimumPivot, maximumPivot);
        
        // Aplicar rotación al transform de la cámara
        Vector3 rotation = Vector3.zero;
        rotation.y = lookAngle;
        transform.rotation = Quaternion.Euler(rotation);
        
        // Aplicar rotación vertical al pivot
        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        cameraPivot.localRotation = Quaternion.Euler(rotation);
    }
    
    void HandleCameraPosition()
    {
        // Seguir la posición del jugador/pivot
        Vector3 targetPosition = cameraPivot.position;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, cameraSmoothTime);
    }
    
    void HandleCameraCollision()
    {
        if (!enableCameraCollision) return;
        
        // Dirección desde el pivot hacia la cámara
        Vector3 cameraDirection = (transform.position - cameraPivot.position).normalized;
        
        // Raycast para detectar colisiones
        RaycastHit hit;
        if (Physics.SphereCast(
            cameraPivot.position, 
            cameraCollisionRadius, 
            cameraDirection, 
            out hit, 
            targetDistance + collisionOffset, 
            collisionLayers))
        {
            // Si hay colisión, ajustar la distancia
            actualDistance = Mathf.Clamp(hit.distance - collisionOffset, minDistance, maxDistance);
        }
        else
        {
            // Sin colisión, usar distancia objetivo
            actualDistance = targetDistance;
        }
        
        // Posicionar la cámara a la distancia calculada
        Vector3 targetCameraPosition = cameraPivot.position - transform.forward * actualDistance;
        cameraObject.transform.position = targetCameraPosition;
    }
    
    // Método para cambiar la distancia de la cámara (zoom)
    public void SetCameraDistance(float distance)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
    }
    
    // Método para cambiar la sensibilidad
    public void SetCameraSensitivity(float sensitivity)
    {
        lookSpeed = sensitivity;
        pivotSpeed = sensitivity;
    }
    
    // Método para reorientar la cámara detrás del jugador
    public void ResetCameraBehindPlayer()
    {
        if (player != null)
        {
            lookAngle = player.eulerAngles.y;
            pivotAngle = 0f;
        }
    }
    
    // Método para alternar el cursor
    public void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    // Dibujar gizmos para debug en el editor
    private void OnDrawGizmosSelected()
    {
        if (cameraPivot != null && enableCameraCollision)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraPivot.position, cameraCollisionRadius);
            
            Vector3 cameraDirection = (transform.position - cameraPivot.position).normalized;
            Gizmos.DrawLine(cameraPivot.position, cameraPivot.position + cameraDirection * (targetDistance + collisionOffset));
        }
    }
}