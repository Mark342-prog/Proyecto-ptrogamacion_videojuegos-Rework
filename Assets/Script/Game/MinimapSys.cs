using UnityEngine;
using UnityEngine.UI;

public class MinimapSystem : MonoBehaviour
{
    [Header("Referencias")]
    public Camera minimapCamera;
    public RawImage minimapDisplay;
    public MapGenerator mapGenerator;
    public Transform player;

    [Header("Configuración")]
    public Vector2 minimapSize = new Vector2(200, 200);
    public float cameraHeight = 30f;

    private RenderTexture renderTexture;

    void Start()
    {
        CreateMinimapSystem();
    }

    void CreateMinimapSystem()
    {
        // 1. Crear Render Texture
        renderTexture = new RenderTexture(256, 256, 16);
        renderTexture.name = "MinimapRenderTexture";
        
        // 2. Configurar cámara del minimapa
        if (minimapCamera == null)
        {
            CreateMinimapCamera();
        }
        else
        {
            ConfigureMinimapCamera();
        }

        // 3. Configurar UI
        if (minimapDisplay == null)
        {
            CreateMinimapUI();
        }
        else
        {
            ConfigureMinimapUI();
        }

        Debug.Log("Sistema de minimapa creado");
    }

    void CreateMinimapCamera()
    {
        GameObject cameraObj = new GameObject("MinimapCamera");
        minimapCamera = cameraObj.AddComponent<Camera>();
        
        ConfigureMinimapCamera();
    }

    void ConfigureMinimapCamera()
    {
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = CalculateCameraSize();
        minimapCamera.transform.position = new Vector3(0, cameraHeight, 0);
        minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        minimapCamera.targetTexture = renderTexture;
        minimapCamera.cullingMask = LayerMask.GetMask("Default"); // Cambia esto si usas layers específicas
        minimapCamera.depth = 0;
    }

    void CreateMinimapUI()
    {
        // Crear Canvas
        GameObject canvasObj = new GameObject("MinimapCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Crear RawImage
        GameObject rawImageObj = new GameObject("MinimapDisplay");
        rawImageObj.transform.SetParent(canvas.transform);
        minimapDisplay = rawImageObj.AddComponent<RawImage>();
        
        ConfigureMinimapUI();
    }

    void ConfigureMinimapUI()
    {
        if (minimapDisplay != null)
        {
            minimapDisplay.texture = renderTexture;
            
            RectTransform rt = minimapDisplay.GetComponent<RectTransform>();
            rt.sizeDelta = minimapSize;
            
            // Posicionar en esquina superior derecha
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -10);
            
            // Agregar borde
            minimapDisplay.color = Color.white;
        }
    }

    float CalculateCameraSize()
    {
        if (mapGenerator != null)
        {
            return (mapGenerator.gridSize * mapGenerator.roomSpacing) / 2f;
        }
        return 25f;
    }

    void Update()
    {
        // Seguir al jugador si existe
        if (player != null && minimapCamera != null)
        {
            Vector3 newPos = player.position;
            newPos.y = cameraHeight;
            minimapCamera.transform.position = newPos;
        }
    }

    // Método para debug
    public void DebugMinimap()
    {
        Debug.Log("=== MINIMAP DEBUG ===");
        Debug.Log($"Cámara: {minimapCamera != null}");
        Debug.Log($"Render Texture: {renderTexture != null}");
        Debug.Log($"UI Display: {minimapDisplay != null}");
        
        if (minimapCamera != null)
        {
            Debug.Log($"Cámara posición: {minimapCamera.transform.position}");
            Debug.Log($"Cámara tamaño: {minimapCamera.orthographicSize}");
        }
    }
}