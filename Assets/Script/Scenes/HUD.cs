using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Elementos del HUD")]
    public TextMeshProUGUI roomCountText;
    public TextMeshProUGUI playerPositionText;
    public GameObject minimapDisplay;
    public Button pauseButton;
    
    [Header("Referencias")]
    public MapGenerator mapGenerator;
    
    private Transform player;
    private float updateTimer = 0f;
    private float updateInterval = 0.2f;

    private void Start()
    {
        // Configurar botón de pausa
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseGame);
        }
        
        // Buscar referencias si no están asignadas
        FindReferences();
        
        // Ocultar HUD al inicio (se mostrará cuando empiece el juego)
        gameObject.SetActive(false);
    }
    
    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateHUD();
            updateTimer = 0f;
        }
    }
    
    void FindReferences()
    {
        // Buscar MapGenerator si no está asignado
        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
        }
        
        // Buscar jugador a través del MapGenerator
        if (mapGenerator != null)
        {
            GameObject playerObj = mapGenerator.GetPlayer();
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        // Si todavía no encontramos al jugador, buscar por tag
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }
    
    void UpdateHUD()
    {
        // Si no tenemos referencias, intentar encontrarlas
        if (mapGenerator == null || player == null)
        {
            FindReferences();
            return;
        }
        
        // Actualizar información de habitaciones
        if (roomCountText != null)
        {
            try
            {
                roomCountText.text = $"Habitaciones: {mapGenerator.GetRoomCount()}";
            }
            catch (System.Exception e)
            {
                roomCountText.text = "Habitaciones: --";
                Debug.LogWarning("Error actualizando contador de habitaciones: " + e.Message);
            }
        }
        
        // Actualizar posición del jugador
        if (playerPositionText != null && player != null)
        {
            Vector3 pos = player.position;
            playerPositionText.text = $"Posición: ({pos.x:F0}, {pos.z:F0})";
        }
    }
    
    void PauseGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogWarning("GameManager no encontrado");
        }
    }
    
    // Método público para forzar actualización del HUD
    public void ForceHUDUpdate()
    {
        FindReferences();
        UpdateHUD();
    }
    
    // Método para cuando se activa/desactiva el HUD
    private void OnEnable()
    {
        ForceHUDUpdate();
    }
}