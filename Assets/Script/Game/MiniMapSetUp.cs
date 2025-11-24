using UnityEngine;
using UnityEngine.UI;

public class MinimapSetup : MonoBehaviour
{
    [Header("Referencias Principales")]
    public Camera minimapCamera;
    public RawImage minimapDisplay;
    public MapGenerator mapGenerator;

    [Header("Configuración")]
    public int renderTextureSize = 256;
    public LayerMask minimapLayers = -1; 

    void Start()
    {
        SetupMinimap();
    }

    void SetupMinimap()
    {
        // 1. Crear Render Texture si no existe
        if (minimapCamera.targetTexture == null)
        {
            CreateRenderTexture();
        }

        // 2. Configurar cámara del minimapa
        ConfigureMinimapCamera();

        // 3. Configurar UI del minimapa
        ConfigureMinimapUI();

        Debug.Log("Minimapa configurado correctamente");
    }

    void CreateRenderTexture()
    {
        RenderTexture renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
        renderTexture.name = "MinimapRenderTexture";
        renderTexture.Create();
        minimapCamera.targetTexture = renderTexture;
    }

    void ConfigureMinimapCamera()
    {
        if (minimapCamera != null)
        {
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = CalculateCameraSize();
            minimapCamera.cullingMask = minimapLayers;
            minimapCamera.transform.position = new Vector3(0, 50, 0);
            minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            minimapCamera.gameObject.SetActive(true);
        }
    }

    void ConfigureMinimapUI()
    {
        if (minimapDisplay != null && minimapCamera.targetTexture != null)
        {
            minimapDisplay.texture = minimapCamera.targetTexture;
            minimapDisplay.rectTransform.sizeDelta = new Vector2(200, 200);
            
            // Posicionar en esquina superior derecha
            minimapDisplay.rectTransform.anchorMin = new Vector2(1, 1);
            minimapDisplay.rectTransform.anchorMax = new Vector2(1, 1);
            minimapDisplay.rectTransform.pivot = new Vector2(1, 1);
            minimapDisplay.rectTransform.anchoredPosition = new Vector2(-10, -10);
        }
    }

    float CalculateCameraSize()
    {
        if (mapGenerator != null)
        {
            return (mapGenerator.gridSize * mapGenerator.roomSpacing) / 2f;
        }
        return 50f; // Valor por defecto
    }

    // Método para debuggear el minimapa
    public void DebugMinimap()
    {
        Debug.Log($"Cámara activa: {minimapCamera.isActiveAndEnabled}");
        Debug.Log($"Render Texture: {minimapCamera.targetTexture}");
        Debug.Log($"UI Display: {minimapDisplay != null}");
        Debug.Log($"Texture en UI: {minimapDisplay.texture != null}");
        Debug.Log($"Cámara posición: {minimapCamera.transform.position}");
        Debug.Log($"Cámara tamaño: {minimapCamera.orthographicSize}");
    }
}