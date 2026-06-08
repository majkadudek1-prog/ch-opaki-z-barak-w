using UnityEngine;
using UnityEngine.AI;

public class PigAI : MonoBehaviour
{
    [Header("Komponenty")]
    public Animator animator;
    public NavMeshAgent agent;
    public ParticleSystem eatParticles;

    [Header("Efekty Spania i Budzenia")]
    public ParticleSystem sleepParticles;
    public ParticleSystem wakeupParticles;

    [Header("Dźwięki (Audio)")]
    public AudioSource pigAudio;
    public AudioClip idleOinkSound;
    public float oinkInterval = 7f;
    private float oinkTimer;

    [Header("Ustawienia Wykrywania")]
    public float detectionRadius = 3.5f; 
    public float eatDistance = 1.5f;    
    public float wakeupAnimationTime = 3.2f; 
    
    public float playerStopDistance = 2.2f; 
    // --- NOWE: Kontrola buforu (Deadzone) ---
    [Tooltip("Ile metrów zapasu ma świnia, zanim ruszy ponownie (zapobiega mikro-kroczkom)")]
    public float stopBuffer = 0.5f; 
    private bool isWaitingAtBuffer = false; // Pamięta, czy świnia już stoi w buforze
    // ----------------------------------------
    private float originalStoppingDistance;

    [Header("Ustawienia Spacerowania i Skrętu")]
    public float wanderRadius = 5f;       
    public float wanderIdleTime = 10f;    
    [Tooltip("Powyżej ilu stopni świnia ma się zatrzymać i zawrócić w miejscu?")]
    public float sharpTurnAngle = 45f; 
    
    // --- NOWE: Wzmacniacz animacji skrętu ---
    [Tooltip("Zwiększ, by świnia mocniej wyginała się na zakrętach (np. 2.5 lub 3.0)")]
    public float turnSensitivity = 2.5f; 
    // ----------------------------------------
    
    private float wanderTimer;
    private float originalSpeed; 

    [Header("Wizualne Śledzenie (VR)")]
    public Transform headBone;          
    public float maxLookAngle = 80f;    
    [Range(0f, 1f)]
    public float lookAtWeight = 0.8f;   
    
    public Vector3 headRotationOffset; 

    private Transform currentFood;
    public bool isAwake = false;
    private bool isWakingUp = false;

    private float currentLookWeight = 0f;
    private Quaternion currentLookRotation;

    void Start()
    {
        wanderTimer = wanderIdleTime; 
        originalSpeed = agent.speed; 
        originalStoppingDistance = agent.stoppingDistance; 

        oinkTimer = Random.Range(2f, oinkInterval); 
        if (sleepParticles != null && !isAwake)
        {
            sleepParticles.Play();
        }
    }

    void Update()
    {
        if (!isAwake)
        {
            if (!isWakingUp) 
                LookForFoodToWakeUp();
            return;
        }

        if (pigAudio != null && idleOinkSound != null)
        {
            oinkTimer += Time.deltaTime;
            if (oinkTimer >= oinkInterval)
            {
                pigAudio.PlayOneShot(idleOinkSound);
                oinkTimer = 0f;
                oinkInterval = Random.Range(5f, 12f); 
            }
        }

        LookForFood();

        if (currentFood != null)
        {
            bool isFoodHeld = false;
            GameObject[] playerHands = GameObject.FindGameObjectsWithTag("PlayerHand");
            foreach (GameObject hand in playerHands)
            {
                if (Vector3.Distance(currentFood.position, hand.transform.position) < 0.60f)
                {
                    isFoodHeld = true;
                    break;
                }
            }

            float distToFood = Vector3.Distance(transform.position, currentFood.position);

            if (isFoodHeld)
            {
                // --- NOWE: Logika Histerezy (Strefa Buforowa) ---
                // Jeśli weszła w strefę zatrzymania, każemy jej czekać
                if (distToFood <= playerStopDistance)
                {
                    isWaitingAtBuffer = true;
                }
                // Jeśli oddalisz marchewkę poza strefę buforu (np. za plecy), świnia znów ruszy
                else if (distToFood > playerStopDistance + stopBuffer)
                {
                    isWaitingAtBuffer = false;
                }

                if (isWaitingAtBuffer)
                {
                    agent.isStopped = true; 
                    if (agent.hasPath) agent.ResetPath();
                    UpdateAnimator(false); 
                }
                else
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = playerStopDistance; 
                    agent.SetDestination(currentFood.position);
                    UpdateAnimator(true);
                }
            }
            else
            {
                isWaitingAtBuffer = false; // Resetujemy bufor
                agent.isStopped = false;
                agent.stoppingDistance = originalStoppingDistance; 
                agent.SetDestination(currentFood.position);
                UpdateAnimator(true);
            }

            if (distToFood <= eatDistance)
            {
                EatFood();
            }
        }
        else
        {
            isWaitingAtBuffer = false;
            agent.isStopped = false;
            agent.stoppingDistance = originalStoppingDistance; 

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                UpdateAnimator(false);

                wanderTimer += Time.deltaTime;
                if (wanderTimer >= wanderIdleTime)
                {
                    Vector3 newWanderPos = GetRandomNavMeshLocation(wanderRadius);
                    agent.SetDestination(newWanderPos);
                    wanderTimer = 0f;
                }
            }
            else
            {
                UpdateAnimator(true);
            }
        }
    }

    void LateUpdate()
    {
        if (headBone == null) return;

        bool shouldLook = false;

        if (isAwake && currentFood != null)
        {
            Vector3 directionToFood = currentFood.position - headBone.position;
            float angle = Vector3.Angle(transform.forward, directionToFood);

            if (angle <= maxLookAngle)
            {
                shouldLook = true;
                
                Quaternion baseRotation = Quaternion.LookRotation(directionToFood);
                currentLookRotation = baseRotation * Quaternion.Euler(headRotationOffset);
            }
        }

        float targetWeight = shouldLook ? lookAtWeight : 0f;
        currentLookWeight = Mathf.Lerp(currentLookWeight, targetWeight, Time.deltaTime * 5f);

        if (currentLookWeight > 0.001f)
        {
            headBone.rotation = Quaternion.Slerp(headBone.rotation, currentLookRotation, currentLookWeight);
        }
    }

    void UpdateAnimator(bool isMoving)
    {
        if (isMoving)
        {
            Vector3 directionToTarget = agent.steeringTarget - transform.position;
            directionToTarget.y = 0f; 
            
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            Vector3 desiredVelocity = agent.desiredVelocity;
            Vector3 localDesiredVelocity = transform.InverseTransformDirection(desiredVelocity).normalized;

            float forwardSpeed = 0f;
            float turnSpeed = 0f;

            if (angleToTarget > sharpTurnAngle && directionToTarget.sqrMagnitude > 0.1f)
            {
                agent.speed = 0.2f; 
                forwardSpeed = 0f; 
                turnSpeed = Mathf.Sign(localDesiredVelocity.x) * 1f; 
            }
            else
            {
                agent.speed = originalSpeed; 
                
                Vector3 velocity = agent.velocity;
                Vector3 localVelocity = transform.InverseTransformDirection(velocity);
                
                forwardSpeed = localVelocity.z / originalSpeed; 
                
                // --- NOWE: Używamy wzmacniacza turnSensitivity ---
                turnSpeed = Mathf.Clamp(localDesiredVelocity.x * turnSensitivity, -1f, 1f);
            }

            animator.SetFloat("Forward", forwardSpeed, 0.25f, Time.deltaTime);
            animator.SetFloat("Turn", turnSpeed, 0.25f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Forward", 0f, 0.25f, Time.deltaTime);
            animator.SetFloat("Turn", 0f, 0.25f, Time.deltaTime);
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
                
                if (sleepParticles != null) sleepParticles.Stop();
                if (wakeupParticles != null) wakeupParticles.Play();

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
        wanderTimer = 0f; 
    }

    Vector3 GetRandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = transform.position;
        
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }
}