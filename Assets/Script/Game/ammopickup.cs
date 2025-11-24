using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Configuración")]
    public int ammoAmount = 10;
    public AudioClip pickupSound;
    public GameObject pickupEffect;
    
    [Header("Animación")]
    public float rotationSpeed = 90f;
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 1f;
    
    private Vector3 startPosition;
    private bool isPickedUp = false;

    void Start()
    {
        startPosition = transform.position;
        
        // Asegurar que tenga tag
        gameObject.tag = "AmmoPickup";
        
        // Asegurar que tenga collider trigger si no lo tiene
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.8f;
        }
        else
        {
            collider.isTrigger = true;
        }
        
        // Asegurar que tenga renderer si es un GameObject vacío
        if (GetComponent<Renderer>() == null)
        {
            // Crear un objeto visual simple si no hay renderer
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
            
            // Hacerlo verde para indicar que es munición
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = Color.green;
                renderer.material = material;
            }
            
            // Remover el collider del objeto visual
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
                Destroy(visualCollider);
        }
    }

    void Update()
    {
        if (isPickedUp) return;
        
        // Rotación continua
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        // Flotación suave
        float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return;
        
        if (other.CompareTag("Player"))
        {
            PickupAmmo(other.gameObject);
        }
    }

    void PickupAmmo(GameObject player)
    {
        isPickedUp = true;
        
        // Agregar munición al GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddAmmo(ammoAmount);
        }

        // Efecto de sonido
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // Efecto visual
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        Debug.Log("Munición recogida: " + ammoAmount + " balas");

        // Destruir el objeto de recogida
        Destroy(gameObject);
    }
}