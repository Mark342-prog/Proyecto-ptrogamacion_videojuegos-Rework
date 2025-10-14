using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Configuración del Mapa")]
    public int gridSize = 10;
    public int numberOfRooms = 15;
    public GameObject[] roomPrefabs;
    public GameObject playerPrefab;
    public Transform mapParent;

    [Header("Tamaño de las Habitaciones")]
    public float roomSpacing = 10f;
    public bool autoCalculateSpacing = false;

    [Header("Referencias")]
    public ThirdPersonCamera thirdPersonCamera;
    public MinimapController minimapController;

    [Header("Minimap")]
    public GameObject minimapTilePrefab;
    public Transform minimapParent;
    public float minimapTileSize = 10f;

    [Header("Debug")]
    public bool debugMode = true;
    public bool forceRegenerateOnStart = false;

    private Dictionary<Vector2Int, GameObject> rooms = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> minimapTiles = new Dictionary<Vector2Int, GameObject>();
    private GameObject playerInstance;
    private Vector2Int playerRoomCoord;
    private bool spawnRoomCreated = false;

    void Start()
    {
        if (debugMode) Debug.Log("=== MAPGENERATOR INICIADO ===");
        
        if (forceRegenerateOnStart)
        {
            RegenerateMap();
        }
        else
        {
            // Calcular automáticamente el espaciado si está activado
            if (autoCalculateSpacing && roomPrefabs.Length > 0 && roomPrefabs[0] != null)
            {
                CalculateRoomSpacing();
            }
            
            GenerateMap();
            CreateMinimap();
            SpawnPlayer();
            AssignPlayerReferences();
        }
    }

    void CalculateRoomSpacing()
    {
        GameObject sampleRoom = roomPrefabs[0];
        float calculatedSize = CalculateObjectSize(sampleRoom);
        roomSpacing = calculatedSize * 1.2f;
        Debug.Log($"Espaciado automático calculado: {roomSpacing} (basado en prefab: {sampleRoom.name})");
    }

    float CalculateObjectSize(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);
        }
        
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            return Mathf.Max(collider.bounds.size.x, collider.bounds.size.z);
        }
        
        Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
            Bounds combinedBounds = childRenderers[0].bounds;
            for (int i = 1; i < childRenderers.Length; i++)
            {
                combinedBounds.Encapsulate(childRenderers[i].bounds);
            }
            return Mathf.Max(combinedBounds.size.x, combinedBounds.size.z);
        }
        
        Debug.LogWarning("No se pudo calcular el tamaño del prefab. Usando valor por defecto de 10 unidades.");
        return 10f;
    }

    void GenerateMap()
    {
        if (debugMode) Debug.Log("Iniciando generación de mapa...");
        
        spawnRoomCreated = false;
        
        // Verificar que tenemos prefabs de habitaciones
        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            Debug.LogError("❌ No hay Room Prefabs asignados en MapGenerator!");
            CreateEmergencyRooms();
            return;
        }

        // Limpiar mapa existente
        ClearExistingMap();

        // Generar nuevas habitaciones
        Vector2Int currentCoord = Vector2Int.zero;
        rooms.Add(currentCoord, CreateRoom(currentCoord, true));

        for (int i = 0; i < numberOfRooms - 1; i++)
        {
            currentCoord = GetNextValidCoordinate(currentCoord);
            rooms.Add(currentCoord, CreateRoom(currentCoord, false));
        }

        VerifySingleSpawnRoom();
        
        if (debugMode) Debug.Log($"✅ Mapa generado: {rooms.Count} habitaciones creadas");
    }

    void CreateEmergencyRooms()
    {
        Debug.LogWarning("Creando habitaciones de emergencia...");
        
        // Crear algunas habitaciones básicas
        for (int i = 0; i < 5; i++)
        {
            Vector2Int coord = new Vector2Int(i, 0);
            GameObject room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.name = $"EmergencyRoom_{i}";
            room.tag = "Room";
            room.transform.position = new Vector3(i * 10f, 0, 0);
            room.transform.localScale = new Vector3(8f, 4f, 8f);
            room.GetComponent<Renderer>().material.color = Color.blue;
            
            rooms.Add(coord, room);
        }
        
        // Marcar la primera como spawn
        if (rooms.ContainsKey(Vector2Int.zero))
        {
            rooms[Vector2Int.zero].tag = "SpawnRoom";
        }
    }

    void ClearExistingMap()
    {
        // Limpiar habitaciones existentes
        foreach (var room in rooms.Values)
        {
            if (room != null)
                DestroyImmediate(room);
        }
        rooms.Clear();
        
        // Limpiar tiles del minimapa
        foreach (var tile in minimapTiles.Values)
        {
            if (tile != null)
                DestroyImmediate(tile);
        }
        minimapTiles.Clear();
        
        // Limpiar jugador
        if (playerInstance != null)
        {
            DestroyImmediate(playerInstance);
            playerInstance = null;
        }
    }

    Vector2Int GetNextValidCoordinate(Vector2Int currentCoord)
    {
        List<Vector2Int> validDirections = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newCoord = currentCoord + dir;
            if (!rooms.ContainsKey(newCoord) && IsValidCoordinate(newCoord))
                validDirections.Add(dir);
        }

        if (validDirections.Count > 0)
            return currentCoord + validDirections[Random.Range(0, validDirections.Count)];
        
        return currentCoord;
    }

    bool IsValidCoordinate(Vector2Int coord)
    {
        return Mathf.Abs(coord.x) <= gridSize/2 && Mathf.Abs(coord.y) <= gridSize/2;
    }

    GameObject CreateRoom(Vector2Int coord, bool isSpawnRoom)
    {
        try
        {
            GameObject roomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];
            Vector3 position = new Vector3(coord.x * roomSpacing, 0, coord.y * roomSpacing);
            
            GameObject room = Instantiate(roomPrefab, position, Quaternion.identity);
            room.name = $"Room_{coord.x}_{coord.y}";
            room.tag = "Room";
            
            // Parentear al mapParent si existe
            if (mapParent != null)
            {
                room.transform.SetParent(mapParent);
            }
            
            if (isSpawnRoom && !spawnRoomCreated)
            {
                room.tag = "SpawnRoom";
                spawnRoomCreated = true;
                if (debugMode) Debug.Log($"📍 SpawnRoom creada en coordenada: {coord}");
            }
            else if (room.CompareTag("SpawnRoom"))
            {
                room.tag = "Room";
                if (debugMode) Debug.Log($"🔄 Removido tag SpawnRoom de habitación en {coord}");
            }
            
            if (debugMode) Debug.Log($"🏠 Habitación creada: {room.name} en posición {position}");
            return room;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error creando habitación en {coord}: {e.Message}");
            return null;
        }
    }

    void VerifySingleSpawnRoom()
    {
        int spawnRoomCount = 0;
        foreach (var room in rooms.Values)
        {
            if (room != null && room.CompareTag("SpawnRoom"))
            {
                spawnRoomCount++;
            }
        }
        
        if (spawnRoomCount == 0)
        {
            Debug.LogWarning("⚠️ No se encontró ninguna SpawnRoom. Asignando la primera habitación.");
            if (rooms.ContainsKey(Vector2Int.zero) && rooms[Vector2Int.zero] != null)
            {
                rooms[Vector2Int.zero].tag = "SpawnRoom";
            }
        }
        else if (spawnRoomCount > 1)
        {
            Debug.LogWarning($"⚠️ Se encontraron {spawnRoomCount} SpawnRooms. Corrigiendo...");
            bool firstFound = false;
            foreach (var room in rooms.Values)
            {
                if (room != null && room.CompareTag("SpawnRoom"))
                {
                    if (!firstFound)
                    {
                        firstFound = true;
                    }
                    else
                    {
                        room.tag = "Room";
                    }
                }
            }
        }
    }

    void CreateMinimap()
    {
        if (minimapTilePrefab == null)
        {
            Debug.LogError("❌ minimapTilePrefab no asignado!");
            return;
        }

        // Crear parent si no existe
        if (minimapParent == null)
        {
            GameObject parentObj = new GameObject("MinimapTiles");
            minimapParent = parentObj.transform;
            if (debugMode) Debug.Log("📁 MinimapParent creado automáticamente");
        }

        if (debugMode) Debug.Log($"🗺️ Creando minimapa con {rooms.Count} habitaciones...");

        foreach (KeyValuePair<Vector2Int, GameObject> room in rooms)
        {
            try
            {
                if (room.Value == null) continue;
                
                GameObject minimapTile = Instantiate(minimapTilePrefab, minimapParent);
                Vector3 position = new Vector3(room.Key.x * roomSpacing, 0.5f, room.Key.y * roomSpacing);
                minimapTile.transform.position = position;
                minimapTile.name = $"Minimap_{room.Key.x}_{room.Key.y}";
                
                SetupMinimapTile(minimapTile);
                minimapTiles.Add(room.Key, minimapTile);
                
                if (debugMode) Debug.Log($"📍 Tile de minimapa creado en posición: {position}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error creando tile: {e.Message}");
            }
        }

        if (debugMode) Debug.Log($"✅ Minimapa creado: {minimapTiles.Count} tiles");
    }

    void SetupMinimapTile(GameObject tile)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = tile.AddComponent<MeshRenderer>();
            MeshFilter filter = tile.AddComponent<MeshFilter>();
            filter.mesh = CreateQuadMesh();
        }

        if (renderer.material == null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
            renderer.material = material;
        }

        tile.transform.localScale = Vector3.one * (roomSpacing * 0.8f);
    }

    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        
        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        Vector2[] uv = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    void SpawnPlayer()
    {
        if (debugMode) Debug.Log("👤 Intentando generar jugador...");
        
        Vector2Int spawnCoord = FindSpawnRoom();
        if (rooms.ContainsKey(spawnCoord) && rooms[spawnCoord] != null)
        {
            Vector3 spawnPosition = rooms[spawnCoord].transform.position + Vector3.up * 2f; // Pequeña altura
            playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerInstance.name = "Player";
            playerRoomCoord = spawnCoord;
            
            SetupPlayerForThirdPerson();
            UpdateMinimapColors();
            
            if (debugMode) Debug.Log($"✅ Jugador generado en SpawnRoom: {spawnCoord} - Posición: {spawnPosition}");
        }
        else
        {
            Debug.LogError($"❌ No se pudo encontrar SpawnRoom válida. Coordenada: {spawnCoord}");
            CreateEmergencyPlayer();
        }
    }

    void CreateEmergencyPlayer()
    {
        Debug.LogWarning("🆘 Creando jugador de emergencia...");
        
        // Crear jugador básico en el centro
        Vector3 spawnPosition = Vector3.zero + Vector3.up * 2f;
        playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerInstance.name = "Player_Emergency";
        playerRoomCoord = Vector2Int.zero;
        
        SetupPlayerForThirdPerson();
        
        Debug.Log("✅ Jugador de emergencia creado");
    }

    void SetupPlayerForThirdPerson()
    {
        if (playerInstance != null)
        {
            // Asegurar que el jugador tenga CharacterController
            CharacterController controller = playerInstance.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = playerInstance.AddComponent<CharacterController>();
                controller.height = 2.0f;
                controller.radius = 0.3f;
                controller.slopeLimit = 45f;
                if (debugMode) Debug.Log("🎮 CharacterController agregado al jugador");
            }

            // Agregar PlayerController si no existe
            /*
            PlayerController playerController = playerInstance.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = playerInstance.AddComponent<PlayerController>();
                if (debugMode) Debug.Log("🎮 PlayerController agregado al jugador");
            }
            */

            // Configurar la cámara para el PlayerController
            /*
            if (playerController != null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    playerController.playerCamera = mainCamera;
                    if (debugMode) Debug.Log("📷 Cámara asignada al PlayerController");
                }
            }
            */
        }
    }

    Vector2Int FindSpawnRoom()
    {
        foreach (KeyValuePair<Vector2Int, GameObject> room in rooms)
        {
            if (room.Value != null && room.Value.CompareTag("SpawnRoom"))
                return room.Key;
        }
        
        // Fallback: usar la primera habitación disponible
        foreach (KeyValuePair<Vector2Int, GameObject> room in rooms)
        {
            if (room.Value != null)
                return room.Key;
        }
        
        return Vector2Int.zero;
    }

    void AssignPlayerReferences()
    {
        if (playerInstance != null)
        {
            if (thirdPersonCamera != null)
            {
                thirdPersonCamera.player = playerInstance.transform;
                if (debugMode) Debug.Log("📷 Jugador asignado a ThirdPersonCamera");
            }
            else
            {
                Debug.LogWarning("⚠️ ThirdPersonCamera no asignada en MapGenerator");
            }

            if (minimapController != null)
            {
                minimapController.player = playerInstance.transform;
                if (debugMode) Debug.Log("🗺️ Jugador asignado a MinimapController");
            }
        }
    }

    void UpdateMinimapColors()
    {
        foreach (KeyValuePair<Vector2Int, GameObject> tile in minimapTiles)
        {
            SpriteRenderer sr = tile.Value.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (tile.Key == playerRoomCoord)
                    sr.color = Color.green;
                else
                    sr.color = Color.white;
            }
        }
    }

    // === MÉTODOS PÚBLICOS ===

    public int GetRoomCount()
    {
        if (rooms == null) return 0;
        return rooms.Count;
    }

    public Vector2Int GetPlayerRoomCoord()
    {
        return playerRoomCoord;
    }

    public GameObject GetPlayer()
    {
        return playerInstance;
    }

    public void RegenerateMap()
    {
        if (debugMode) Debug.Log("🔄 Regenerando mapa...");
        
        ClearExistingMap();
        GenerateMap();
        CreateMinimap();
        SpawnPlayer();
        AssignPlayerReferences();
        
        if (debugMode) Debug.Log("✅ Mapa regenerado exitosamente");
    }

    public bool IsMapReady()
    {
        return rooms != null && rooms.Count > 0 && playerInstance != null;
    }

    public string GetMapInfo()
    {
        return $"🗺️ Mapa: {GetRoomCount()} habitaciones, 👤 Jugador en: {GetPlayerRoomCoord()}";
    }

    // Método para debug visual en el editor
    private void OnDrawGizmosSelected()
    {
        // Dibujar gizmos para las habitaciones
        Gizmos.color = Color.blue;
        foreach (var room in rooms.Values)
        {
            if (room != null)
            {
                Gizmos.DrawWireCube(room.transform.position, new Vector3(roomSpacing * 0.8f, 2f, roomSpacing * 0.8f));
            }
        }
        
        // Dibujar spawn room en verde
        Gizmos.color = Color.green;
        Vector2Int spawnCoord = FindSpawnRoom();
        if (rooms.ContainsKey(spawnCoord) && rooms[spawnCoord] != null)
        {
            Gizmos.DrawWireCube(rooms[spawnCoord].transform.position, new Vector3(roomSpacing, 3f, roomSpacing));
        }
    }
}