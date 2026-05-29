using UnityEngine;

public class GrillKnobController : MonoBehaviour
{
    [Header("Ustawienia Ognia")]
    [Tooltip("Lista obiektów z Particle System (ogniem), którymi skrypt ma sterować.")]
    public ParticleSystem[] fireParticleSystems;

    [Header("Ustawienia Obrotu")]
    [Tooltip("Kąt progowy (w stopniach). Ogień włączy się, gdy pokrętło obróci się poniżej tej wartości.")]
    public float thresholdAngle = -87f;

    private void Update()
    {
        // Pobieramy obrót lokalny w osi Z
        float currentZ = transform.localEulerAngles.z;

        // Konwertujemy wartości powyżej 180 na ujemne, aby pasowały do układu z ujemnymi stopniami
        if (currentZ > 180f)
        {
            currentZ -= 360f;
        }

        // Sprawdzamy warunek obrotu pokrętła
        if (currentZ < thresholdAngle)
        {
            SetEmissionActive(true);
        }
        else
        {
            SetEmissionActive(false);
        }
    }

    // Metoda kontrolująca emisję cząsteczek
    private void SetEmissionActive(bool isActive)
    {
        foreach (ParticleSystem ps in fireParticleSystems)
        {
            if (ps != null)
            {
                // W Unity modyfikacja modułów ParticleSystem wymaga przypisania ich do zmiennej pomocniczej
                var emission = ps.emission;
                
                // Zmieniamy stan emisji tylko wtedy, gdy jest inny niż docelowy
                if (emission.enabled != isActive)
                {
                    emission.enabled = isActive;
                }
            }
        }
    }
}