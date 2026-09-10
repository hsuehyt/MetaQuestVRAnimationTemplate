using UnityEngine;

namespace VRAnimationTemplate
{
    /// <summary>Rigid bone animation in metres. One step, discovery, then a persistent attentive idle.</summary>
    public sealed class ForestRobotEncounter : MonoBehaviour
    {
        public Transform robot, hips, torso, head;
        public Transform leftHip, leftKnee, leftAnkle, rightHip, rightKnee, rightAnkle;
        public Transform leftShoulder, rightShoulder;
        public Transform[] eyes, beams;
        public Light[] searchlights;
        public AudioSource footfall;
        public Transform[] nearbyFoliage;
        [Range(0, 40)] public float previewSeconds;
        public float Elapsed { get; private set; }
        Vector3 start;
        bool sounded;
        MaterialPropertyBlock beamProperties;

        void Awake() { start = robot.localPosition; beamProperties = new MaterialPropertyBlock(); }
        void Update() { Elapsed += Time.deltaTime; Sample(Elapsed); }
        static float Ease(float t, float a, float b) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));

        public void Sample(float t)
        {
            if (robot == null) return;
            if (!Application.isPlaying) start = new Vector3(0, 0, 29);
            float step = Ease(t, 15, 19);
            float lift = Mathf.Sin(step * Mathf.PI);
            // Robot initially faces diagonally away; its single stride proceeds across the clearing.
            robot.localPosition = start + Quaternion.Euler(0, 65, 0) * new Vector3(0, 0, step * 3.4f);
            hips.localPosition = new Vector3(0, 12.2f - .65f * step - .2f * lift, 0);
            // Analytic two-bone legs: the supporting foot stays planted and the other clears the ground.
            PoseLeg(leftHip, leftKnee, leftAnkle, 1.8f + 1.15f * lift, step * 3.4f);
            PoseLeg(rightHip, rightKnee, rightAnkle, 1.8f, -step * 3.4f);
            float discover = Ease(t, 20, 26);
            torso.localRotation = Quaternion.Euler(0, 115 * Ease(t, 22, 27), 0);
            leftShoulder.localRotation = Quaternion.Euler(5 * lift, 0, -4 + 1.2f * Mathf.Sin(t * .5f));
            rightShoulder.localRotation = Quaternion.Euler(-6 * lift, 0, 4 - 1.2f * Mathf.Sin(t * .5f));
            float scan = Mathf.Sin(Mathf.InverseLerp(6, 14, t) * Mathf.PI * 2) * 32;
            Quaternion lookingAround = Quaternion.Euler(10, scan, 0);
            Vector3 viewer = Camera.main != null ? Camera.main.transform.position : new Vector3(0, 1.65f, 0);
            Quaternion gaze = Quaternion.LookRotation(viewer - head.position, Vector3.up);
            Quaternion localGaze = Quaternion.Inverse(head.parent.rotation) * gaze;
            head.localRotation = Quaternion.Slerp(lookingAround, localGaze * Quaternion.Euler(0, 0, 5 * Ease(t, 27, 30)), discover);
            bool blink = (t > 26.2f && t < 26.38f) || (t > 26.65f && t < 26.88f);
            float brightness = blink ? 0 : Mathf.Lerp(1, .12f, discover);
            foreach (Transform eye in eyes) eye.localScale = new Vector3(.62f, blink ? .035f : .62f, .18f);
            foreach (Transform beam in beams) beam.gameObject.SetActive(!blink);
            foreach (Light light in searchlights) light.intensity = brightness * 6;
            foreach (Transform beam in beams)
            {
                if (beamProperties == null) beamProperties = new MaterialPropertyBlock();
                beamProperties.SetFloat("_Strength", brightness);
                beam.GetComponent<Renderer>().SetPropertyBlock(beamProperties);
            }
            if (t >= 18.9f && !sounded && Application.isPlaying) { sounded = true; if (footfall) footfall.Play(); }
            float tremble = t > 18.9f && t < 21 ? Mathf.Sin((t - 18.9f) * 24) * Mathf.Exp(-(t - 18.9f) * 2) * 2 : 0;
            foreach (Transform plant in nearbyFoliage) plant.localRotation = Quaternion.Euler(tremble, 0, tremble * .5f);
        }

        void PoseLeg(Transform hip, Transform knee, Transform ankle, float footHeight, float footZ)
        {
            const float upper = 5.3f, lower = 5.1f;
            float down = hips.localPosition.y - footHeight;
            float distance = Mathf.Clamp(Mathf.Sqrt(down * down + footZ * footZ), .01f, upper + lower - .001f);
            float bend = Mathf.Acos(Mathf.Clamp((distance * distance - upper * upper - lower * lower) / (2 * upper * lower), -1, 1));
            float thigh = Mathf.Atan2(footZ, down) + Mathf.Atan2(lower * Mathf.Sin(bend), upper + lower * Mathf.Cos(bend));
            hip.localRotation = Quaternion.Euler(-thigh * Mathf.Rad2Deg, 0, 0);
            knee.localRotation = Quaternion.Euler(bend * Mathf.Rad2Deg, 0, 0);
            ankle.localRotation = Quaternion.Euler((thigh - bend) * Mathf.Rad2Deg, 0, 0);
        }
    }
}
