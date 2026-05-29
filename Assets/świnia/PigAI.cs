using UnityEngine;
using UnityEngine.AI;

public class PigAI : MonoBehaviour
{
    [Header("Komponenty")]
    public Animator animator;
    public NavMeshAgent agent;
    public ParticleSystem eatParticles;

    [Header("Ustawienia Wykrywania")]
    public float detectionRadius = 3.5f; 
    public float eatDistance = 1.5f;    
    public float wakeupAnimationTime = 3.2f; 

    [Header("Ustawienia Spacerowania")]
    public float wanderRadius = 5f;       // Jak daleko od siebie świnka może wybrać nowy punkt do spaceru
    public float wanderIdleTime = 10f;    // Ile sekund stoi w miejscu przed kolejnym krokiem
    private float wanderTimer;

    [Header("Wizualne Śledzenie (VR)")]
    public Transform headBone;          // Przeciągnij tutaj kość Head z Hierarchy
    public float maxLookAngle = 60f;    // Maksymalny kąt, by świnia nie skręciła sobie karku o 180 stopni
    [Range(0f, 1f)]
    public float lookAtWeight = 0.6f;   // Jak mocno głowa ma podążać (np. 0.6 to naturalny zez, 1.0 to sztywne patrzenie)

    private Transform currentFood;
    private bool isAwake = false;
    private bool isWakingUp = false;

    // NOWE ZMIENNE DO PŁYNNEGO RUCHU GŁOWY
    private float currentLookWeight = 0f;
    private Quaternion currentLookRotation;

    void Start()
    {
        wanderTimer = wanderIdleTime; // Sprawi, że po obudzeniu od razu pomyśli o spacerze, jeśli nie ma jedzenia
    }

    void Update()
    {
        if (!isAwake)
        {
            if (!isWakingUp) 
                LookForFoodToWakeUp();
            return;
        }

        // Zawsze najpierw szukaj jedzenia
        LookForFood();

        // LOGIKA 1: Jest jedzenie -> Biegnij do niego
        if (currentFood != null)
        {
            agent.SetDestination(currentFood.position);
            UpdateAnimator(true); // Wywołujemy naszą nową, ulepszoną funkcję ruchu

            if (Vector3.Distance(transform.position, currentFood.position) <= eatDistance)
            {
                EatFood();
            }
        }
        // LOGIKA 2: Nie ma jedzenia -> Spaceruj po zagrodzie
        else
        {
            // Sprawdź, czy świnka doszła już do wyznaczonego punktu spaceru
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Świnka stoi w miejscu - wygładzamy animacje do zera (stan fin)
                animator.SetFloat("Forward", 0f, 0.15f, Time.deltaTime);
                animator.SetFloat("Turn", 0f, 0.15f, Time.deltaTime);

                // Odliczanie czasu stania
                wanderTimer += Time.deltaTime;
                if (wanderTimer >= wanderIdleTime)
                {
                    // Czas minął -> wybierz nowy losowy punkt na NavMeshu
                    Vector3 newWanderPos = GetRandomNavMeshLocation(wanderRadius);
                    agent.SetDestination(newWanderPos);
                    wanderTimer = 0f;
                }
            }
            else
            {
                // Świnka idzie w trakcie spaceru
                UpdateAnimator(true);
            }
        }
    }

    // --- MAGIA ŚLEDZENIA GŁOWĄ (PŁYNNA) ---
    void LateUpdate()
    {
        // Zabezpieczenie, jeśli nie przypisano kości
        if (headBone == null) return;

        bool shouldLook = false;

        // Jeśli świnka nie śpi i widzi marchewkę
        if (isAwake && currentFood != null)
        {
            Vector3 directionToFood = currentFood.position - headBone.position;
            float angle = Vector3.Angle(transform.forward, directionToFood);

            // Jeśli marchewka jest z przodu
            if (angle <= maxLookAngle)
            {
                shouldLook = true;
                // Zapisujemy idealny kąt patrzenia na jedzenie
                currentLookRotation = Quaternion.LookRotation(directionToFood);
            }
        }

        // Płynnie zwiększamy lub zmniejszamy wagę patrzenia (Mnożnik 3f to prędkość skręcania karku)
        float targetWeight = shouldLook ? lookAtWeight : 0f;
        currentLookWeight = Mathf.Lerp(currentLookWeight, targetWeight, Time.deltaTime * 3f);

        // Aplikujemy obrót tylko, jeśli waga jest większa od zera (czyli gdy świnia zaczyna patrzeć)
        if (currentLookWeight > 0.001f)
        {
            headBone.rotation = Quaternion.Slerp(headBone.rotation, currentLookRotation, currentLookWeight);
        }
    }

    // --- FUNKCJA DO PŁYNNEGO RUCHU ---
    void UpdateAnimator(bool isMoving)
    {
        if (isMoving)
        {
            // Prędkość do przodu
            Vector3 velocity = agent.velocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            float forwardSpeed = localVelocity.z / agent.speed; 

            // ULEPSZONY SKRĘT: Patrzymy gdzie NavMeshAgent CHCE iść, a nie jak się ślizga
            Vector3 desiredVelocity = agent.desiredVelocity;
            Vector3 localDesiredVelocity = transform.InverseTransformDirection(desiredVelocity).normalized;
            
            // localDesiredVelocity.x oddaje nam idealną wartość od -1 (mocno w lewo) do 1 (mocno w prawo)
            // Mnożymy to lekko (* 1.5f), żeby świnia chętniej odtwarzała maksymalne wygięcie z animacji
            float turnSpeed = Mathf.Clamp(localDesiredVelocity.x * 1.5f, -1f, 1f);

            animator.SetFloat("Forward", forwardSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat("Turn", turnSpeed, 0.1f, Time.deltaTime);
        }
    }

    void LookForFoodToWakeUp()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Marchewka"))
            {
                currentFood = hit.transform;
                isWakingUp = true;
                
                animator.SetTrigger("IsAwake"); 
                Invoke(nameof(FinishWakeUp), wakeupAnimationTime);
                break;
            }
        }
    }

    void LookForFood()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        float closestDist = Mathf.Infinity;
        Transform closest = null;
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Marchewka"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = hit.transform;
                }
            }
        }
        currentFood = closest;
    }

    void FinishWakeUp()
    {
        isAwake = true;
    }

    void EatFood()
    {
        if (eatParticles != null)
        {
            eatParticles.transform.position = currentFood.position;
            eatParticles.Play();
        }
        
        Destroy(currentFood.gameObject);
        currentFood = null;
        wanderTimer = 0f; // Po zjedzeniu postój chwilę przed kolejnym spacerem
    }

    // Specjalna funkcja, która bezpiecznie losuje punkt na niebieskiej podłodze (NavMesh)
    Vector3 GetRandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = transform.position;
        
        // Szukamy najbliższego prawidłowego punktu na siatce nawigacji
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }
}