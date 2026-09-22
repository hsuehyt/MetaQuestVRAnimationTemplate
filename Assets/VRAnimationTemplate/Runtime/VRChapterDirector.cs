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
        private bool externalLoopTime;

        public void SetDuration(float seconds) => durationSeconds = Mathf.Max(.1f, seconds);

        public void SetLoopTime(float seconds, float loopDuration)
        {
            durationSeconds = Mathf.Max(.1f, loopDuration);
            elapsed = Mathf.Repeat(Mathf.Max(0f, seconds), durationSeconds);
            externalLoopTime = true;
        }

        private void LateUpdate()
        {
            if (finished)
                return;

            if (!externalLoopTime)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= durationSeconds)
                {
                    if (autoAdvance)
                    {
                        elapsed = durationSeconds;
                        finished = true;
                        VRSceneFlow.Instance?.NextScene();
                    }
                    else
                    {
                        elapsed = Mathf.Repeat(elapsed, durationSeconds);
                    }
                }
            }

            float normalized = Mathf.Clamp01(elapsed / durationSeconds);
            if (progressFill != null)
                progressFill.fillAmount = normalized;
            if (timeLabel != null)
                timeLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0f, durationSeconds - elapsed))}s";
        }

        public void ContinueNow()
        {
            finished = true;
            VRSceneFlow.Instance?.NextScene();
        }
    }
}
