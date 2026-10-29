using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CursedCrown
{
    // Scene-local camera/input. Networked players contain only replicated visuals.
    // Persons 1/2 can replace this with an XRI rig without changing AvatarPose.
    public sealed class FoundationRig : MonoBehaviour
    {
        public static FoundationRig Instance { get; private set; }
        public Transform Head, LeftHand, RightHand;
        public bool AutomatedMotion;
        public bool IsTracked { get; private set; }
        public bool TriggerPressed { get; private set; }
        public bool PickupPressed { get; private set; }
        public bool RightTracked { get; private set; }
        readonly List<XRInputSubsystem> subsystems = new();
        bool lastTrigger, lastPickup, floorOrigin;
        float retryOrigin, desktopYaw;
        Vector3 desktopOffset;
        void Awake() { Instance = this; }
        void Update()
        {
            if (Time.unscaledTime >= retryOrigin)
            {
                retryOrigin = Time.unscaledTime + 2;
                SubsystemManager.GetSubsystems(subsystems);
                foreach (var sub in subsystems)
                    if (sub.running) floorOrigin = sub.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            }
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            IsTracked = head.isValid && head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked) && tracked;
            bool trigger = false, pickup = false;
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            RightTracked = IsTracked && right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var rightTracked) && rightTracked;
            if (IsTracked)
            {
                Track(XRNode.Head,Head);
                Track(XRNode.LeftHand,LeftHand);
                Track(XRNode.RightHand,RightHand);
                right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out trigger);
                if (RightTracked) right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out pickup);
            }
            else
            {
                if(AutomatedMotion)desktopOffset=new Vector3(Mathf.Sin(Time.unscaledTime*2)*.3f,0,0);
                var keyboard = Keyboard.current;
                if (!AutomatedMotion && keyboard != null && (Mouse.current == null || Mouse.current.rightButton.isPressed))
                {
                    float x=(keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0);
                    float z=(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0);
                    desktopOffset=Vector3.ClampMagnitude(desktopOffset+new Vector3(x,0,z)*Time.unscaledDeltaTime,1.5f);
                    desktopYaw+=((keyboard.eKey.isPressed?1:0)-(keyboard.qKey.isPressed?1:0))*60*Time.unscaledDeltaTime;
                }
                Head.localPosition=desktopOffset+Vector3.up*1.65f;
                Head.localRotation=Quaternion.Euler(0,desktopYaw,0);
                LeftHand.localPosition=Head.localPosition+Head.localRotation*new Vector3(-.25f,-.4f,.35f);
                RightHand.localPosition=Head.localPosition+Head.localRotation*new Vector3(.25f,-.4f,.35f);
                LeftHand.localRotation=RightHand.localRotation=Head.localRotation;
            }
            TriggerPressed=trigger&&!lastTrigger;lastTrigger=trigger;
            PickupPressed=pickup&&!lastPickup;lastPickup=pickup;
        }
        void Track(XRNode node,Transform target)
        {
            var input=InputDevices.GetDeviceAtXRNode(node);
            if(input.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,out var p))target.localPosition=p+(floorOrigin?Vector3.zero:Vector3.up*1.65f);
            if(input.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation,out var q))target.localRotation=q;
        }
        public AvatarPose ReadPose() => new AvatarPose { Head=Head.localPosition,Left=LeftHand.localPosition,Right=RightHand.localPosition,
            HeadRotation=Head.localRotation,LeftRotation=LeftHand.localRotation,RightRotation=RightHand.localRotation, RightTracked=RightTracked };
        void OnDestroy() { if(Instance==this)Instance=null; }
    }
}
