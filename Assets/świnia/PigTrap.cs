using UnityEngine;
using UnityEngine.AI;

public class PigTrap : MonoBehaviour
{
    [Header("Elementy Pułapki")]
    public Transform mainGate;          
    public Transform smallDoor;         

    // NOWOŚĆ: Szufladka na skrypt koła, żeby pułapka mogła je zablokować
    [Header("Połączenie z Kołem")]
    public GateWheel gateWheelScript;   

    [Header("Miejsca Docelowe")]
    public Transform gateClosedPosition;
    public Transform doorOpenPosition;  

    [Header("Ustawienia Czasu i Prędkości")]
    public float mainGateSpeed = 15f;    // Prędkość bramy (szybkie trzaśnięcie)
    public float smallDoorSpeed = 2f;    // Prędkość klapki (powolne otwarcie)
    public float freezeDelay = 0.25f;    // Czas do zamrożenia świni
    public float smallDoorDelay = 3f;    // Ile sekund czekać w ciszy przed otwarciem klapki

    private bool isTrapped = false;
    private bool isGateClosed = false;
    private float doorTimer = 0f;        // Zegar odliczający czas w ukryciu
    
    private GameObject trappedPig;
    private PigAI pigScript;

    void Update()
    {
        if (isTrapped)
        {
            // 1. Zamykanie głównej bramy
            if (!isGateClosed)
            {
                mainGate.position = Vector3.MoveTowards(mainGate.position, gateClosedPosition.position, mainGateSpeed * Time.deltaTime);

                // Kiedy brama w pełni opadnie na ziemię
                if (Vector3.Distance(mainGate.position, gateClosedPosition.position) < 0.01f)
                {
                    isGateClosed = true;
                    
                    // Świnia znika w ciemnościach
                    if (trappedPig != null)
                    {
                        trappedPig.SetActive(false);
                    }
                }
            }
            // 2. Brama zamknięta? Odliczamy czas i powoli otwieramy drzwiczki!
            else
            {
                doorTimer += Time.deltaTime; // Stoper tyka...

                if (doorTimer >= smallDoorDelay)
                {
                    smallDoor.position = Vector3.MoveTowards(smallDoor.position, doorOpenPosition.position, smallDoorSpeed * Time.deltaTime);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PigAI incomingPig = other.GetComponentInParent<PigAI>();
        
        if (incomingPig != null && !isTrapped)
        {
            isTrapped = true; 

            // NOWOŚĆ: Jeśli podpięłaś koło, informujemy je, że pułapka zatrzasnęła bramę i blokujemy kręcenie!
            if (gateWheelScript != null) 
            {
                gateWheelScript.isTrapSprung = true;
            }
            
            pigScript = incomingPig;
            trappedPig = pigScript.gameObject;

            Invoke(nameof(FreezePig), freezeDelay);
        }
    }

    void FreezePig()
    {
        if (trappedPig != null)
        {
            NavMeshAgent agent = trappedPig.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            
            pigScript.enabled = false;

            Animator anim = trappedPig.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Forward", 0f);
                anim.SetFloat("Turn", 0f);
            }
        }
    }
}