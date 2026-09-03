using UnityEngine;
using UnityEngine.UI;

namespace VRAnimationTemplate
{
    /// <summary>Controls a chapter timer and optionally advances to the next scene.</summary>
    public sealed class VRChapterDirector : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float durationSeconds = 12f;
        [SerializeField] private bool autoAdvance = true;
        [SerializeField] private Text timeLabel;
        [SerializeField] private Image progressFill;

        private float elapsed;
        private bool finished;

        private void Update()
        {
            if (finished)
                return;

            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / durationSeconds);

            if (progressFill != null)
                progressFill.fillAmount = normalized;
            if (timeLabel != null)
                timeLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, durationSeconds - elapsed))}s";

            if (elapsed >= durationSeconds)
            {
                finished = true;
                if (autoAdvance && VRSceneFlow.Instance != null)
                    VRSceneFlow.Instance.NextScene();
            }
        }

        public void ContinueNow()
        {
            finished = true;
            VRSceneFlow.Instance?.NextScene();
        }
    }
}
