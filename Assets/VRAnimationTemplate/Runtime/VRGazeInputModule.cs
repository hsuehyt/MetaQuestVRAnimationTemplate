using UnityEngine;
using UnityEngine.UI;

namespace VRAnimationTemplate
{
    /// <summary>Activates UI buttons after the viewer holds the center gaze on them.</summary>
    public sealed class VRGazeInputModule : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float dwellSeconds = 1.1f;
        [SerializeField] private Image reticleProgress;
        [SerializeField] private Camera gazeCamera;

        private Button[] buttons;
        private Button gazeButton;
        private float gazeTime;

        private void Awake()
        {
            if (gazeCamera == null)
                gazeCamera = Camera.main;

            buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private void Update()
        {
            if (gazeCamera == null)
                gazeCamera = Camera.main;

            if (gazeCamera == null)
                return;

            Button currentButton = FindGazedButton();

            if (currentButton != gazeButton)
            {
                gazeButton = currentButton;
                gazeTime = 0f;
            }
            else if (gazeButton != null && gazeButton.IsActive() && gazeButton.IsInteractable())
            {
                gazeTime += Time.unscaledDeltaTime;
                if (gazeTime >= dwellSeconds)
                {
                    Debug.Log("Gaze activated button: " + gazeButton.name, gazeButton);
                    gazeButton.onClick.Invoke();
                    gazeTime = 0f;
                }
            }
            else
            {
                gazeTime = 0f;
            }

            if (reticleProgress != null)
            {
                reticleProgress.fillAmount = gazeButton == null ? 0f : Mathf.Clamp01(gazeTime / dwellSeconds);
                reticleProgress.color = gazeButton == null
                    ? Color.white
                    : new Color(0.1f, 0.85f, 1f, 1f);
            }
        }

        private Button FindGazedButton()
        {
            Ray gazeRay = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);
            Button closestButton = null;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !button.IsActive() || !button.IsInteractable())
                    continue;

                RectTransform rectTransform = button.transform as RectTransform;
                if (rectTransform == null)
                    continue;

                Plane buttonPlane = new Plane(rectTransform.forward, rectTransform.position);
                if (!buttonPlane.Raycast(gazeRay, out float distance) || distance >= closestDistance)
                    continue;

                Vector3 localPoint = rectTransform.InverseTransformPoint(gazeRay.GetPoint(distance));
                if (!rectTransform.rect.Contains(new Vector2(localPoint.x, localPoint.y)))
                    continue;

                closestButton = button;
                closestDistance = distance;
            }

            return closestButton;
        }
    }
}
