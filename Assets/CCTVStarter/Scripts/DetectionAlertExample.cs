using UnityEngine;
using UnityEngine.UI;

public class DetectionAlertExample : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.55f, 1f);
    [SerializeField] private Color alertColor = new Color(1f, 0.15f, 0.1f);
    [SerializeField] private Text alertText;
    [SerializeField] private string detectedMessage = "발각됨!";

    private Material runtimeMaterial;
    private CctvDetector[] subscribedDetectors;

    public void Configure(Text newAlertText)
    {
        alertText = newAlertText;
        SetAlertVisible(false);
    }

    private void OnEnable()
    {
        SubscribeToDetectors();
    }

    private void OnDisable()
    {
        if (subscribedDetectors == null)
        {
            return;
        }

        foreach (CctvDetector detector in subscribedDetectors)
        {
            if (detector == null)
            {
                continue;
            }

            detector.onDetected.RemoveListener(SetDetected);
            detector.onLost.RemoveListener(SetLost);
        }
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            runtimeMaterial.color = normalColor;
        }

        if (alertText == null)
        {
#if UNITY_2023_1_OR_NEWER
            alertText = FindFirstObjectByType<Text>();
#else
            alertText = FindObjectOfType<Text>();
#endif
        }

        if (alertText == null)
        {
            alertText = CreateAlertText();
        }

        SetAlertVisible(false);
    }

    private void SubscribeToDetectors()
    {
#if UNITY_2023_1_OR_NEWER
        subscribedDetectors = FindObjectsByType<CctvDetector>(FindObjectsSortMode.None);
#else
        subscribedDetectors = FindObjectsOfType<CctvDetector>();
#endif

        foreach (CctvDetector detector in subscribedDetectors)
        {
            detector.onDetected.RemoveListener(SetDetected);
            detector.onLost.RemoveListener(SetLost);
            detector.onDetected.AddListener(SetDetected);
            detector.onLost.AddListener(SetLost);
        }
    }

    public void SetDetected()
    {
        SetColor(alertColor);
        SetAlertVisible(true);
        Debug.Log("CCTV detected the player.");
    }

    public void SetLost()
    {
        SetColor(normalColor);
        SetAlertVisible(false);
        Debug.Log("CCTV lost the player.");
    }

    private void SetColor(Color color)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.color = color;
        }
    }

    private void SetAlertVisible(bool visible)
    {
        if (alertText == null)
        {
            return;
        }

        alertText.text = detectedMessage;
        alertText.gameObject.SetActive(visible);
    }

    private static Text CreateAlertText()
    {
        GameObject canvasObject = new GameObject("Detection_UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("Detection_Text");
        textObject.transform.SetParent(canvasObject.transform, false);

        Text text = textObject.AddComponent<Text>();
        text.text = "발각됨!";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = CreateUiFont();
        text.fontSize = 54;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.12f, 0.08f);

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -48f);
        rect.sizeDelta = new Vector2(420f, 90f);

        return text;
    }

    private static Font CreateUiFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 54);
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
