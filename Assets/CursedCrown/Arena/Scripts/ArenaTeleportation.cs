using UnityEngine;
using UnityEngine.InputSystem;

namespace CursedCrown
{
    [DefaultExecutionOrder(50)]
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
        GameObject landingHints;
        Mesh hintMesh;
        Material hintMaterial;
        public int LandingHintCount { get; private set; }

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

            var localPlayer = Unity.Netcode.NetworkManager.Singleton?.LocalClient?.PlayerObject;
            var owner = localPlayer != null ? localPlayer.GetComponent<FoundationAvatar>() : null;
            float nearest = maxDistance;
            foreach (var hit in Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IgnoreCollider(hit.collider, owner) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                endPoint = hit.point;
                validDestination = hit.collider == arenaFloor && TryResolveLanding(hit.point, ray.origin, owner, out targetPosition);
                if(validDestination)endPoint=targetPosition;
            }

            if(landingHints==null)BuildLandingHints();
            landingHints.SetActive(true);

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

        bool IgnoreCollider(Collider collider, FoundationAvatar owner)
        {
            if (owner != null && collider.transform.IsChildOf(owner.transform)) return true;
            if (playerRig != null && collider.transform.IsChildOf(playerRig)) return true;
            if (destinationMarker != null && collider.transform.IsChildOf(destinationMarker.transform)) return true;
            // Runtime menu tiles and floor markings are not arena obstacles.
            var presentation = collider.GetComponentInParent<MatchPresentation>();
            if ((presentation != null && presentation.IsControlCollider(collider)) ||
                collider.GetComponentInParent<FoundationWorldMenu>() != null) return true;
            return collider != arenaFloor && collider.gameObject.name.StartsWith("Spawn Marker");
        }

        bool IsDestinationClear(Vector3 destination, FoundationAvatar owner)
        {
            foreach (var obstacle in Physics.OverlapCapsule(
                destination + Vector3.up * (playerRadius + .05f),
                destination + Vector3.up * (1.8f - playerRadius),
                playerRadius, ~0, QueryTriggerInteraction.Ignore))
                if (obstacle != arenaFloor && !IgnoreCollider(obstacle, owner)) return false;
            return true;
        }

        public bool IsServerValidDestination(Vector3 destination) => IsServerValidDestination(destination, null);
        public bool IsServerValidDestination(Vector3 destination, FoundationAvatar owner)
        {
            if (arenaFloor == null || !Finite(destination)) return false;
            // Query the actual floor, not a ray that can hit our own body or a menu.
            if (!arenaFloor.Raycast(new Ray(destination + Vector3.up, Vector3.down), out var hit, 2) ||
                Mathf.Abs(hit.point.y - destination.y) > .15f) return false;
            return IsDestinationClear(hit.point, owner);
        }

        // Forgive small aiming errors at the edge of cover, but never snap through a wall.
        public bool TryResolveLanding(Vector3 point, Vector3 origin, FoundationAvatar owner, out Vector3 landing)
        {
            landing=point;
            if(IsServerValidDestination(point,owner))return true;
            for(int radius=1;radius<=3;radius++)
                for(int i=0;i<12;i++)
                {
                    float angle=i*Mathf.PI/6;
                    var candidate=point+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(radius*.25f);
                    if(!IsServerValidDestination(candidate,owner) || Vector3.Distance(origin,candidate)>maxDistance)continue;
                    var delta=candidate+Vector3.up*.025f-origin;
                    bool blocked=false;
                    foreach(var hit in Physics.RaycastAll(origin,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                        if(hit.collider!=arenaFloor && !IgnoreCollider(hit.collider,owner)){blocked=true;break;}
                    if(blocked)continue;
                    landing=candidate;return true;
                }
            return false;
        }
        public void BuildLandingHints()
        {
            if(landingHints!=null)return;
            Physics.SyncTransforms();
            var points=new System.Collections.Generic.List<Vector3>();
            for(float x=-12;x<=12;x+=3)
                for(float z=-12;z<=12;z+=3)
                    points.Add(new Vector3(x,0,z));
            // The coarse grid misses the pickup radius: provide close approaches on every side.
            var crownCentre=MatchController.Instance!=null?MatchController.Instance.Pedestal:Vector3.zero;
            crownCentre.y=arenaFloor.bounds.max.y;
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;
                float radius=i%2==0?1.15f:1.3f;
                points.Add(crownCentre+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius);
            }
            for(int side=0;side<4;side++)
                foreach(var p in new[]{new Vector3(10,0,7),new Vector3(7,0,10),new Vector3(10,0,10),new Vector3(10,0,5.5f),new Vector3(5.5f,0,10)})
                    points.Add(Quaternion.Euler(0,side*90,0)*p);
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            foreach(var p in points)
            {
                if(!IsServerValidDestination(p))continue;
                LandingHintCount++;
                for(int i=0;i<24;i++)
                {
                    int start=vertices.Count;
                    float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
                    vertices.Add(p+new Vector3(Mathf.Sin(a)*.20f,.018f,Mathf.Cos(a)*.20f));
                    vertices.Add(p+new Vector3(Mathf.Sin(a)*.17f,.018f,Mathf.Cos(a)*.17f));
                    vertices.Add(p+new Vector3(Mathf.Sin(b)*.17f,.018f,Mathf.Cos(b)*.17f));
                    vertices.Add(p+new Vector3(Mathf.Sin(b)*.20f,.018f,Mathf.Cos(b)*.20f));
                    triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                }
            }
            hintMesh=new Mesh {name="Teleport landing guidance"};hintMesh.SetVertices(vertices);hintMesh.SetTriangles(triangles,0);hintMesh.RecalculateNormals();
            landingHints=new GameObject("Teleport landing hints",typeof(MeshFilter),typeof(MeshRenderer));
            landingHints.GetComponent<MeshFilter>().sharedMesh=hintMesh;
            var source=FindFirstObjectByType<MatchPresentation>()?.PanelMaterial;
            if(source==null && teleportLine!=null)source=teleportLine.sharedMaterial;
            hintMaterial=source!=null?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            hintMaterial.color=new Color(.75f,.6f,.3f);
            hintMaterial.SetFloat("_Cull",0);
            landingHints.GetComponent<Renderer>().sharedMaterial=hintMaterial;
            landingHints.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            landingHints.SetActive(false);
        }
        void OnDestroy()
        {if(landingHints!=null)Destroy(landingHints);if(hintMesh!=null)Destroy(hintMesh);if(hintMaterial!=null)Destroy(hintMaterial);}

        static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
            !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);

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
            if(landingHints!=null)landingHints.SetActive(false);
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