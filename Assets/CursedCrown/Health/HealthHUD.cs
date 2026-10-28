using UnityEngine;
using UnityEngine.UI;

namespace CursedCrown
{
    // Shows health and death status for the local player only.
    public sealed class HealthHUD : MonoBehaviour
    {
        public Text HealthText;
        public Text StatusText;

        PlayerHealth player;
        Canvas hudCanvas;

        void Update()
        {
            if (player == null)
                player = GetComponent<PlayerHealth>();

            if (player == null || !player.IsSpawned || !player.IsOwner)
                return;

            if (HealthText == null || StatusText == null)
                CreateHud();

            HealthText.text = "Health: " + player.CurrentHealth + "/" + GameRules.MaxHealth;
            StatusText.text = player.IsAlive ? "" : "ELIMINATED - RESPAWNING";
        }

        void CreateHud()
        {
            if (FoundationRig.Instance == null || FoundationRig.Instance.Head == null)
                return;

            GameObject canvasObject = new GameObject("Player Health HUD");
            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.WorldSpace;
            hudCanvas.sortingOrder = 100;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(300, 100);
            canvasObject.transform.SetParent(FoundationRig.Instance.Head, false);
            canvasObject.transform.localPosition = new Vector3(0, -0.18f, 0.65f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.00075f;

            GameObject panel = new GameObject("Health Panel");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(280, 82);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.03f, 0.05f, 0.08f, 0.85f);

            HealthText = CreateLabel("Health Text", panel.transform, new Vector2(12, -8));
            StatusText = CreateLabel("Status Text", panel.transform, new Vector2(12, -43));
        }

        Text CreateLabel(string objectName, Transform parent, Vector2 position)
        {
            GameObject label = new GameObject(objectName);
            label.transform.SetParent(parent, false);

            RectTransform rect = label.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(256, 30);

            Text text = label.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = "";
            return text;
        }

        void OnDestroy()
        {
            if (hudCanvas != null)
                Destroy(hudCanvas.gameObject);
        }
    }
}
