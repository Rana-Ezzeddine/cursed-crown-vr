namespace CursedCrown
{
    // Proposed tuning, not assignment requirements. Host implementations own these values.
    public static class GameRules
    {
        public const int MaxPlayers = 5;
        public const int MatchSeconds = 240;
        public const int MaxHealth = 100;
        public const int ShotDamage = 25;
        public const int MaxCharge = 10;
        public const float RechargeSeconds = 2f;
        public const float RespawnSeconds = 3f;
        public const float CrownPickupDistance = 1f;
    }

    public enum MatchPhase { Lobby, Playing, Finished }
}
