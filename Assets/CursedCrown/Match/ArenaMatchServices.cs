using UnityEngine;

namespace CursedCrown
{
    // Adapts P2's slot-based reservations to the shared ulong client-ID contract.
    // Initial spawning and respawning use the same manager and session slot map.
    public sealed class ArenaMatchServices : MonoBehaviour, ISpawnService
    {
        public ArenaSpawnManager Spawns;
        public CrownDropManager Drops;
        void OnEnable() => PlayerHealth.SetSpawnService(this);
        void OnDisable()
        {
            if (ReferenceEquals(PlayerHealth.SpawnService, this)) PlayerHealth.SetSpawnService(null);
        }
        public bool TryReserveSpawn(ulong id, out Pose pose)
        {
            pose = default;
            var session = FoundationSession.Instance;
            if (session == null || !session.Manager.IsServer || Spawns == null ||
                !session.TryGetSpawnSlot(id, out int slot)) return false;
            if (!Spawns.TryReserveSpawn(slot, out var position, out var rotation)) return false;
            pose = new Pose(position, rotation);
            return true;
        }
        public void ReleaseReservation(ulong id)
        {
            var session = FoundationSession.Instance;
            if (session != null && session.Manager.IsServer && Spawns != null &&
                session.TryGetSpawnSlot(id, out int slot)) Spawns.ReleaseSpawn(slot);
        }
        public Vector3 GetValidCrownDrop(Vector3 requested)
        {
            if (Drops != null && Drops.TryGetSafeDropPosition(out var position)) return position;
            // MatchController validates this sentinel and uses its pedestal fallback.
            return new Vector3(float.NaN, 0, 0);
        }
    }
}
