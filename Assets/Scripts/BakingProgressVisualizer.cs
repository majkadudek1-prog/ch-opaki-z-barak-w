using UnityEngine;
using System.Collections.Generic;

public class BakingProgressVisualizer : MonoBehaviour
{
    [System.Serializable]
    public struct BakingStage
    {
        public string name;
        public GameObject visualObject;
        [Range(0f, 1f)] public float activationThreshold;
    }

    [Header("Ustawienia Pieczenia")]
    [Range(0f, 1f)]
    [SerializeField] private float bakingLevel = 0f;
    [SerializeField] private float bakingSpeed = 0.1f; // Jak szybko siê piecze (0.1 = 10 sekund do max)

    [Header("Etapy Wizualne")]
    [SerializeField] private List<BakingStage> stages = new List<BakingStage>();

    public GrillStove collidedStove = null;

    public float BakingLevel
    {
        get => bakingLevel;
        set
        {
            bakingLevel = Mathf.Clamp01(value);
            UpdateVisuals();
        }
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        // Jeœli obiekt jest na grillu, zwiêkszaj poziom pieczenia co klatkê
        if (collidedStove!=null && collidedStove.isOn && bakingLevel < 1f)
        {
            BakingLevel += bakingSpeed * Time.deltaTime;
        }
    }

    private void UpdateVisuals()
    {
        if (stages == null || stages.Count == 0) return;

        GameObject objectToEnable = null;
        float highestThresholdReached = -1f;

        foreach (var stage in stages)
        {
            if (stage.visualObject == null) continue;
            stage.visualObject.SetActive(false);

            if (bakingLevel >= stage.activationThreshold && stage.activationThreshold > highestThresholdReached)
            {
                highestThresholdReached = stage.activationThreshold;
                objectToEnable = stage.visualObject;
            }
        }

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
        }
    }

    // Wykrywanie wejœcia na grill
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponent<GrillStove>() != null)
        {
            collidedStove = collision.collider.GetComponent<GrillStove>();
        }
    }

    // Wykrywanie zejœcia z grilla
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.GetComponent<GrillStove>() != null)
        {
            collidedStove = null;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        UpdateVisuals();
    }
}