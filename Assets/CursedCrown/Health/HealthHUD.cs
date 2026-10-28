using UnityEngine;
using UnityEngine.UI;

namespace CursedCrown
{
    // Displays the local player's health and death status.
    public sealed class HealthHUD : MonoBehaviour
    {
        public Text HealthText;
        public Text StatusText;

        PlayerHealth player;

        void Update()
        {
            if (player == null || !player.IsSpawned)
                player = PlayerHealth.LocalPlayer;

            if (player == null || HealthText == null)
                return;

            HealthText.text = "Health: " + player.CurrentHealth + "/" + GameRules.MaxHealth;

            if (StatusText != null)
                StatusText.text = player.IsAlive ? "" : "ELIMINATED - RESPAWNING";
        }
    }
}
