using UnityEngine;

public class PulseGlow : MonoBehaviour
{
    Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        float s = 1 + Mathf.Sin(Time.time * 2f) * 0.02f;
        transform.localScale = startScale * s;
    }
}