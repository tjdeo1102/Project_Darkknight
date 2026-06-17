using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class EasternFantasyUI
{
    public static readonly Color Ink = new(0.025f, 0.035f, 0.032f, 0.94f);
    public static readonly Color InkSoft = new(0.06f, 0.075f, 0.065f, 0.9f);
    public static readonly Color Jade = new(0.28f, 0.62f, 0.48f, 1f);
    public static readonly Color Vermilion = new(0.68f, 0.12f, 0.08f, 1f);
    public static readonly Color Gold = new(0.78f, 0.62f, 0.3f, 1f);
    public static readonly Color Paper = new(0.88f, 0.84f, 0.72f, 1f);

    public static void StylePanel(Image image, float alpha = 0.92f)
    {
        if (image == null) return;

        image.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
        var outline = image.GetComponent<Outline>();
        if (outline == null)
            outline = image.gameObject.AddComponent<Outline>();
        if (outline == null) return;

        outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.72f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    public static void StyleButton(Button button)
    {
        if (button == null) return;

        var image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = InkSoft;
            var outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = image.gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.55f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }

        var colors = button.colors;
        colors.normalColor = InkSoft;
        colors.highlightedColor = new Color(Jade.r, Jade.g, Jade.b, 0.9f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(Gold.r, Gold.g, Gold.b, 0.9f);
        colors.disabledColor = new Color(Ink.r, Ink.g, Ink.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.color = Paper;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 0f;
        }
    }

    public static TextMeshProUGUI CreateLabel(
        Transform parent,
        string objectName,
        TextMeshProUGUI fontSource)
    {
        var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = fontSource != null ? fontSource.font : TMP_Settings.defaultFontAsset;
        label.color = Paper;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.characterSpacing = 0f;
        return label;
    }
}
