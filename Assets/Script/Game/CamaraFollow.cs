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
    public float maximumPivot = 60f;

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
    private Transform headTarget;

    private void Awake()
    {
        FindReferences();
        targetDistance = defaultDistance;
        actualDistance = defaultDistance;
    }

    private void Start()
    {
        if (player != null)
            transform.position = player.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (player != null)
        {
            HandleCameraRotation();
            UpdateCameraPivotPosition();
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
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (cameraObject == null)
            cameraObject = Camera.main;

        if (cameraPivot == null)
            CreateCameraPivot();

        if (player != null)
        {
            Transform foundHead = player.Find("Head");
            if (foundHead != null)
                headTarget = foundHead;
            else
                headTarget = player;
        }
    }

    void CreateCameraPivot()
    {
        GameObject pivotObject = new GameObject("CameraPivot");
        cameraPivot = pivotObject.transform;
        cameraPivot.SetParent(player != null ? player : transform);
        cameraPivot.localPosition = Vector3.zero;
        cameraPivot.localRotation = Quaternion.identity;
    }

    void UpdateCameraPivotPosition()
    {
        if (player == null || cameraPivot == null) return;

        if (headTarget != null)
            cameraPivot.position = headTarget.position;
        else
            cameraPivot.position = player.position + Vector3.up * 1.6f;
    }

    void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * pivotSpeed;

        lookAngle += mouseX;
        pivotAngle -= mouseY;
        pivotAngle = Mathf.Clamp(pivotAngle, minimumPivot, maximumPivot);

        // Rotación horizontal (Y) aplicada al pivot principal
        transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);

        // Rotación vertical (X) aplicada al pivot de la cámara
        cameraPivot.localRotation = Quaternion.Euler(pivotAngle, 0f, 0f);
    }

    void HandleCameraPosition()
    {
        // La cámara sigue la posición del pivot
        Vector3 targetPosition = cameraPivot.position;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, cameraSmoothTime);

        // Aplicar la rotación vertical para que la cámara mire hacia arriba/abajo correctamente
        cameraObject.transform.position = cameraPivot.position - cameraPivot.forward * actualDistance;
        cameraObject.transform.rotation = cameraPivot.rotation;
    }

    void HandleCameraCollision()
    {
        if (!enableCameraCollision) return;

        Vector3 direction = -cameraPivot.forward;
        RaycastHit hit;

        if (Physics.SphereCast(cameraPivot.position, cameraCollisionRadius, direction, out hit, targetDistance + collisionOffset, collisionLayers))
        {
            actualDistance = Mathf.Clamp(hit.distance - collisionOffset, minDistance, maxDistance);
        }
        else
        {
            actualDistance = targetDistance;
        }
    }

    public void SetCameraDistance(float distance)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    public void SetCameraSensitivity(float sensitivity)
    {
        lookSpeed = sensitivity;
        pivotSpeed = sensitivity;
    }

    public void ResetCameraBehindPlayer()
    {
        if (player != null)
        {
            lookAngle = player.eulerAngles.y;
            pivotAngle = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraPivot != null && enableCameraCollision)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraPivot.position, cameraCollisionRadius);
            Gizmos.DrawLine(cameraPivot.position, cameraPivot.position - cameraPivot.forward * (targetDistance + collisionOffset));
        }
    }
}
