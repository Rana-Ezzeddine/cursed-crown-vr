using UnityEngine;
using UnityEngine.InputSystem;

namespace CursedCrown
{
    public class ArenaTeleportation : MonoBehaviour
    {
        [Header("Player References")]
        public Transform playerRig;
        public Transform playerHead;
        public Transform rightController;
        public Collider arenaFloor;

        [Header("Teleportation Settings")]
        public float maxDistance = 15f;
        public float playerRadius = 0.35f;

        [Header("Teleportation Visuals")]
        public LineRenderer teleportLine;
        public GameObject destinationMarker;

        private bool isAiming;
        private bool validDestination;
        private Vector3 targetPosition;

        void Start()
        {
            HideVisuals();
        }

        void Update()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null &&
                Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                var player = Unity.Netcode.NetworkManager.Singleton.LocalClient?.PlayerObject;
                var avatar = player != null ? player.GetComponent<FoundationAvatar>() : null;
                if (avatar == null || !avatar.CanTeleport)
                {
                    StopAiming();
                    return;
                }
            }
            // Desktop: hold T to aim, release to teleport.
            bool desktopHeld =
                Keyboard.current != null &&
                Keyboard.current.tKey.isPressed;

            // Quest: hold A to aim, release to teleport.
            bool questHeld = false;

            UnityEngine.XR.InputDevice controller =
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(
                    UnityEngine.XR.XRNode.RightHand
                );

            if (controller.isValid)
            {
                controller.TryGetFeatureValue(
                    UnityEngine.XR.CommonUsages.primaryButton,
                    out questHeld
                );
            }

            bool buttonHeld = desktopHeld || questHeld;

            if (buttonHeld)
            {
                isAiming = true;
                UpdateTeleportAim(questHeld);
            }
            else if (isAiming)
            {
                if (validDestination)
                    PerformTeleport();

                StopAiming();
            }
        }

        void UpdateTeleportAim(bool usingQuest)
        {
            if (playerHead == null || arenaFloor == null)
            {
                validDestination = false;
                HideVisuals();
                return;
            }

            Ray ray;

            // Quest: aim using the right controller.
            if (usingQuest && rightController != null)
            {
                ray = new Ray(
                    rightController.position,
                    rightController.forward
                );
            }
            // Desktop: aim using mouse or touchpad.
            else if (Mouse.current != null &&
                     Camera.main != null)
            {
                Vector2 cursorPosition =
                    Mouse.current.position.ReadValue();

                ray = Camera.main.ScreenPointToRay(
                    cursorPosition
                );
            }
            else
            {
                validDestination = false;
                HideVisuals();
                return;
            }

            Vector3 endPoint =
                ray.origin + ray.direction * maxDistance;

            validDestination = false;

            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;

                if (hit.collider == arenaFloor &&
                    IsDestinationClear(hit.point))
                {
                    validDestination = true;
                    targetPosition = hit.point;
                }
            }

            // Show aiming laser.
            if (teleportLine != null)
            {
                teleportLine.enabled = true;
                teleportLine.positionCount = 2;
                teleportLine.useWorldSpace = true;

                Vector3 laserStart = ray.origin;

                // Offset desktop laser so it remains visible.
                if (!usingQuest && Camera.main != null)
                {
                    laserStart +=
                        Camera.main.transform.forward * 0.3f;

                    laserStart +=
                        Camera.main.transform.right * 0.15f;

                    laserStart -=
                        Camera.main.transform.up * 0.15f;
                }

                teleportLine.SetPosition(0, laserStart);
                teleportLine.SetPosition(1, endPoint);

                Color color = validDestination
                    ? Color.green
                    : Color.red;

                teleportLine.startColor = color;
                teleportLine.endColor = color;
            }

            // Show destination marker.
            if (destinationMarker != null)
            {
                destinationMarker.SetActive(validDestination);

                if (validDestination)
                {
                    destinationMarker.transform.position =
                        targetPosition + Vector3.up * 0.03f;
                }
            }
        }

        // =====================================================
        // DESTINATION COLLISION VALIDATION
        // =====================================================

        bool IsDestinationClear(Vector3 destination)
        {
            float radius = playerRadius * 0.95f;

            Vector3 capsuleBottom =
                destination +
                Vector3.up * (playerRadius + 0.05f);

            Vector3 capsuleTop =
                destination + Vector3.up * 1.65f;

            Collider[] overlaps = Physics.OverlapCapsule(
                capsuleBottom,
                capsuleTop,
                radius,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider obstacle in overlaps)
            {
                if (obstacle == arenaFloor)
                    continue;

                if (playerRig != null &&
                    obstacle.transform.IsChildOf(playerRig))
                    continue;

                if (destinationMarker != null &&
                    obstacle.transform.IsChildOf(
                        destinationMarker.transform))
                    continue;

                return false;
            }

            return true;
        }

        // =====================================================
        // NEW: SERVER-SIDE DESTINATION VALIDATION
        // =====================================================

        // Called by FoundationAvatar on the server.
        public bool IsServerValidDestination(Vector3 destination)
        {
            if (arenaFloor == null)
                return false;

            // Reject invalid coordinates.
            if (float.IsNaN(destination.x) ||
                float.IsNaN(destination.y) ||
                float.IsNaN(destination.z) ||
                float.IsInfinity(destination.x) ||
                float.IsInfinity(destination.y) ||
                float.IsInfinity(destination.z))
            {
                return false;
            }

            // Check that the destination is on the arena floor.
            Vector3 rayStart =
                destination + Vector3.up * 1f;

            if (!Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                2f,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.collider != arenaFloor)
                return false;

            // Ensure destination is close to floor height.
            if (Vector3.Distance(
                hit.point,
                destination
            ) > 0.15f)
            {
                return false;
            }

            // Reject obstacles and occupied destinations
            // when those objects have detectable colliders.
            return IsDestinationClear(hit.point);
        }

        // =====================================================
        // UPDATED: LOCAL + MULTIPLAYER TELEPORTATION
        // =====================================================

        void PerformTeleport()
        {
            if (playerRig == null ||
                playerHead == null ||
                !validDestination)
            {
                return;
            }

            // Preserve the player's horizontal headset offset.
            Vector3 headOffset =
                playerHead.position - playerRig.position;

            headOffset.y = 0f;

            Vector3 newRootPosition =
                targetPosition - headOffset;

            // Find the local player's networked avatar.
            FoundationAvatar[] avatars =
                FindObjectsByType<FoundationAvatar>(
                    FindObjectsSortMode.None
                );

            foreach (FoundationAvatar avatar in avatars)
            {
                if (!avatar.IsOwner || !avatar.IsSpawned)
                    continue;

                // Multiplayer: request server authorization.
                avatar.RequestTeleport(newRootPosition);

                Debug.Log(
                    "Multiplayer teleport requested."
                );

                return;
            }

            // If a network session is active but our avatar
            // isn't ready, don't bypass server authorization.
            if (Unity.Netcode.NetworkManager.Singleton != null &&
                Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning(
                    "Teleport unavailable: local network avatar not ready."
                );

                return;
            }

            // Offline desktop testing.
            playerRig.position = newRootPosition;

            Debug.Log("Local teleport completed.");
        }

        // =====================================================
        // VISUAL HELPERS
        // =====================================================

        void HideVisuals()
        {
            if (teleportLine != null)
                teleportLine.enabled = false;

            if (destinationMarker != null)
                destinationMarker.SetActive(false);
        }

        void StopAiming()
        {
            isAiming = false;
            validDestination = false;
            HideVisuals();
        }
    }
}