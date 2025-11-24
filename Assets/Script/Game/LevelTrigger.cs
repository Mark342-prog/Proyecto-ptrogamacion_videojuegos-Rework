using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    [Header("Configuración")]
    public MapGenerator mapGen;
    public bool useAutoDifficulty = true;
    
    [Header("Configuración Manual")]
    public int enemiesToAdd = 2;
    public float reloadDelay = 1f;
    
    void Start()
    {
        // Buscar MapGenerator automáticamente si no está asignado
        if (mapGen == null)
        {
            mapGen = FindObjectOfType<MapGenerator>();
            if (mapGen != null)
            {
                Debug.Log("MapGenerator encontrado automáticamente");
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(3f, 3f, 3f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("LevelTrigger activado por el jugador");
            
            // Verificar que GameManager existe
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance es NULL!");
                return;
            }
            
            // Mostrar información del juego
            Debug.Log($"Puntuación actual: {GameManager.Instance.CurrentScore} - Munición: {GameManager.Instance.CurrentAmmo}");
            
            // Manejar la transición de nivel
            HandleLevelTransition();
            
            // Desactivar el trigger para evitar múltiples activaciones
            gameObject.SetActive(false);
        }
    }

    private void HandleLevelTransition()
    {
        if (mapGen != null)
        {
            Debug.Log("Usando MapGenerator para aumentar dificultad...");
            mapGen.IncreaseDifficultyAndReloadScene();
        }
        else
        {
            Debug.LogError("MapGenerator no asignado y no se pudo encontrar en la escena!");
            // Sistema de emergencia
            EmergencyLevelTransition();
        }
    }

    private void EmergencyLevelTransition()
    {
        Debug.Log("Usando transición de nivel de emergencia...");
        
        // Incrementar dificultad en PlayerPrefs como respaldo
        int currentDifficulty = PlayerPrefs.GetInt("EnemyDifficulty", 1);
        currentDifficulty++;
        PlayerPrefs.SetInt("EnemyDifficulty", currentDifficulty);
        PlayerPrefs.Save();
        
        Debug.Log($"Dificultad aumentada a: {currentDifficulty}. Recargando escena...");
        Invoke("ReloadScene", reloadDelay);
    }

    private void ReloadScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}