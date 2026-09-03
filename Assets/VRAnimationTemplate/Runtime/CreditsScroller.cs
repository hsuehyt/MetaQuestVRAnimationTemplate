using UnityEngine;

namespace VRAnimationTemplate
{
    public sealed class CreditsScroller : MonoBehaviour
    {
        [SerializeField] private RectTransform credits;
        [SerializeField, Min(1f)] private float pixelsPerSecond = 28f;
        [SerializeField] private float resetAtY = 900f;
        private Vector2 startPosition;

        private void Start()
        {
            if (credits != null)
                startPosition = credits.anchoredPosition;
        }

        private void Update()
        {
            if (credits == null)
                return;

            credits.anchoredPosition += Vector2.up * (pixelsPerSecond * Time.deltaTime);
            if (credits.anchoredPosition.y >= resetAtY)
                credits.anchoredPosition = startPosition;
        }
    }
}
