using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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

    [Header("Configuración de NPCs")]
    public GameObject[] npcPrefabs;
    public int npcPerRoom = 1;
    public float npcSpawnHeight = 1.0f;

    [Header("Muros automáticos")]
    public GameObject wallPrefab;
    [Header("Recogida de Munición")]
    public GameObject ammoPickupPrefab;
    public int ammoPickupsPerRoom = 2; 
    public float ammoSpawnHeight = 0.5f;

    [Header("Referencias")]
    public ThirdPersonCamera thirdPersonCamera;
    public MinimapController minimapController;

    [Header("Triggers")]
    public GameObject levelTriggerPrefab;

    [Header("Minimap")]
    public GameObject minimapTilePrefab;
    public Transform minimapParent;
    public float minimapTileSize = 10f;

    [Header("Debug")]
    public bool debugMode = true;
    public bool forceRegenerateOnStart = false;

    [Header("Navegación IA")]
    public NavMeshSurface navSurface;

    // Variables de dificultad persistente
    private static int currentDifficultyLevel = 1;
    private static bool difficultyInitialized = false;

    private Dictionary<Vector2Int, GameObject> rooms = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> minimapTiles = new Dictionary<Vector2Int, GameObject>();
    private List<GameObject> walls = new List<GameObject>();
    private List<GameObject> levelTriggers = new List<GameObject>();
    private GameObject playerInstance;
    private Vector2Int playerRoomCoord;
    private bool spawnRoomCreated = false;

    void Start()
    {
        if (debugMode) Debug.Log("=== MAPGENERATOR INICIADO ===");

        // Inicializar dificultad solo una vez
        if (!difficultyInitialized)
        {
            currentDifficultyLevel = 1;
            difficultyInitialized = true;
            if (debugMode) Debug.Log("Dificultad inicializada: Nivel " + currentDifficultyLevel);
        }

        // Aplicar dificultad actual a los parámetros del mapa
        ApplyDifficultySettings();

        if (forceRegenerateOnStart)
        {
            RegenerateMap();
        }
        else
        {
            GenerateMapComplete();
        }
    }
    void SpawnAmmoPickups()
{
    if (ammoPickupPrefab == null)
    {
        Debug.LogWarning("ammoPickupPrefab no asignado. No se generarán pickups de munición.");
        return;
    }

    Debug.Log("Generando ammo pickups en las habitaciones...");

    int ammoPickupsCreated = 0;
    int roomIndex = 0;

    foreach (var kvp in rooms)
    {
        GameObject room = kvp.Value;
        if (room == null) continue;

        // No spawnear en la habitación del jugador (primera habitación)
        if (roomIndex == 0)
        {
            roomIndex++;
            continue;
        }

        for (int i = 0; i < ammoPickupsPerRoom; i++)
        {
            Vector3 basePos = room.transform.position + new Vector3(
                Random.Range(-roomSpacing * 0.3f, roomSpacing * 0.3f),
                ammoSpawnHeight,
                Random.Range(-roomSpacing * 0.3f, roomSpacing * 0.3f)
            );

            // Verificar que la posición sea válida en el NavMesh
            NavMeshHit hit;
            float sampleRadius = Mathf.Max(2f, roomSpacing * 0.3f);
            if (NavMesh.SamplePosition(basePos, out hit, sampleRadius, NavMesh.AllAreas))
            {
                GameObject ammoPickup = Instantiate(ammoPickupPrefab, hit.position, Quaternion.identity);
                ammoPickup.name = $"AmmoPickup_{room.name}_{i}";
                
                // Asegurar que tenga collider de trigger
                Collider collider = ammoPickup.GetComponent<Collider>();
                if (collider == null)
                {
                    SphereCollider sphereCollider = ammoPickup.AddComponent<SphereCollider>();
                    sphereCollider.isTrigger = true;
                    sphereCollider.radius = 0.5f;
                }
                else
                {
                    collider.isTrigger = true;
                }

                // Rotación aleatoria para variedad visual
                ammoPickup.transform.Rotate(0, Random.Range(0, 360), 0);
                
                ammoPickupsCreated++;
                
                if (debugMode) 
                    Debug.Log($"Ammo pickup creado en habitación {room.name} en posición {hit.position}");
            }
        }

        roomIndex++;
    }

    Debug.Log($"Ammo pickups generados: {ammoPickupsCreated}");
}

    void ApplyDifficultySettings()
    {
        // Aumentar tamaño y complejidad según el nivel de dificultad
        gridSize = 10 + (currentDifficultyLevel * 2);
        numberOfRooms = 15 + (currentDifficultyLevel * 3);
        
        // Opcional: Aumentar NPCs por habitación cada 2 niveles
        npcPerRoom = 1 + (currentDifficultyLevel / 2);

        if (debugMode) 
        {
            Debug.Log("Configuración de dificultad aplicada:");
            Debug.Log("- Nivel: " + currentDifficultyLevel);
            Debug.Log("- Grid Size: " + gridSize);
            Debug.Log("- Numero de Habitaciones: " + numberOfRooms);
            Debug.Log("- NPCs por Habitación: " + npcPerRoom);
        }
    }

    void GenerateMapComplete()
    {
        if (autoCalculateSpacing && roomPrefabs.Length > 0 && roomPrefabs[0] != null)
        {
            CalculateRoomSpacing();
        }

        GenerateMap();
        CreateMinimap();
        SpawnPlayer();
        AssignPlayerReferences();

        FillEmptySpacesWithWalls();
        SpawnLevelTrigger();
        SpawnAmmoPickups();

        BuildNavMeshRuntime();
        ConnectAdjacentRooms();
        SpawnNPCsInRooms();

        Debug.Log("MAPA COMPLETAMENTE GENERADO - Nivel de Dificultad: " + currentDifficultyLevel);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugMapStatus();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("Forzando regeneración manual...");
            RegenerateMap();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("Forzando aumento de dificultad...");
            IncreaseDifficultyAndReloadScene();
        }

        // Tecla para reiniciar la dificultad
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ResetDifficulty();
            ReloadScene();
        }
    }

    void DebugMapStatus()
    {
        Debug.Log("DEBUG MAPA:");
        Debug.Log("- Nivel Dificultad: " + currentDifficultyLevel);
        Debug.Log("- Habitaciones: " + rooms.Count);
        Debug.Log("- Player Room Coord: " + playerRoomCoord);
        Debug.Log("- Player Instance: " + (playerInstance != null));
        Debug.Log("- Grid Size: " + gridSize);
        Debug.Log("- Numero de Habitaciones: " + numberOfRooms);
    }

    void SpawnLevelTrigger()
    {
        if (levelTriggerPrefab == null)
        {
            Debug.LogWarning("levelTriggerPrefab no asignado.");
            return;
        }

        if (rooms.Count == 0)
        {
            Debug.LogWarning("No hay habitaciones generadas para colocar el trigger.");
            return;
        }

        Vector2Int farCoord = FindFarthestRoomFromPlayer();

        if (!rooms.ContainsKey(farCoord) || rooms[farCoord] == null)
        {
            Debug.LogWarning("No se encontró una habitación válida para el trigger. Usando fallback...");
            
            foreach (var kvp in rooms)
            {
                if (kvp.Value != null)
                {
                    farCoord = kvp.Key;
                    break;
                }
            }
            
            if (!rooms.ContainsKey(farCoord))
            {
                Debug.LogError("No se pudo encontrar ninguna habitación válida para el trigger.");
                return;
            }
        }

        GameObject farRoom = rooms[farCoord];
        Vector3 pos = farRoom.transform.position + new Vector3(0f, 1f, 0f);

        GameObject trigger = Instantiate(levelTriggerPrefab, pos, Quaternion.identity);
        trigger.name = "LevelTrigger";

        Collider collider = trigger.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider boxCollider = trigger.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(3f, 3f, 3f);
        }
        else
        {
            collider.isTrigger = true;
        }

        LevelTrigger lt = trigger.GetComponent<LevelTrigger>();
        if (lt != null)
        {
            lt.mapGen = this;
            if (debugMode) Debug.Log("LevelTrigger configurado en habitación: " + farCoord);
        }
        else
        {
            Debug.LogError("El prefab LevelTrigger no tiene el componente LevelTrigger!");
        }

        levelTriggers.Add(trigger);

        Debug.Log("Trigger generado en la habitación más alejada: " + farCoord + " en " + pos);
    }

    Vector2Int FindFarthestRoomFromPlayer()
    {
        if (rooms.Count == 0)
        {
            Debug.LogWarning("No hay habitaciones disponibles para buscar la más lejana");
            return Vector2Int.zero;
        }

        Vector2Int referenceCoord = playerRoomCoord;
        if (!rooms.ContainsKey(referenceCoord))
        {
            foreach (var kvp in rooms)
            {
                referenceCoord = kvp.Key;
                break;
            }
            Debug.LogWarning("Usando coordenada de referencia: " + referenceCoord + " porque playerRoomCoord no es válida");
        }

        float maxDistance = -1f;
        Vector2Int farthestRoom = referenceCoord;

        foreach (var kvp in rooms)
        {
            float dist = Vector2Int.Distance(referenceCoord, kvp.Key);
            if (dist > maxDistance)
            {
                maxDistance = dist;
                farthestRoom = kvp.Key;
            }
        }

        if (debugMode) Debug.Log("Habitación más lejana encontrada: " + farthestRoom + " (distancia: " + maxDistance + ")");
        return farthestRoom;
    }
        private int GetCurrentGameScore()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.CurrentScore;
        }
        return 0;
    }
    public void CompleteGameReset()
    {
        currentDifficultyLevel = 1;
        difficultyInitialized = true;
        PlayerPrefs.DeleteKey("EnemyDifficulty");
        Debug.Log("Juego completamente reiniciado - Dificultad: 1");
    }

    public void IncreaseDifficultyAndReloadScene()
    {
        currentDifficultyLevel++;
        Debug.Log($"Aumentando dificultad a nivel: {currentDifficultyLevel}. Puntuación actual: {GetCurrentGameScore()}");
        
        // Guardar dificultad para persistencia
        PlayerPrefs.SetInt("EnemyDifficulty", currentDifficultyLevel);
        PlayerPrefs.Save();
        
        ReloadScene();
    }

    public void ResetDifficulty()
    {
        currentDifficultyLevel = 1;
        difficultyInitialized = true;
        Debug.Log("Dificultad reiniciada a nivel: " + currentDifficultyLevel);
    }

    void ReloadScene()
    {
        Debug.Log("Recargando escena con nueva dificultad...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    void FillEmptySpacesWithWalls()
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning("wallPrefab no asignado. Creando muros de emergencia...");
            CreateEmergencyWalls();
            return;
        }

        Debug.Log("Rellenando espacios vacíos con muros. Grid: " + gridSize + ", Espaciado: " + roomSpacing);

        int wallsCreated = 0;
        int half = gridSize / 2;

        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);

                if (rooms.ContainsKey(coord))
                    continue;

                Vector3 pos = new Vector3(x * roomSpacing, 12.5f, y * roomSpacing);

                GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
                wall.name = "Wall_" + x + "_" + y;

                if (mapParent != null)
                    wall.transform.SetParent(mapParent);

                EnsureWallVisibility(wall);

                walls.Add(wall);
                wallsCreated++;
                
                if (debugMode && wallsCreated <= 10)
                    Debug.Log("Muro creado en " + coord + " en posición " + pos);
            }
        }

        Debug.Log("Relleno completado: " + wallsCreated + " muros creados");
    }

    void EnsureWallVisibility(GameObject wall)
    {
        Renderer renderer = wall.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = wall.AddComponent<MeshRenderer>();
            MeshFilter filter = wall.AddComponent<MeshFilter>();
            filter.mesh = CreateCubeMesh();
            
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.red;
            renderer.material = material;
            
            Debug.LogWarning("Muro " + wall.name + " no tenía renderer. Se agregó uno automáticamente.");
        }

        if (wall.GetComponent<Collider>() == null)
        {
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            // Tamaño del collider igual a roomSpacing en X y Z, 25 en Y
            collider.size = new Vector3(roomSpacing, 25f, roomSpacing);
        }

        // Escala del muro igual a roomSpacing en X y Z, 25 en Y
        wall.transform.localScale = new Vector3(roomSpacing, 25f, roomSpacing);

        wall.layer = LayerMask.NameToLayer("Default");
    }

    void CreateEmergencyWalls()
    {
        Debug.LogWarning("Creando muros de emergencia...");

        int wallsCreated = 0;
        int half = gridSize / 2;

        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);

                if (rooms.ContainsKey(coord))
                    continue;

                Vector3 pos = new Vector3(x * roomSpacing, 12.5f, y * roomSpacing);

                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "EmergencyWall_" + x + "_" + y;
                wall.transform.position = pos;
                // Tamaño del muro de emergencia igual a roomSpacing en X y Z, 25 en Y
                wall.transform.localScale = new Vector3(roomSpacing, 25f, roomSpacing);

                Renderer renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = Color.red;

                if (mapParent != null)
                    wall.transform.SetParent(mapParent);

                walls.Add(wall);
                wallsCreated++;
            }
        }

        Debug.Log("Muros de emergencia creados: " + wallsCreated);
    }

    Mesh CreateCubeMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f)
        };

        int[] triangles = {
            0, 2, 1, 0, 3, 2,
            2, 3, 4, 2, 4, 5,
            1, 2, 5, 1, 5, 6,
            0, 7, 4, 0, 4, 3,
            5, 4, 7, 5, 7, 6,
            0, 1, 6, 0, 6, 7
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    void CalculateRoomSpacing()
    {
        if (roomPrefabs.Length == 0 || roomPrefabs[0] == null)
        {
            Debug.LogWarning("No hay roomPrefabs para calcular espaciado.");
            return;
        }

        GameObject sampleRoom = roomPrefabs[0];
        float calculatedSize = CalculateObjectSize(sampleRoom);
        roomSpacing = calculatedSize * 1.2f;
        if (debugMode) Debug.Log("Espaciado automático calculado: " + roomSpacing + " (basado en prefab: " + sampleRoom.name + ")");
    }

    float CalculateObjectSize(GameObject obj)
    {
        if (obj == null) return 10f;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            return Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z);

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            return Mathf.Max(collider.bounds.size.x, collider.bounds.size.z);

        Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
            Bounds combinedBounds = childRenderers[0].bounds;
            for (int i = 1; i < childRenderers.Length; i++)
                combinedBounds.Encapsulate(childRenderers[i].bounds);
            return Mathf.Max(combinedBounds.size.x, combinedBounds.size.z);
        }

        if (debugMode) Debug.LogWarning("No se pudo calcular el tamaño del prefab. Usando valor por defecto de 10 unidades.");
        return 10f;
    }

    void GenerateMap()
    {
        if (debugMode) Debug.Log("Iniciando generación de mapa...");

        spawnRoomCreated = false;

        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            Debug.LogError("No hay Room Prefabs asignados en MapGenerator!");
            CreateEmergencyRooms();
            return;
        }

        foreach (var prefab in roomPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("Uno o más roomPrefabs están NULL!");
                CreateEmergencyRooms();
                return;
            }
        }

        ClearExistingMap();

        Vector2Int currentCoord = Vector2Int.zero;
        GameObject firstRoom = CreateRoom(currentCoord, true);
        if (firstRoom != null)
        {
            rooms.Add(currentCoord, firstRoom);
            Debug.Log("Primera habitación creada en " + currentCoord);
        }
        else
        {
            Debug.LogError("Falló la creación de la primera habitación!");
            CreateEmergencyRooms();
            return;
        }
        GameObject[] existingAmmoPickups = GameObject.FindGameObjectsWithTag("AmmoPickup");
        foreach (GameObject ammoPickup in existingAmmoPickups)
        {
            if (ammoPickup != null)
                DestroyImmediate(ammoPickup);
        }

        for (int i = 0; i < numberOfRooms - 1; i++)
        {
            currentCoord = GetNextValidCoordinate(currentCoord);
            if (!rooms.ContainsKey(currentCoord))
            {
                GameObject room = CreateRoom(currentCoord, false);
                if (room != null)
                {
                    rooms.Add(currentCoord, room);
                    if (debugMode) Debug.Log("Habitación " + (i+1) + " creada en " + currentCoord);
                }
            }
        }

        Debug.Log("Mapa generado: " + rooms.Count + " habitaciones creadas");
    }

    GameObject CreateRoom(Vector2Int coord, bool isSpawnRoom)
    {
        try
        {
            if (roomPrefabs.Length == 0)
            {
                Debug.LogError("No hay roomPrefabs disponibles");
                return null;
            }

            GameObject roomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];
            if (roomPrefab == null)
            {
                Debug.LogError("Room prefab seleccionado es NULL");
                return null;
            }

            Vector3 position = new Vector3(coord.x * roomSpacing, 0f, coord.y * roomSpacing);

            GameObject room = Instantiate(roomPrefab, position, Quaternion.identity);
            if (room == null)
            {
                Debug.LogError("Fallo al instanciar la habitación");
                return null;
            }

            room.name = "Room_" + coord.x + "_" + coord.y;

            if (room.GetComponent<Collider>() == null)
            {
                var bc = room.AddComponent<BoxCollider>();
                Renderer r = room.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    Vector3 localCenter = r.bounds.center - room.transform.position;
                    bc.center = localCenter;
                    bc.size = new Vector3(r.bounds.size.x, Mathf.Max(0.5f, r.bounds.size.y), r.bounds.size.z);
                }
            }

            int targetLayer = LayerMask.NameToLayer("Default");
            if (targetLayer != -1)
                room.layer = targetLayer;

            if (mapParent != null)
            {
                room.transform.SetParent(mapParent);
            }

            if (isSpawnRoom && !spawnRoomCreated)
            {
                spawnRoomCreated = true;
                if (debugMode) Debug.Log("SpawnRoom creada en coordenada: " + coord);
            }

            if (debugMode) Debug.Log("Habitación creada: " + room.name + " en posición " + position);
            return room;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error creando habitación en " + coord + ": " + e.Message);
            return null;
        }
    }

    void CreateEmergencyRooms()
    {
        Debug.LogWarning("Creando habitaciones de emergencia...");

        for (int i = 0; i < 5; i++)
        {
            Vector2Int coord = new Vector2Int(i, 0);
            GameObject room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.name = "EmergencyRoom_" + i;
            room.transform.position = new Vector3(i * 10f, 0, 0);
            room.transform.localScale = new Vector3(8f, 4f, 8f);
            
            Renderer renderer = room.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.blue;

            rooms.Add(coord, room);
        }

        Debug.Log("Habitaciones de emergencia creadas");
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

        for (int x = -gridSize/2; x <= gridSize/2; x++)
        {
            for (int y = -gridSize/2; y <= gridSize/2; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (!rooms.ContainsKey(coord) && IsValidCoordinate(coord))
                    return coord;
            }
        }

        return currentCoord;
    }

    bool IsValidCoordinate(Vector2Int coord)
    {
        return Mathf.Abs(coord.x) <= gridSize / 2 && Mathf.Abs(coord.y) <= gridSize / 2;
    }

    void ClearExistingMap()
    {
        foreach (var room in rooms.Values)
        {
            if (room != null)
                DestroyImmediate(room);
        }
        rooms.Clear();

        foreach (var tile in minimapTiles.Values)
        {
            if (tile != null)
                DestroyImmediate(tile);
        }
        minimapTiles.Clear();

        foreach (GameObject wall in walls)
        {
            if (wall != null)
                DestroyImmediate(wall);
        }
        walls.Clear();

        foreach (GameObject trigger in levelTriggers)
        {
            if (trigger != null)
                DestroyImmediate(trigger);
        }
        levelTriggers.Clear();

        if (playerInstance != null)
        {
            DestroyImmediate(playerInstance);
            playerInstance = null;
        }

        if (navSurface != null)
        {
            DestroyImmediate(navSurface.gameObject);
            navSurface = null;
        }

        Debug.Log("Mapa anterior limpiado");
    }

    public void BuildNavMeshRuntime()
    {
        if (navSurface != null)
        {
            DestroyImmediate(navSurface.gameObject);
        }

        GameObject surfaceObj = new GameObject("RuntimeNavMeshSurface");
        navSurface = surfaceObj.AddComponent<NavMeshSurface>();

        navSurface.collectObjects = CollectObjects.All;
        navSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        navSurface.layerMask = LayerMask.GetMask("Default"); 
        
        navSurface.BuildNavMesh();

        if (debugMode) Debug.Log("NavMesh generado en tiempo de ejecución");
    }

    void ConnectAdjacentRooms()
    {
        int linksCreated = 0;
        foreach (var kvp in rooms)
        {
            Vector2Int coord = kvp.Key;
            GameObject currentRoom = kvp.Value;

            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborCoord = coord + dir;
                if (!rooms.ContainsKey(neighborCoord)) continue;

                GameObject neighborRoom = rooms[neighborCoord];
                if (neighborRoom == null) continue;

                string linkName = "Link_" + coord.x + "_" + coord.y + "_to_" + neighborCoord.x + "_" + neighborCoord.y;
                if (GameObject.Find(linkName) != null) continue;

                Vector3 start = currentRoom.transform.position;
                Vector3 end = neighborRoom.transform.position;
                Vector3 mid = (start + end) / 2f;
                Vector3 direction = (end - start).normalized;

                GameObject linkObj = new GameObject(linkName);
                var link = linkObj.AddComponent<NavMeshLink>();

                link.startPoint = -direction * (roomSpacing * 0.4f); 
                link.endPoint = direction * (roomSpacing * 0.4f);
                link.width = Mathf.Max(1f, roomSpacing * 0.3f);
                link.bidirectional = true;
                link.autoUpdate = true;

                linkObj.transform.position = mid + Vector3.up * 0.1f;
                linkObj.transform.SetParent(mapParent != null ? mapParent : transform);

                linksCreated++;
            }
        }

        if (debugMode) Debug.Log(linksCreated + " NavMesh links creados");
    }

    void CreateMinimap()
    {
        if (minimapTilePrefab == null)
        {
            if (debugMode) Debug.LogError("minimapTilePrefab no asignado!");
            return;
        }

        if (minimapParent == null)
        {
            GameObject parentObj = new GameObject("MinimapTiles");
            minimapParent = parentObj.transform;
            if (debugMode) Debug.Log("MinimapParent creado automáticamente");
        }

        if (debugMode) Debug.Log("Creando minimapa con " + rooms.Count + " habitaciones...");

        foreach (KeyValuePair<Vector2Int, GameObject> room in rooms)
        {
            try
            {
                if (room.Value == null) continue;

                GameObject minimapTile = Instantiate(minimapTilePrefab, minimapParent);
                Vector3 position = new Vector3(room.Key.x * roomSpacing, 0.5f, room.Key.y * roomSpacing);
                minimapTile.transform.position = position;
                minimapTile.name = "Minimap_" + room.Key.x + "_" + room.Key.y;

                SetupMinimapTile(minimapTile);
                minimapTiles.Add(room.Key, minimapTile);

                if (debugMode) Debug.Log("Tile de minimapa creado en posición: " + position);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error creando tile: " + e.Message);
            }
        }

        if (debugMode) Debug.Log("Minimapa creado: " + minimapTiles.Count + " tiles");
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

        tile.transform.localScale = Vector3.one * roomSpacing;
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
        if (debugMode) Debug.Log("Intentando generar jugador...");

        if (playerPrefab == null)
        {
            Debug.LogError("playerPrefab no asignado!");
            CreateEmergencyPlayer();
            return;
        }

        Vector2Int spawnCoord = Vector2Int.zero;
        if (rooms.ContainsKey(spawnCoord) && rooms[spawnCoord] != null)
        {
            Vector3 spawnPosition = rooms[spawnCoord].transform.position + Vector3.up * 2f;
            playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerInstance.name = "Player";
            playerRoomCoord = spawnCoord;

            SetupPlayerForThirdPerson();
            UpdateMinimapColors();

            if (debugMode) Debug.Log("Jugador generado en posición: " + spawnPosition);
        }
        else
        {
            Debug.LogError("No se pudo encontrar habitación válida para spawn. Coordenada: " + spawnCoord);
            CreateEmergencyPlayer();
        }
    }

    void CreateEmergencyPlayer()
    {
        Debug.LogWarning("Creando jugador de emergencia...");

        Vector3 spawnPosition = Vector3.zero + Vector3.up * 2f;
        
        if (playerPrefab != null)
        {
            playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            playerInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerInstance.transform.position = spawnPosition;
            playerInstance.name = "Player_Emergency";
            playerInstance.AddComponent<CharacterController>();
        }
        
        playerInstance.name = "Player_Emergency";
        playerRoomCoord = Vector2Int.zero;

        SetupPlayerForThirdPerson();

        Debug.Log("Jugador de emergencia creado");
    }

    void SetupPlayerForThirdPerson()
    {
        if (playerInstance != null)
        {
            CharacterController controller = playerInstance.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = playerInstance.AddComponent<CharacterController>();
                controller.height = 2.0f;
                controller.radius = 0.3f;
                controller.slopeLimit = 45f;
                if (debugMode) Debug.Log("CharacterController agregado al jugador");
            }
        }
    }

    void AssignPlayerReferences()
    {
        if (playerInstance != null)
        {
            if (thirdPersonCamera != null)
            {
                thirdPersonCamera.player = playerInstance.transform;
                if (debugMode) Debug.Log("Jugador asignado a ThirdPersonCamera");
            }
            else
            {
                if (debugMode) Debug.LogWarning("ThirdPersonCamera no asignada en MapGenerator");
            }

            if (minimapController != null)
            {
                minimapController.player = playerInstance.transform;
                if (debugMode) Debug.Log("Jugador asignado a MinimapController");
            }
        }
    }

    void UpdateMinimapColors()
    {
        foreach (KeyValuePair<Vector2Int, GameObject> tile in minimapTiles)
        {
            Renderer renderer = tile.Value.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (tile.Key == playerRoomCoord)
                    renderer.material.color = Color.green;
                else
                    renderer.material.color = Color.white;
            }
        }
    }

    public void SpawnNPCsInRooms()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            if (debugMode) Debug.LogWarning("No hay prefabs de NPC asignados en MapGenerator.");
            return;
        }
        

        if (rooms == null || rooms.Count == 0)
        {
            if (debugMode) Debug.LogWarning("No hay habitaciones generadas para colocar NPCs.");
            return;
        }

        int npcsSpawned = 0;
        int roomIndex = 0;

        foreach (var kvp in rooms)
        {
            GameObject room = kvp.Value;
            if (room == null) continue;

            if (roomIndex == 0)
            {
                roomIndex++;
                continue;
            }

            GameObject npcPrefab = npcPrefabs[roomIndex % npcPrefabs.Length];

            for (int i = 0; i < npcPerRoom; i++)
            {
                Vector3 basePos = room.transform.position + new Vector3(
                    Random.Range(-roomSpacing * 0.4f, roomSpacing * 0.4f),
                    npcSpawnHeight,
                    Random.Range(-roomSpacing * 0.4f, roomSpacing * 0.4f)
                );

                NavMeshHit hit;
                float sampleRadius = Mathf.Max(3f, roomSpacing * 0.5f);
                if (NavMesh.SamplePosition(basePos, out hit, sampleRadius, NavMesh.AllAreas))
                {
                    GameObject npcInstance = Instantiate(npcPrefab, hit.position, Quaternion.identity);
                    npcInstance.name = "NPC_" + room.name + "_" + i;

                    NavMeshAgent agent = npcInstance.GetComponent<NavMeshAgent>();
                    if (agent == null)
                    {
                        agent = npcInstance.AddComponent<NavMeshAgent>();
                        agent.speed = 3.5f;
                        agent.angularSpeed = 180f;
                        agent.acceleration = 8f;
                        agent.baseOffset = 0f;
                    }

                    npcsSpawned++;
                }
            }

            roomIndex++;
        }

        if (debugMode) Debug.Log(npcsSpawned + " NPCs generados correctamente");
    }

    public void RegenerateMap()
    {
        if (debugMode) Debug.Log("Regenerando mapa...");

        try
        {
            ClearExistingMap();
            GenerateMapComplete();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error durante la regeneración del mapa: " + e.Message);
        }
    }

    public bool IsMapReady()
    {
        return rooms != null && rooms.Count > 0 && playerInstance != null;
    }

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

    public string GetMapInfo()
    {
        return "Mapa: " + GetRoomCount() + " habitaciones, Jugador en: " + GetPlayerRoomCoord() + ", Dificultad: " + currentDifficultyLevel;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        foreach (var room in rooms.Values)
        {
            if (room != null)
            {
                Gizmos.DrawWireCube(room.transform.position, new Vector3(roomSpacing, 2f, roomSpacing));
            }
        }

        Gizmos.color = Color.red;
        foreach (var wall in walls)
        {
            if (wall != null)
            {
                Gizmos.DrawWireCube(wall.transform.position, new Vector3(roomSpacing, 25f, roomSpacing));
            }
        }

        Gizmos.color = Color.green;
        if (rooms.ContainsKey(Vector2Int.zero) && rooms[Vector2Int.zero] != null)
        {
            Gizmos.DrawWireCube(rooms[Vector2Int.zero].transform.position, new Vector3(roomSpacing, 3f, roomSpacing));
        }
    }
}