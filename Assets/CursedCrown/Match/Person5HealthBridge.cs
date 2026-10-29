using UnityEngine;

namespace CursedCrown
{
    // Person 5 adapter for the unchanged PlayerHealth supplied by Person 4.
    // Attach beside MatchController in an integration match prefab.
    [RequireComponent(typeof(MatchController))]
    public sealed class Person5HealthBridge : MonoBehaviour, IPlayerState
    {
        public bool IsAlive(ulong id)
        {
            var health = FindHealth(id);
            return health != null && health.CanAct;
        }
        // Charge is Person 3's responsibility; this adapter does not claim to provide it.
        public int GetCharge(ulong id) => 0;
        public static PlayerHealth FindHealth(ulong id)
        {
            var avatar = MatchController.FindAvatar(id);
            return avatar != null ? avatar.GetComponent<PlayerHealth>() : null;
        }
        // Until P4 validates round/phase itself, P3 can use this guarded boundary.
        // This does not validate aim, cover, damage or shot rate: P3 must do that first.
        public bool TryApplyValidatedHit(ValidatedHit hit)
        {
            var match = GetComponent<MatchController>();
            if (!match.IsSpawned || !match.IsServer || match.Phase != MatchPhase.Playing || hit.Round != match.RoundId)
                return false;
            var health = FindHealth(hit.Target);
            return health != null && health.ApplyValidatedHit(hit);
        }
    }
}
