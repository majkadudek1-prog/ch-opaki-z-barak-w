using UnityEngine;

public class PigPetting : MonoBehaviour
{
    [Header("Zależności")]
    [Tooltip("Przeciągnij tu główny obiekt świni (ten ze skryptem PigAI)")]
    public PigAI pigScript; // <--- Skrypt teraz komunikuje się z mózgiem świni!

    [Header("Reakcje na głaskanie")]
    public ParticleSystem heartParticles;
    public AudioSource pigAudio;
    public AudioClip happyOinkSound;

    private bool isBeingPetted = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            // ZABEZPIECZENIE: Jeśli przypisaliśmy świnię i ona nadal śpi - anuluj głaskanie!
            if (pigScript != null && !pigScript.isAwake)
            {
                return; 
            }

            isBeingPetted = true;
            
            if (heartParticles != null) heartParticles.Play();

            if (pigAudio != null && happyOinkSound != null)
            {
                pigAudio.PlayOneShot(happyOinkSound);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            isBeingPetted = false;
            if (heartParticles != null) heartParticles.Stop();
        }
    }
}