using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class SmartEnemy : MonoBehaviour
{
    [Header("Configuración del Enemigo")]
    public Transform player;
    public float visionRange = 15f;
    public float visionAngle = 90f;
    public float chaseSpeed = 4f;
    public float patrolSpeed = 2f;
    public float updateRate = 0.3f;

    [Header("Patrullaje")]
    public float patrolRadius = 6f;
    public float waitTime = 2f;

    [Header("Salto entre losas")]
    public float jumpDuration = 0.8f;
    public float jumpHeight = 1.5f;

    private NavMeshAgent agent;
    private Vector3 initialPosition;
    private bool playerVisible;
    private bool isPatrolling;
    private bool isCrossingLink = false;
    private float nextUpdateTime;
    private float attackCooldown = 1.2f;
    private float lastAttackTime = -10f;

    private Vector3 patrolTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Evitar clipeo y mejorar movimiento
        agent.baseOffset = agent.height * 0.5f;
        agent.avoidancePriority = Random.Range(40, 80);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoBraking = false;
        agent.updateRotation = true;
        agent.updatePosition = true;

        initialPosition = transform.position;

        // Buscar jugador automáticamente si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Empezar patrullaje después de un pequeño retraso aleatorio
        Invoke(nameof(StartPatrolling), Random.Range(0f, 1f));
    }

    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateRate;

            // Si está cruzando un enlace, no hacer nada más
            if (agent.isOnOffMeshLink && !isCrossingLink)
            {
                StartCoroutine(CrossLink(agent.currentOffMeshLinkData));
                return;
            }

            if (player != null && CanSeePlayer())
            {
                playerVisible = true;
                ChasePlayer();
            }
            else
            {
                playerVisible = false;
                if (!isPatrolling)
                    StartPatrolling();
            }
        }

        // Rotación suave hacia el movimiento
        if (agent.velocity.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(agent.velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Time.time - lastAttackTime > attackCooldown)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
                lastAttackTime = Time.time;
            }
        }
    }


    private void ChasePlayer()
    {
        agent.speed = chaseSpeed;

        // Validar que el destino esté sobre el NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(player.position, out hit, 1.5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si está muy cerca, evitar comportamiento errático
        if (distanceToPlayer < 2f)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
        else
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        float distance = dirToPlayer.magnitude;

        if (distance > visionRange) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > visionAngle * 0.5f) return false;

        // Raycast para comprobar obstrucciones
        if (Physics.Raycast(transform.position + Vector3.up * 1f, dirToPlayer.normalized, out RaycastHit hit, visionRange))
        {
            if (hit.collider.CompareTag("Player"))
                return true;
        }

        return false;
    }

    private void StartPatrolling()
    {
        isPatrolling = true;
        agent.speed = patrolSpeed;
        ChooseNewPatrolPoint();
        Invoke(nameof(StopPatrolling), waitTime + Random.Range(0.5f, 1.5f));
    }

    private void StopPatrolling()
    {
        isPatrolling = false;
    }

    private void ChooseNewPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir += initialPosition;
        randomDir.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }
    }

    private System.Collections.IEnumerator CrossLink(OffMeshLinkData linkData)
    {
        isCrossingLink = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = linkData.endPos + Vector3.up * 0.05f;

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            float heightOffset = Mathf.Sin(Mathf.PI * t) * jumpHeight;
            transform.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * heightOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        agent.CompleteOffMeshLink();
        isCrossingLink = false;
    }

    // Dibuja visión y detección en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = playerVisible ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Vector3 forward = transform.forward * visionRange;
        Quaternion leftRay = Quaternion.Euler(0, -visionAngle / 2, 0);
        Quaternion rightRay = Quaternion.Euler(0, visionAngle / 2, 0);

        Gizmos.DrawRay(transform.position, leftRay * forward);
        Gizmos.DrawRay(transform.position, rightRay * forward);
    }
}
