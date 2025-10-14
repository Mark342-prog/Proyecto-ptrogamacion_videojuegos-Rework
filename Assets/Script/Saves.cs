using UnityEngine;

public class BasicSaveSystem : MonoBehaviour
{
    public static BasicSaveSystem Instance;
    
    // Datos del juego a guardar
    public int nivel;
    public float salud;
    public int monedas;
    public Vector3 posicionJugador;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GuardarDatos()
    {
        PlayerPrefs.SetInt("Nivel", nivel);
        PlayerPrefs.SetFloat("Salud", salud);
        PlayerPrefs.SetInt("Monedas", monedas);
        
        // Guardar posición (Vector3)
        PlayerPrefs.SetFloat("PosX", posicionJugador.x);
        PlayerPrefs.SetFloat("PosY", posicionJugador.y);
        PlayerPrefs.SetFloat("PosZ", posicionJugador.z);
        
        PlayerPrefs.Save();
        Debug.Log("Datos guardados!");
    }

    public void CargarDatos()
    {
        nivel = PlayerPrefs.GetInt("Nivel", 1); // Valor por defecto 1
        salud = PlayerPrefs.GetFloat("Salud", 100f);
        monedas = PlayerPrefs.GetInt("Monedas", 0);
        
        // Cargar posición
        float x = PlayerPrefs.GetFloat("PosX", 0f);
        float y = PlayerPrefs.GetFloat("PosY", 0f);
        float z = PlayerPrefs.GetFloat("PosZ", 0f);
        posicionJugador = new Vector3(x, y, z);
        
        Debug.Log("Datos cargados!");
    }

    public void BorrarDatos()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Datos borrados!");
    }
}