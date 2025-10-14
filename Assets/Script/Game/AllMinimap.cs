using UnityEngine;
using UnityEngine.UI;

public class CompleteMinimap : MonoBehaviour
{
    [Header("Referencias")]
    public MapGenerator mapGenerator;
    public GameObject minimapTilePrefab;

    [Header("Configuración")]
    public int renderTextureSize = 256;
    public Vector2 minimapSize = new Vector2(200, 200);

    private Camera minimapCamera;
    private RawImage minimapDisplay;
    private RenderTexture renderTexture;

    void Start()
    {
        Invoke("SetupCompleteMinimap", 0.5f);
    }

    void SetupCompleteMinimap()
    {
        Debug.Log("Iniciando configuración completa del minimapa...");

        CreateMinimapCamera();
        
        // 2. Crear UI del minimapa
        CreateMinimapUI();
        
        // 3. Forzar creación de tiles del minimapa
        ForceCreateMinimapTiles();
        
        Debug.Log("Configuración del minimapa completada");
    }

    void CreateMinimapCamera()
    {
        GameObject cameraObj = new GameObject("MinimapCamera");
        minimapCamera = cameraObj.AddComponent<Camera>();
        
        // Configurar cámara
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = 25f; // Ajustar según el tamaño del mapa
        minimapCamera.transform.position = new Vector3(0, 50, 0);
        minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        minimapCamera.depth = 0;
        
        // Crear Render Texture
        renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
        minimapCamera.targetTexture = renderTexture;
        
        Debug.Log("Cámara del minimapa creada");
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
        minimapDisplay.texture = renderTexture;
        
        // Configurar posición y tamaño
        RectTransform rt = minimapDisplay.rectTransform;
        rt.sizeDelta = minimapSize;
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-10, -10);
        
        // Agregar borde
        minimapDisplay.color = Color.white;
        
        Debug.Log("UI del minimapa creada");
    }

    void ForceCreateMinimapTiles()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator no asignado!");
            return;
        }

        // Crear contenedor para tiles
        GameObject minimapTilesContainer = new GameObject("MinimapTilesContainer");
        
        // Calcular tamaño del mapa
        float mapSize = mapGenerator.gridSize * mapGenerator.roomSpacing;
        
        // Crear tiles manualmente
        for (int x = -mapGenerator.gridSize/2; x <= mapGenerator.gridSize/2; x++)
        {
            for (int y = -mapGenerator.gridSize/2; y <= mapGenerator.gridSize/2; y++)
            {
                CreateMinimapTile(x, y, minimapTilesContainer.transform);
            }
        }
        
        Debug.Log($"Tiles del minimapa creados: {minimapTilesContainer.transform.childCount}");
    }

    void CreateMinimapTile(int x, int y, Transform parent)
    {
        if (minimapTilePrefab == null)
        {
            CreateEmergencyTile(x, y, parent);
            return;
        }

        GameObject tile = Instantiate(minimapTilePrefab, parent);
        tile.name = $"Minimap_{x}_{y}";
        
        // Posición en el mundo 3D
        Vector3 position = new Vector3(x * mapGenerator.roomSpacing, 1f, y * mapGenerator.roomSpacing);
        tile.transform.position = position;
        
        // Configurar para que sea visible
        SetupTileVisibility(tile);
    }

    void CreateEmergencyTile(int x, int y, Transform parent)
    {
        // Crear tile de emergencia
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = $"Minimap_{x}_{y}";
        tile.transform.SetParent(parent);
        
        // Posición
        Vector3 position = new Vector3(x * mapGenerator.roomSpacing, 1f, y * mapGenerator.roomSpacing);
        tile.transform.position = position;
        
        // Hacerlo visible
        tile.transform.localScale = Vector3.one * (mapGenerator.roomSpacing * 0.8f);
        tile.GetComponent<Renderer>().material.color = new Color(1, 0.5f, 0.5f); // Color naranja
        
        Debug.Log($"Tile de emergencia creado en {position}");
    }

    void SetupTileVisibility(GameObject tile)
    {
        // Asegurar que tenga renderer
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = tile.AddComponent<MeshRenderer>();
            MeshFilter filter = tile.AddComponent<MeshFilter>();
            filter.mesh = CreateSimpleQuad();
        }

        // Material blanco básico
        if (renderer.material == null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
            renderer.material = material;
        }

        // Escala adecuada
        tile.transform.localScale = Vector3.one * (mapGenerator.roomSpacing * 0.8f);
        
        // Asegurar que esté activo
        tile.SetActive(true);
    }

    Mesh CreateSimpleQuad()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    void Update()
    {
        // Seguir al jugador si existe
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && minimapCamera != null)
        {
            Vector3 playerPos = player.transform.position;
            minimapCamera.transform.position = new Vector3(playerPos.x, 50, playerPos.z);
        }
    }
}