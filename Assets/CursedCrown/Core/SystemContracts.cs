using UnityEngine;

namespace CursedCrown
{
    // Plain contracts: no networking is implemented here. Sender IDs must come
    // from authenticated RPC metadata, never a client-supplied player ID.
    public interface ISpawnService
    {
        // Host serializes requests; successful reservations stay occupied until released.
        bool TryReserveSpawn(ulong playerId, out Pose pose);
        void ReleaseReservation(ulong playerId);
        Vector3 GetValidCrownDrop(Vector3 requestedPosition);
    }

    public interface IPlayerState
    {
        bool IsAlive(ulong playerId);
        int GetCharge(ulong playerId);
    }

    public interface IMatchState
    {
        MatchPhase Phase { get; }
    }

    public interface IRoundResettable
    {
        // Called on the host once per new round; each owner replicates its own reset.
        void ResetRound();
    }

    // Host-generated after shot validation. Round + shooter + shot sequence
    // uniquely identify a shot so duplicate deliveries cannot apply damage twice.
    public readonly struct ValidatedHit
    {
        public readonly uint Round;
        public readonly ulong Shooter;
        public readonly ulong Target;
        public readonly uint ShotSequence;
        public readonly Vector3 Point;
        public ValidatedHit(uint round, ulong shooter, ulong target, uint sequence, Vector3 point)
        { Round = round; Shooter = shooter; Target = target; ShotSequence = sequence; Point = point; }
    }
}
