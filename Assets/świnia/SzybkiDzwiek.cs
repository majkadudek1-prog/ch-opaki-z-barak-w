using UnityEngine;

public class SzybkiDzwiek : MonoBehaviour
{
    public AudioClip dzwiekUderzenia;

    void OnCollisionEnter(Collision collision)
    {
        // Ignorujemy leciutkie dotkniêcia, ¿eby dŸwiêk nie spamowa³, gdy coœ tylko le¿y
        if (collision.relativeVelocity.magnitude > 0.5f)
        {
            // Odtwarza ten sam dŸwiêk w miejscu styku przedmiotu z pod³og¹ (¿eby w VR brzmia³o to naturalnie)
            AudioSource.PlayClipAtPoint(dzwiekUderzenia, collision.contacts[0].point);
        }
    }
}