using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Disparo")]
    public KeyCode shootKey = KeyCode.Mouse0;
    public float shootRange = 100f;
    public int damagePerShot = 1;
    public LayerMask enemyLayerMask;

    [Header("Punto de Disparo")]
    public Transform weaponTip;
    public Transform forwardReference;

    [Header("Efectos")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    public GameObject hitEffect;

    [Header("Debug Visual")]
    public LineRenderer laserSight;
    public bool showLaserSight = true;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (laserSight == null && showLaserSight)
        {
            CreateLaserSight();
        }
    }

    void Update()
    {
        if (showLaserSight && laserSight != null)
        {
            UpdateLaserSight();
        }

        if (Input.GetKeyDown(shootKey))
        {
            Shoot();
        }
    }

    void CreateLaserSight()
    {
        laserSight = gameObject.AddComponent<LineRenderer>();
        laserSight.startWidth = 0.02f;
        laserSight.endWidth = 0.02f;
        laserSight.material = new Material(Shader.Find("Sprites/Default"));
        laserSight.startColor = Color.red;
        laserSight.endColor = Color.red;
        laserSight.positionCount = 2;
    }

    void UpdateLaserSight()
    {
        if (weaponTip == null) return;

        Vector3 shootOrigin = weaponTip.position;
        Vector3 shootDirection = GetShootDirection();

        laserSight.SetPosition(0, shootOrigin);

        RaycastHit hit;
        if (Physics.Raycast(shootOrigin, shootDirection, out hit, shootRange, enemyLayerMask, QueryTriggerInteraction.Collide))
        {
            laserSight.SetPosition(1, hit.point);
            laserSight.endColor = Color.green;
        }
        else
        {
            laserSight.SetPosition(1, shootOrigin + shootDirection * shootRange);
            laserSight.endColor = Color.red;
        }
    }

    Vector3 GetShootDirection()
    {
        if (forwardReference != null)
        {
            return forwardReference.forward;
        }
        return transform.forward;
    }

    void Shoot()
    {
        // Verificar munición
        if (GameManager.Instance != null && !GameManager.Instance.UseAmmo())
        {
            Debug.Log("Sin munición!");
            return;
        }

        // Efectos de disparo
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (shootSound != null && audioSource != null)
            audioSource.PlayOneShot(shootSound);

        // Calcular origen y dirección del disparo
        Vector3 shootOrigin = weaponTip != null ? weaponTip.position : transform.position;
        Vector3 shootDirection = GetShootDirection();

        // DEBUG: Mostrar información del disparo
        Debug.Log($"DISPARO - Origen: {shootOrigin}, Dirección: {shootDirection}");
        Debug.Log($"LayerMask: {enemyLayerMask.value} (binario: {System.Convert.ToString(enemyLayerMask.value, 2)})");

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(shootOrigin, shootDirection, out hit, shootRange, enemyLayerMask, QueryTriggerInteraction.Collide);
        
        if (hitSomething)
        {
            Debug.Log($"IMPACTO DETECTADO - Objeto: {hit.collider.name}, Capa: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Debug.Log($"Posición impacto: {hit.point}, Distancia: {hit.distance}");

            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                Debug.Log($"COMPONENTE EnemyHealth ENCONTRADO en {hit.collider.name}");
                if (!enemyHealth.IsDead())
                {
                    enemyHealth.TakeDamage(damagePerShot);
                    Debug.Log($"DAÑO APLICADO: {damagePerShot} a {hit.collider.name}");
                }
                else
                {
                    Debug.Log($"ENEMIGO YA ESTABA MUERTO: {hit.collider.name}");
                }
            }
            else
            {
                Debug.Log($"NO SE ENCONTRÓ EnemyHealth en {hit.collider.name}. Buscando en padres...");
                
                // Buscar en el padre si no está en este objeto
                enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    Debug.Log($"EnemyHealth ENCONTRADO en el PADRE: {enemyHealth.gameObject.name}");
                    if (!enemyHealth.IsDead())
                    {
                        enemyHealth.TakeDamage(damagePerShot);
                        Debug.Log($"DAÑO APLICADO: {damagePerShot} a {enemyHealth.gameObject.name}");
                    }
                }
                else
                {
                    Debug.Log($"Tampoco se encontró EnemyHealth en los padres de {hit.collider.name}");
                }
            }

            // Efecto de impacto
            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 2f);
            }
        }
        else
        {
            Debug.Log("NO SE DETECTÓ NINGÚN IMPACTO con el LayerMask especificado");
            
            // DEBUG: Hacer un raycast sin layerMask para ver qué hay en el camino
            RaycastHit debugHit;
            if (Physics.Raycast(shootOrigin, shootDirection, out debugHit, shootRange, ~0, QueryTriggerInteraction.Collide))
            {
                Debug.Log($"DEBUG - Impactó con: {debugHit.collider.name} (Capa: {LayerMask.LayerToName(debugHit.collider.gameObject.layer)})");
            }
            else
            {
                Debug.Log("DEBUG - No impactó con NADA (ni siquiera sin LayerMask)");
            }
        }

        // Debug visual del disparo
        Debug.DrawRay(shootOrigin, shootDirection * shootRange, Color.yellow, 2f);
    }
}