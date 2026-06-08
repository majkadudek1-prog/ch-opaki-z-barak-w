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
    
    [HideInInspector]
    public bool isTrapSprung = false;     

    private float currentProgress = 0f;   
    private Vector3 lastHandLocalPos;
    private bool isHandInside = false;

    // NOWOŚĆ: Zmienna sprawdzająca, czy brama "zatrzasnęła się" na górze
    private bool isLockedOpen = false;

    void Update()
    {
        if (isTrapSprung) return; 

        // 1. Sprawdzamy, czy dociągnęliśmy bramę do samego końca (100%)
        if (currentProgress >= 1f)
        {
            isLockedOpen = true; // ZATRZASK!
            currentProgress = 1f; // Trzymamy sztywno na 100%
        }

        // 2. Brama opada TYLKO wtedy, gdy:
        // puścisz koło (!isHandInside) ORAZ brama nie jest na samym dole (> 0) ORAZ brama NIE zablokowała się na górze (!isLockedOpen)
        if (!isHandInside && currentProgress > 0f && !isLockedOpen)
        {
            currentProgress -= dropSpeed * Time.deltaTime;
            currentProgress = Mathf.Clamp01(currentProgress);
        }

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
        // Jeśli brama się zablokowała, ignorujemy już rękę (nie trzeba dalej kręcić)
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

            if (Mathf.Abs(angleDelta) > 0.1f) 
            {
                float progressDelta = Mathf.Abs(angleDelta) / (requiredRotations * 360f);
                currentProgress += progressDelta;
                currentProgress = Mathf.Clamp01(currentProgress);
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