using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFollower : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player; // objetivo (jugador)

    [Header("Configuración de movimiento")]
    public float followDistance = 15f; // hasta dónde detecta al jugador
    public float stopDistance = 2f;    // distancia mínima antes de detenerse

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Buscar jugador si no está asignado
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        // Evitar que el NPC caiga al vacío
        agent.autoTraverseOffMeshLink = false; // no intentará saltar huecos
        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Si el jugador está dentro del rango de detección
        if (distance <= followDistance)
        {
            // Si está más lejos que la distancia mínima, seguir
            if (distance > stopDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                // Detenerse cerca del jugador
                agent.isStopped = true;
            }
        }
        else
        {
            // Jugador fuera de rango: detenerse
            agent.isStopped = true;
        }

        // Evitar que se caiga si se acerca al borde
        PreventFalling();
    }

    void PreventFalling()
    {
        // Raycast hacia abajo desde el frente del NPC
        Vector3 rayOrigin = transform.position + transform.forward * 0.5f;
        Ray ray = new Ray(rayOrigin, Vector3.down);
        if (!Physics.Raycast(ray, 2f))
        {
            // No hay suelo adelante → detenerse
            agent.isStopped = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
