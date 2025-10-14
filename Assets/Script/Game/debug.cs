using UnityEngine;
using System.Collections.Generic;

public class MinimapDebugger : MonoBehaviour
{
    public Camera minimapCamera;
    public UnityEngine.UI.RawImage minimapDisplay;
    public MapGenerator mapGenerator;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMinimap();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugMinimap();
        }
    }

    void ToggleMinimap()
    {
        if (minimapDisplay != null)
        {
            minimapDisplay.enabled = !minimapDisplay.enabled;
            Debug.Log($"Minimap visible: {minimapDisplay.enabled}");
        }
    }

    void DebugMinimap()
    {
        Debug.Log("=== DEBUG MINIMAPA ===");
        
        // 1. Verificar cámara
        Debug.Log($"Cámara activa: {minimapCamera != null && minimapCamera.isActiveAndEnabled}");
        if (minimapCamera != null)
        {
            Debug.Log($"Target Texture: {minimapCamera.targetTexture}");
            Debug.Log($"Orthographic Size: {minimapCamera.orthographicSize}");
            Debug.Log($"Posición: {minimapCamera.transform.position}");
        }

        // 2. Verificar UI
        Debug.Log($"Raw Image: {minimapDisplay != null}");
        if (minimapDisplay != null)
        {
            Debug.Log($"Raw Image activo: {minimapDisplay.isActiveAndEnabled}");
            Debug.Log($"Texture asignada: {minimapDisplay.texture != null}");
        }

        // 3. Buscar tiles por nombre (sin usar tags)
        DebugMinimapTiles();
    }

    void DebugMinimapTiles()
    {
        // Buscar todos los objetos que empiecen con "Minimap_"
        List<GameObject> minimapTiles = new List<GameObject>();
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Minimap_"))
            {
                minimapTiles.Add(obj);
            }
        }

        Debug.Log($"Tiles del minimapa encontrados: {minimapTiles.Count}");
        
        foreach (GameObject tile in minimapTiles)
        {
            Debug.Log($"Tile: {tile.name} - Posición: {tile.transform.position} - Activo: {tile.activeInHierarchy}");
            
            // Verificar si tiene renderer y si es visible
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                Debug.Log($"  - Renderer: {renderer.enabled} - Material: {renderer.material.name}");
            }
            else
            {
                Debug.Log($"  - NO tiene Renderer component!");
            }
        }
    }
}