using UnityEngine;

namespace CursedCrown
{
    // Local-only visual effect; the host sends endpoints, never client-selected hits.
    public sealed class GunEffects : MonoBehaviour
    {
        [SerializeField] float lifetime = .08f;
        void OnEnable() => NetworkGun.ShotFired += Show;
        void OnDisable() => NetworkGun.ShotFired -= Show;
        void Show(ulong shooter, Vector3 origin, Vector3 end)
        {
            var avatar = GetComponent<FoundationAvatar>();
            if (avatar == null || avatar.OwnerClientId != shooter) return;
            var tracer = new GameObject("Shot tracer");
            var line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            line.startWidth = .018f;
            line.endWidth = .006f;
            line.useWorldSpace = true;
            line.startColor = Color.cyan;
            line.endColor = Color.white;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) line.sharedMaterial = new Material(shader);
            StartCoroutine(Cleanup(tracer, line.sharedMaterial));
        }
        System.Collections.IEnumerator Cleanup(GameObject tracer, Material material)
        {
            yield return new WaitForSeconds(lifetime);
            if (material != null) Destroy(material);
            if (tracer != null) Destroy(tracer);
        }
    }
}



