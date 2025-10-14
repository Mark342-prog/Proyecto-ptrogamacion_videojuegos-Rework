using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [Header("Referencias")]
    public Camera minimapCamera;
    public Transform player;
    public bool autoFindPlayer = true;
    public float searchInterval = 1f;

    [Header("Configuración Minimapa")]
    public float cameraHeight = 50f;
    public bool rotateWithPlayer = false;
    public Vector2 minimapSize = new Vector2(200, 200);

    private float searchTimer = 0f;

    void Start()
    {
        if (minimapCamera == null)
        {
            // Intentar encontrar la cámara del minimapa automáticamente
            minimapCamera = GetComponent<Camera>();
        }

        if (player == null && autoFindPlayer)
        {
            FindPlayer();
        }
    }

    void LateUpdate()
    {
        if (player != null)
        {
            UpdateMinimapPosition();
        }
        else if (autoFindPlayer)
        {
            // Buscar al jugador periódicamente si no se ha encontrado
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                FindPlayer();
                searchTimer = 0f;
            }
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("Jugador encontrado y asignado al minimapa");
        }
    }

    void UpdateMinimapPosition()
    {
        if (minimapCamera != null)
        {
            // Seguir al jugador en X y Z, mantener altura fija
            Vector3 newPosition = player.position;
            newPosition.y = cameraHeight;
            minimapCamera.transform.position = newPosition;

            // Rotación (opcional)
            if (rotateWithPlayer)
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
            }
            else
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }

    // Método público para asignar manualmente si es necesario
    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }
}