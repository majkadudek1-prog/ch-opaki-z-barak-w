using UnityEngine;
using UnityEngine.AI;

public class GateWheel : MonoBehaviour
{
    [Header("Obiekty")]
    public Transform mainGate;
    public Transform gateClosedPos;
    public Transform gateOpenPos;
    public NavMeshObstacle gateObstacle; 

    [Header("Ustawienia Kręcenia")]
    public float requiredRotations = 3f;  
    public float dropSpeed = 0.5f;        
    
    [Header("Fizyka Koła (VR Inertia)")]
    [Tooltip("Oporność koła. Wyższa wartość = koło szybciej się zatrzyma po puszczeniu.")]
    public float resistance = 1.0f; 
    [Tooltip("Jak szybko koło reaguje na ruch ręki (czułość).")]
    public float responsiveness = 5f;

    [HideInInspector]
    public bool isTrapSprung = false;     

    private float currentProgress = 0f;   
    private Vector3 lastHandLocalPos;
    private bool isHandInside = false;
    private bool isLockedOpen = false;

    // NOWE ZMIENNE: Do obsługi płynnego wygaszania ruchu
    private float wheelVelocity = 0f; 
    private bool isHandMovingThisFrame = false;

    void Update()
    {
        if (isTrapSprung) return; 

        // 1. Sprawdzamy, czy dociągnęliśmy bramę do samego końca (100%)
        if (currentProgress >= 1f)
        {
            isLockedOpen = true; 
            currentProgress = 1f; 
            wheelVelocity = 0f; // Zatrzymujemy koło, gdy zaskoczy zatrzask
        }

        // 2. LOGIKA OPORU: Jeśli gracz nie kręci kołem w tej klatce
        if (!isHandMovingThisFrame && !isLockedOpen)
        {
            // Płynnie wygaszamy prędkość koła do zera na podstawie oporności (resistance)
            wheelVelocity = Mathf.MoveTowards(wheelVelocity, 0f, resistance * Time.deltaTime);

            // Grawitacja: Brama opada TYLKO wtedy, gdy koło całkowicie przestało się kręcić,
            // gracz nie trzyma koła ORAZ brama nie jest na samym dole
            if (wheelVelocity <= 0f && !isHandInside && currentProgress > 0f)
            {
                currentProgress -= dropSpeed * Time.deltaTime;
                currentProgress = Mathf.Clamp01(currentProgress);
            }
        }

        // 3. APLIKACJA RUCHU: Przesuwamy bramę o wyliczoną płynną prędkość koła
        if (!isLockedOpen && wheelVelocity > 0f)
        {
            currentProgress += wheelVelocity * Time.deltaTime;
            currentProgress = Mathf.Clamp01(currentProgress);
        }

        // Resetujemy flagę na koniec klatki (OnTriggerStay podniesie ją w następnej, jeśli ręka się ruszy)
        isHandMovingThisFrame = false;

        // Fizyczne przesuwanie bramy i obrót koła
        mainGate.position = Vector3.Lerp(gateClosedPos.position, gateOpenPos.position, currentProgress);
        transform.localRotation = Quaternion.Euler(0, 0, currentProgress * requiredRotations * 360f);

        // Odblokowanie podłogi (NavMesh) dla świni
        if (gateObstacle != null)
        {
            gateObstacle.enabled = currentProgress < 0.95f; 
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isTrapSprung || isLockedOpen) return; 

        if (other.CompareTag("PlayerHand"))
        {
            isHandInside = true;
            lastHandLocalPos = transform.InverseTransformPoint(other.transform.position);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (isTrapSprung || isLockedOpen) return;

        if (other.CompareTag("PlayerHand"))
        {
            Vector3 currentHandLocalPos = transform.InverseTransformPoint(other.transform.position);

            Vector2 lastDir = new Vector2(lastHandLocalPos.x, lastHandLocalPos.y).normalized;
            Vector2 currentDir = new Vector2(currentHandLocalPos.x, currentHandLocalPos.y).normalized;

            float angleDelta = Vector2.SignedAngle(lastDir, currentDir);

            // Sprawdzamy, czy ręka faktycznie wykonuje ruch obrotowy wokół środka koła
            if (Mathf.Abs(angleDelta) > 0.1f) 
            {
                float progressDelta = Mathf.Abs(angleDelta) / (requiredRotations * 360f);
                
                // Wyliczamy rzeczywistą prędkość ruchu ręki gracza na sekundę
                float handSpeed = progressDelta / Time.deltaTime;

                // Koło płynnie zbliża się do prędkości ręki (nadajemy mu masę przez responsiveness)
                wheelVelocity = Mathf.Lerp(wheelVelocity, handSpeed, Time.deltaTime * responsiveness);
                isHandMovingThisFrame = true;
            }

            lastHandLocalPos = currentHandLocalPos;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            isHandInside = false;
        }
    }
}