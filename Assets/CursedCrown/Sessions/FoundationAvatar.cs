using Unity.Netcode;
using UnityEngine;

namespace CursedCrown
{
    public struct AvatarPose : INetworkSerializable, System.IEquatable<AvatarPose>
    {
        public Vector3 Head, Left, Right;
        public bool RightTracked;
        public Quaternion HeadRotation, LeftRotation, RightRotation;

        public void NetworkSerialize<T>(BufferSerializer<T> s)
            where T : IReaderWriter
        {
            s.SerializeValue(ref Head);
            s.SerializeValue(ref Left);
            s.SerializeValue(ref Right);

            s.SerializeValue(ref HeadRotation);
            s.SerializeValue(ref LeftRotation);
            s.SerializeValue(ref RightRotation);
            s.SerializeValue(ref RightTracked);
        }

        public bool Equals(AvatarPose p) =>
            Head == p.Head &&
            Left == p.Left &&
            Right == p.Right &&
            HeadRotation == p.HeadRotation &&
            LeftRotation == p.LeftRotation &&
            RightRotation == p.RightRotation && RightTracked == p.RightTracked;

        public bool IsValid() =>
            ValidVector(Head) &&
            ValidVector(Left) &&
            ValidVector(Right) &&
            ValidRotation(HeadRotation) &&
            ValidRotation(LeftRotation) &&
            ValidRotation(RightRotation) &&
            Head.y >= .1f &&
            Head.y <= 2.6f &&
            new Vector2(Head.x, Head.z).magnitude <= 2f &&
            Vector3.Distance(Left, Head) <= 1.8f &&
            Vector3.Distance(Right, Head) <= 1.8f;

        static bool ValidVector(Vector3 p) =>
            Finite(p.x) &&
            Finite(p.y) &&
            Finite(p.z);

        static bool ValidRotation(Quaternion q) =>
            Finite(q.x) &&
            Finite(q.y) &&
            Finite(q.z) &&
            Finite(q.w) &&
            Quaternion.Dot(q, q) > .9f &&
            Quaternion.Dot(q, q) < 1.1f;

        static bool Finite(float f) =>
            !float.IsNaN(f) &&
            !float.IsInfinity(f);
    }

    public sealed class FoundationAvatar : NetworkBehaviour
    {
        public Transform Head, LeftHand, RightHand, Body;

        public NetworkVariable<Vector3> RootPosition = new();

        public NetworkVariable<Quaternion> RootRotation =
            new(Quaternion.identity);

        public NetworkVariable<AvatarPose> TrackedPose = new();

        double nextHostPose;
        float nextSend;

        Renderer[] headRenderers, bodyRenderers;

        readonly System.Collections.Generic.List<Material>
            ownedMaterials = new();

        public override void OnNetworkSpawn()
        {
            nextHostPose = 0;
            nextSend = 0;
            if (IsServer)
            {
                var spawn =
                    FoundationSession.Instance.AssignSpawn(OwnerClientId);

                RootPosition.Value = spawn.position;
                RootRotation.Value = spawn.rotation;

                TrackedPose.Value = new AvatarPose
                {
                    Head = Vector3.up * 1.65f,

                    Left = new Vector3(
                        -.25f,
                        1.2f,
                        .35f
                    ),

                    Right = new Vector3(
                        .25f,
                        1.2f,
                        .35f
                    ),

                    HeadRotation = Quaternion.identity,
                    LeftRotation = Quaternion.identity,
                    RightRotation = Quaternion.identity
                };
            }

            headRenderers =
                Head.GetComponentsInChildren<Renderer>();

            bodyRenderers =
                Body.GetComponentsInChildren<Renderer>();

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var material = r.material;

                ownedMaterials.Add(material);

                material.color = Color.HSVToRGB(
                    (OwnerClientId * .21f + .52f) % 1f,
                    .65f,
                    .9f
                );
            }

            if (IsOwner)
            {
                foreach (var r in headRenderers)
                    r.enabled = false;

                foreach (var r in bodyRenderers)
                    r.enabled = false;
            }
            transform.SetPositionAndRotation(
    RootPosition.Value,
    RootRotation.Value
);

var pose = TrackedPose.Value;
if (pose.IsValid())
    ApplyVisuals(pose, true);

FoundationSession.Instance?.RegisterAvatar(OwnerClientId);
        }
        public override void OnNetworkDespawn()
{
    FoundationSession.Instance?.UnregisterAvatar(OwnerClientId);
    base.OnNetworkDespawn();
}
        void LateUpdate()
        {
            if (!IsSpawned)
                return;

            transform.SetPositionAndRotation(
                RootPosition.Value,
                RootRotation.Value
            );

            var rig = FoundationRig.Instance;

            AvatarPose pose = TrackedPose.Value;
            if (IsOwner && rig != null)
            {
                rig.transform.SetPositionAndRotation(RootPosition.Value, RootRotation.Value);
                // Freeze the last valid pose when a headset temporarily loses tracking.
                var input = rig.ReadPose();
                bool inputReady = (!rig.HasHeadset || rig.IsTracked) && input.IsValid();
                if (inputReady) pose = input;
                else pose.RightTracked = false;
                if (pose.IsValid() && Time.unscaledTime >= nextSend)
                {
                    nextSend = Time.unscaledTime + .05f;
                    SubmitPoseRpc(pose);
                }
            }
            if (pose.IsValid()) ApplyVisuals(pose, IsOwner);
        }
        void ApplyVisuals(AvatarPose pose, bool immediate)
        {
            Apply(Head, pose.Head, pose.HeadRotation, immediate);
            Apply(LeftHand, pose.Left, pose.LeftRotation, immediate);
            Apply(RightHand, pose.Right, pose.RightRotation, immediate);
            Apply(Body, new Vector3(pose.Head.x, Mathf.Max(.35f,pose.Head.y*.48f),pose.Head.z),
                Quaternion.Euler(0,pose.HeadRotation.eulerAngles.y,0), immediate);
        }

        public override void OnDestroy()
        {
            foreach (var material in ownedMaterials)
            {
                if (material != null)
                    Destroy(material);
            }

            base.OnDestroy();
        }

        static void Apply(
            Transform target,
            Vector3 position,
            Quaternion rotation,
            bool immediate)
        {
            float t = immediate
                ? 1f
                : 1f - Mathf.Exp(
                    -20f * Time.unscaledDeltaTime
                );

            target.localPosition = Vector3.Lerp(
                target.localPosition,
                position,
                t
            );

            target.localRotation = Quaternion.Slerp(
                target.localRotation,
                rotation,
                t
            );
        }

        // =====================================================
        // EXISTING MULTIPLAYER AVATAR POSE SYNCHRONIZATION
        // =====================================================

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Owner,
            Delivery = RpcDelivery.Unreliable
        )]
        void SubmitPoseRpc(
            AvatarPose pose,
            RpcParams rpcParams = default)
        {
            // Default RPC ownership + explicit sender check.
            // Root placement remains host owned.

            if (rpcParams.Receive.SenderClientId != OwnerClientId ||
                !pose.IsValid())
                return;

            double now = NetworkManager.ServerTime.Time;

            if (now < nextHostPose)
                return;

            nextHostPose = now + .025;

            TrackedPose.Value = pose;
        }

        // =====================================================
        // NEW: MULTIPLAYER TELEPORTATION
        // =====================================================

        // Called by ArenaTeleportation when the local
        // player selects a valid teleport destination.
        public void RequestTeleport(Vector3 destination)
        {
            if (!IsSpawned || !IsOwner)
                return;

            RequestTeleportRpc(destination);
        }

        // The server validates and applies teleportation.
        // Clients cannot directly modify RootPosition.
        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Owner
        )]
        private void RequestTeleportRpc(
            Vector3 destination,
            RpcParams rpcParams = default)
        {
            // Verify that the request belongs to this player.
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            // Reject invalid coordinates.
            if (float.IsNaN(destination.x) ||
                float.IsNaN(destination.y) ||
                float.IsNaN(destination.z) ||
                float.IsInfinity(destination.x) ||
                float.IsInfinity(destination.y) ||
                float.IsInfinity(destination.z))
            {
                Debug.LogWarning(
                    "Teleportation rejected: invalid coordinates."
                );

                return;
            }

            // Find the arena teleportation manager.
            ArenaTeleportation teleport =
                FindFirstObjectByType<ArenaTeleportation>();

            if (teleport == null)
            {
                Debug.LogWarning(
                    "Teleportation rejected: " +
                    "ArenaTeleportation manager not found."
                );

                return;
            }

            // Validate the destination on the server.
            if (!teleport.IsServerValidDestination(destination))
            {
                Debug.LogWarning(
                    "Teleportation rejected: unsafe destination."
                );

                return;
            }

            // Prevent requests beyond the allowed distance.
            Vector3 currentPosition = RootPosition.Value;

            Vector2 currentXZ = new Vector2(
                currentPosition.x,
                currentPosition.z
            );

            Vector2 destinationXZ = new Vector2(
                destination.x,
                destination.z
            );

            float distance = Vector2.Distance(
                currentXZ,
                destinationXZ
            );

            // Small tolerance for headset offset.
            if (distance > teleport.maxDistance + 0.5f)
            {
                Debug.LogWarning(
                    "Teleportation rejected: destination too far."
                );

                return;
            }

            // Server-authoritative position update.
            RootPosition.Value = destination;

            Debug.Log(
                "Player " + OwnerClientId +
                " teleported to " + destination
            );
        }
    }
}