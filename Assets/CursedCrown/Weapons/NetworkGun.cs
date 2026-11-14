using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CursedCrown
{
    [RequireComponent(typeof(FoundationAvatar))]
    public sealed class NetworkGun : NetworkBehaviour, IRoundResettable
    {
        public static event Action<ValidatedHit> HitValidated;
        public static event Action<ulong, Vector3, Vector3> ShotFired;
        public NetworkVariable<int> Charge = new(GameRules.MaxCharge);
        public NetworkVariable<bool> Recharging = new(false);
        public IPlayerState PlayerState;
        public IMatchState MatchState;
        public uint Round { get; private set; }
        // Only enable on a local desktop test prefab. Production requires tracked hands.
        [SerializeField] bool allowUntrackedDesktopAimInTests;
        [SerializeField] float range = 35f;
        [SerializeField] float fireInterval = .25f;
        [SerializeField] LayerMask hitMask = ~0;
        FoundationAvatar avatar;
        double nextShot, rechargeEnd;
        uint sequence;
        bool triggerHeld, reloadHeld;

        void Awake() => avatar = GetComponent<FoundationAvatar>();
        void Update()
        {
            if (IsServer && Recharging.Value && NetworkManager.ServerTime.Time >= rechargeEnd)
            {
                Recharging.Value = false;
                Charge.Value = GameRules.MaxCharge;
            }

            if (!IsSpawned || !IsOwner) return;
            var match = MatchController.Instance;
            if (match == null || !match.IsSpawned) return;
            uint requestedRound = match.RoundId;

            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool xrFire = right.isValid &&
                right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool pressed) && pressed;
            bool keyboardFire = Keyboard.current != null &&
                Keyboard.current.spaceKey.isPressed;
            bool fire = xrFire || keyboardFire;

            if (fire && !triggerHeld) FireRpc(requestedRound);
            triggerHeld = fire;

            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool xrReload = left.isValid &&
                left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool button) && button;
            bool keyboardReload = Keyboard.current != null &&
                Keyboard.current.rKey.wasPressedThisFrame;
            bool reload = xrReload || keyboardReload;

            if (reload && !reloadHeld) RechargeRpc(requestedRound);
            reloadHeld = reload;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void FireRpc(uint requestedRound, RpcParams rpc = default)
        {
            ulong shooter = rpc.Receive.SenderClientId;
            double now = NetworkManager.ServerTime.Time;
            if (shooter != OwnerClientId || requestedRound != Round || !CanAct(shooter) ||
                Recharging.Value || Charge.Value <= 0 || now < nextShot) return;
            var pose = avatar.TrackedPose.Value;
            // Unreliable pose delivery may stop during tracking loss; bound the age of
            // the last accepted pose as well as checking its tracked-hand flag.
            if ((!pose.RightTracked && !allowUntrackedDesktopAimInTests) ||
                now - avatar.LastAcceptedPoseTime > .3) return;
            nextShot = now + fireInterval;
            Charge.Value--;
            sequence++;
            // Match P1/P2's host-owned root values even in the frame of a teleport or respawn.
            Vector3 origin = avatar.RootPosition.Value + avatar.RootRotation.Value * pose.Right;
            Vector3 direction = avatar.RootRotation.Value * pose.RightRotation * Vector3.forward;
            Vector3 end = origin + direction * range;
            var hits = Physics.RaycastAll(origin, direction, range, hitMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                end = hit.point;
                var target = hit.collider.GetComponentInParent<FoundationAvatar>();
                if (target != null && target.OwnerClientId != shooter && PlayerState.IsAlive(target.OwnerClientId))
                    HitValidated?.Invoke(new ValidatedHit(Round, shooter, target.OwnerClientId, sequence, hit.point));
                break;
            }
            ShotFired?.Invoke(shooter, origin, end);
            ShotEffectRpc(origin, end);
        }
        bool CanAct(ulong player) => PlayerState != null && PlayerState.IsAlive(player) &&
            MatchState != null && MatchState.Phase == MatchPhase.Playing;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void RechargeRpc(uint requestedRound, RpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != OwnerClientId || requestedRound != Round ||
                !CanAct(OwnerClientId) || Recharging.Value || Charge.Value >= GameRules.MaxCharge) return;
            Recharging.Value = true;
            rechargeEnd = NetworkManager.ServerTime.Time + GameRules.RechargeSeconds;
        }
        [Rpc(SendTo.ClientsAndHost)]
        void ShotEffectRpc(Vector3 origin, Vector3 end)
        {
            if (!IsServer) ShotFired?.Invoke(OwnerClientId, origin, end);
        }
        // P5 owns the authoritative match round; align the gun to that ID.
        public void SetRound(uint round)
        {
            if (!IsServer || Round == round) return;

            Round = round;
            sequence = 0;
            nextShot = rechargeEnd = 0;
            Recharging.Value = false;
            Charge.Value = GameRules.MaxCharge;
        }

        public void ResetRound()
        {
            if (!IsServer) return;

            // MatchController owns the authoritative round number. Do not
            // independently increment the gun's counter during a match restart.
            if (MatchController.Instance != null && MatchController.Instance.IsSpawned)
                SetRound(MatchController.Instance.RoundId);
            else if (Round != uint.MaxValue)
                SetRound(Round + 1);
        }
    }
}



