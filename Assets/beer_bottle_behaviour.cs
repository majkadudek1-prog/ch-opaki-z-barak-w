using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class beer_broken : MonoBehaviour
{
    [Header("Referencje")]
    [Tooltip("Prefab zawierający pocięte kawałki butelki. Każdy kawałek powinien mieć Rigidbody i Collider.")]
    public GameObject brokenBottlePrefab;

    [Header("Ustawienia rozbijania")]
    [Tooltip("Minimalna siła uderzenia wymagana do rozbicia butelki.")]
    public float breakForceLimit = 2f;
    
    [Header("Ustawienia odłamków")]
    [Tooltip("Siła, z jaką odłamki zostaną rozrzucone na zewnątrz.")]
    public float explosionForce = 150f;
    [Tooltip("Promień wybuchu odłamków.")]
    public float explosionRadius = 1.5f;

    private bool _isBroken = false;
    private Rigidbody _rb;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Zapobiega podwójnemu wykonaniu kodu, jeśli kolizja nastąpi w tej samej klatce
        if (_isBroken) return;

        // Sprawdzamy, czy siła uderzenia przekracza zdefiniowany limit
        if (collision.relativeVelocity.magnitude >= breakForceLimit)
        {
            BreakBottle();
        }
    }

    public void BreakBottle()
    {
        _isBroken = true;

        // 1. Instancjowanie prefaba z odłamkami w miejscu obecnej butelki
        if (brokenBottlePrefab != null)
        {
            GameObject shatteredObject = Instantiate(brokenBottlePrefab, transform.position, transform.rotation);

            // 2. Dodanie fizyki do odłamków
            // Zakładamy, że odłamki są dziećmi (child objects) wewnątrz prefaba
            foreach (Rigidbody shardRb in shatteredObject.GetComponentsInChildren<Rigidbody>())
            {
                // Zachowujemy pęd całej butelki przed rozbiciem
                shardRb.linearVelocity = _rb.linearVelocity;

                // Dodajemy lekką siłę wybuchu, aby odłamki efektownie się rozsypały
                shardRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }
        else
        {
            Debug.LogWarning("Nie przypisano prefaba rozbitej butelki w skrypcie BreakableBottle!");
        }

        // 3. Zniszczenie oryginalnego, nienaruszonego obiektu
        gameObject.SetActive(false);
    }
}
