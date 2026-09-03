using UnityEngine;
using UnityEngine.InputSystem;

namespace VRAnimationTemplate
{
    /// <summary>
    /// Minimal OpenXR head-pose driver. It keeps the starter independent of a particular
    /// controller/hand rig while remaining replaceable by Meta's Camera Rig Building Block.
    /// </summary>
    public sealed class QuestCameraPoseDriver : MonoBehaviour
    {
        private InputAction positionAction;
        private InputAction rotationAction;

        private void OnEnable()
        {
            positionAction = new InputAction("HMD Position", binding: "<XRHMD>/centerEyePosition");
            rotationAction = new InputAction("HMD Rotation", binding: "<XRHMD>/centerEyeRotation");
            positionAction.Enable();
            rotationAction.Enable();
        }

        private void OnDisable()
        {
            positionAction?.Dispose();
            rotationAction?.Dispose();
            positionAction = null;
            rotationAction = null;
        }

        private void LateUpdate()
        {
            if (positionAction == null || rotationAction == null)
                return;

            if (positionAction.activeControl != null)
                transform.localPosition = positionAction.ReadValue<Vector3>();

            if (rotationAction.activeControl != null)
            {
                Quaternion rotation = rotationAction.ReadValue<Quaternion>();
                if (Quaternion.Dot(rotation, rotation) > 0.5f)
                    transform.localRotation = rotation;
            }
        }
    }
}
