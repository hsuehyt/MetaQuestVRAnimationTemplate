using UnityEngine;

namespace VRAnimationTemplate
{
    /// <summary>Visible placeholder motion; replace this object with the real chapter content.</summary>
    public sealed class DemoOrbitAnimation : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(18f, 42f, 9f);
        [SerializeField, Min(0f)] private float bobHeight = 0.18f;
        [SerializeField, Min(0f)] private float bobSpeed = 1.4f;
        [SerializeField, Min(0f)] private float pulseAmount = 0.08f;

        private Vector3 startPosition;
        private Vector3 startScale;

        private void Start()
        {
            startPosition = transform.localPosition;
            startScale = transform.localScale;
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
            float wave = Mathf.Sin(Time.time * bobSpeed);
            transform.localPosition = startPosition + Vector3.up * (wave * bobHeight);
            transform.localScale = startScale * (1f + wave * pulseAmount);
        }
    }
}
