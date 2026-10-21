using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace CursedCrown
{
    // Local preview rig only. Person 1 replaces/wraps this with networked avatars.
    // No movement, shooting or network authority is provided by this component.
    public sealed class LocalTrackedRig : MonoBehaviour
    {
        public Transform Head;
        public Transform LeftHand;
        public Transform RightHand;
        private readonly List<XRInputSubsystem> subsystems = new();

        private void Start()
        {
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }

        private void Update()
        {
            Apply(XRNode.Head, Head);
            Apply(XRNode.LeftHand, LeftHand);
            Apply(XRNode.RightHand, RightHand);
        }

        private static void Apply(XRNode node, Transform target)
        {
            if (target == null) return;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out var position))
                target.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation))
                target.localRotation = rotation;
        }
    }
}
