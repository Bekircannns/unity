using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UiTheme
{
    private const string ButtonNormalResource = "UI/btn_primary_normal";
    private const string ButtonPressedResource = "UI/btn_primary_pressed";
    private const string ButtonDisabledResource = "UI/btn_primary_disabled";

    private static readonly Dictionary<string, string> PanelSkinResources = new Dictionary<string, string>
    {
        { "menu_background", "UI/menu_bg" },
        { "menu_card", "UI/menu_panel" },
        { "gameplay_background", "UI/gameplay_bg" },
        { "gameplay_hud", "UI/hud_panel" },
        { "results_background", "UI/results_bg" },
        { "results_card", "UI/results_panel" }
    };

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    public static bool TryApplyPanelSkin(Image image, string panelSkinKey)
    {
        if (image == null || string.IsNullOrEmpty(panelSkinKey))
        {
            return false;
        }

        string resourcePath;
        if (!PanelSkinResources.TryGetValue(panelSkinKey, out resourcePath))
        {
            return false;
        }

        var sprite = LoadSprite(resourcePath);
        if (sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        return true;
    }

    public static bool TryApplyPrimaryButtonSkin(Button button, Image buttonImage)
    {
        if (button == null || buttonImage == null)
        {
            return false;
        }

        var normal = LoadSprite(ButtonNormalResource);
        if (normal == null)
        {
            return false;
        }

        var pressed = LoadSprite(ButtonPressedResource);
        var disabled = LoadSprite(ButtonDisabledResource);

        buttonImage.sprite = normal;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Color.white;

        button.transition = Selectable.Transition.SpriteSwap;
        var spriteState = button.spriteState;
        spriteState.highlightedSprite = pressed != null ? pressed : normal;
        spriteState.pressedSprite = pressed != null ? pressed : normal;
        spriteState.disabledSprite = disabled != null ? disabled : normal;
        button.spriteState = spriteState;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.56f);
        button.colors = colors;
        return true;
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        Sprite cached;
        if (SpriteCache.TryGetValue(resourcePath, out cached))
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(resourcePath);
        SpriteCache[resourcePath] = sprite;
        return sprite;
    }
}
