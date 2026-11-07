using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CursedCrown
{
    // Temporary raycast shooter for testing Player 4 health without Person 3's gun.
    public sealed class HealthTestShooter : NetworkBehaviour
    {
        const float MaxShotDistance = 30f;
        [SerializeField] bool useXRTrigger = true;
        uint nextShotSequence = 1;

        void Update()
        {
            if (!IsSpawned || !IsOwner)
                return;

            PlayerHealth player = GetComponent<PlayerHealth>();
            if (player == null || !player.CanAct)
                return;

            MatchController match = MatchController.Instance;
            if (match != null && match.Phase != MatchPhase.Playing)
                return;

            bool triggerPressed = useXRTrigger &&
                FoundationRig.Instance != null &&
                FoundationRig.Instance.TriggerPressed;
            bool spacePressed = Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame;

            if (triggerPressed || spacePressed)
                RequestTestShotRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void RequestTestShotRpc(RpcParams rpcParams = default)
        {
            if (!IsServer || !IsSpawned ||
                rpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            MatchController match = MatchController.Instance;
            if (match != null && (!match.IsSpawned || !match.IsServer ||
                match.Phase != MatchPhase.Playing))
                return;

            PlayerHealth shooter = GetComponent<PlayerHealth>();
            FoundationAvatar avatar = GetComponent<FoundationAvatar>();
            if (shooter == null || avatar == null || !shooter.CanAct ||
                avatar.RightHand == null)
                return;

            Physics.SyncTransforms();
            Ray ray = new Ray(avatar.RightHand.position, avatar.RightHand.forward);
            RaycastHit[] hits = Physics.RaycastAll(
                ray, MaxShotDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                PlayerHealth target = hit.collider.GetComponentInParent<PlayerHealth>();

                if (target == shooter)
                    continue;

                if (target == null)
                {
                    Debug.Log("Health test shot blocked by: " + hit.collider.name);
                    return;
                }

                ValidatedHit validatedHit = new ValidatedHit(
                    shooter.CurrentRound, OwnerClientId, target.OwnerClientId,
                    nextShotSequence++, hit.point);

                Person5HealthBridge bridge = match != null
                    ? match.GetComponent<Person5HealthBridge>() : null;
                bool accepted = bridge != null
                    ? bridge.TryApplyValidatedHit(validatedHit)
                    : target.ApplyValidatedHit(validatedHit);
                Debug.Log("Health test shot hit client " + target.OwnerClientId +
                    ". Accepted: " + accepted + "; health: " +
                    target.CurrentHealth + "/" + GameRules.MaxHealth);
                return;
            }

            Debug.Log("Health test shot missed. Aim at another player's body.");
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !IsSpawned || !IsOwner)
                return;

            GUILayout.BeginArea(new Rect(20, 20, 440, 42), GUI.skin.box);
            GUILayout.Label("HEALTH TEST SHOT: Aim with right hand, then press Space (desktop) or XR Trigger");
            GUILayout.EndArea();
        }
    }
}
