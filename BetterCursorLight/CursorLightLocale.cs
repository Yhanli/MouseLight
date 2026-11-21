using System.Collections.Generic;
using Colossal;

namespace BetterCursorLight
{
    internal sealed class CursorLightLocale : IDictionarySource
    {
        private readonly IReadOnlyDictionary<string, string> m_Entries;

        private CursorLightLocale(IReadOnlyDictionary<string, string> entries)
        {
            m_Entries = entries;
        }

        // 英语是默认语言，也是其他语言缺失时的文本基准。
        internal static CursorLightLocale CreateEnglish(CursorLightSettings settings)
        {
            return new CursorLightLocale(CreateEntries(
                settings,
                "Better Cursor Light",
                "General",
                "Cursor light",
                "Enable cursor light",
                "Illuminate the game world position pointed to by the mouse in dark environments.",
                "Light intensity multiplier",
                "Controls the illumination intensity multiplier of the cursor spotlight.",
                "Range multiplier",
                "Controls how much the target illumination radius changes with camera distance.",
                "Red channel",
                "Red component of the light color (0-255).",
                "Green channel",
                "Green component of the light color (0-255).",
                "Blue channel",
                "Blue component of the light color (0-255).",
                "Reset settings",
                "Restore all Better Cursor Light settings to their defaults.",
                "Also light up during the day",
                "Keep the cursor light on in daylight, so places in building shadows can be seen clearly.",
                "Light distance",
                "Moves the light further back from the cursor towards the camera (in metres). A larger distance spreads the light over a wider area, making it softer and less concentrated.",
                "Light reflections",
                "Show shiny highlights from the light on roads, water, glass and other glossy surfaces. Turn off for softer, matte lighting.",
                "Warm tone",
                "Tint the light like a real light bulb using a color temperature.",
                "Color temperature (K)",
                "Lower is warmer: 1500 K candle, 2700-3000 K warm bulb, 4000 K neutral, 6500 K daylight white."));
        }

        // 简体中文设置文本。
        internal static CursorLightLocale CreateChinese(CursorLightSettings settings)
        {
            return new CursorLightLocale(CreateEntries(
                settings,
                "Better Cursor Light",
                "常规",
                "光标照明",
                "启用光标照明",
                "在较暗环境中照亮鼠标指向的游戏世界位置。",
                "灯光亮度倍率",
                "控制鼠标聚光灯的光照强度倍率。",
                "范围倍率",
                "控制目标照明半径随摄像机距离变化的幅度。",
                "红色通道",
                "灯光颜色的红色分量（0-255）。",
                "绿色通道",
                "灯光颜色的绿色分量（0-255）。",
                "蓝色通道",
                "灯光颜色的蓝色分量（0-255）。",
                "重置设置",
                "将 Better Cursor Light 的全部设置恢复为默认值。",
                "白天也启用照明",
                "白天也保持光标照明，以便看清建筑阴影中的区域。",
                "灯光距离",
                "将灯光从光标位置向摄像机方向后移（单位：米）。距离越大，光照范围越广，光线越柔和、越不集中。",
                "灯光反射",
                "在道路、水面、玻璃等光滑表面上显示灯光的高光反射。关闭后光照更柔和、无反光。",
                "暖色调",
                "使用色温为灯光着色，效果类似真实灯泡。",
                "色温（K）",
                "数值越低越暖：1500 K 烛光，2700-3000 K 暖光灯泡，4000 K 中性光，6500 K 日光白。"));
        }

        // 德语设置文本。
        internal static CursorLightLocale CreateGerman(CursorLightSettings settings)
        {
            return new CursorLightLocale(CreateEntries(
                settings,
                "Better Cursor Light",
                "Allgemein",
                "Cursor-Beleuchtung",
                "Cursor-Beleuchtung aktivieren",
                "Beleuchtet in dunkler Umgebung die vom Mauszeiger markierte Position in der Spielwelt.",
                "Lichtstärke-Multiplikator",
                "Steuert den Multiplikator der Beleuchtungsstärke des Maus-Spotlights.",
                "Reichweitenmultiplikator",
                "Steuert, wie stark sich der Zielbeleuchtungsradius mit der Kameradistanz verändert.",
                "Rotkanal",
                "Rotanteil der Lichtfarbe (0-255).",
                "Grünkanal",
                "Grünanteil der Lichtfarbe (0-255).",
                "Blaukanal",
                "Blauanteil der Lichtfarbe (0-255).",
                "Einstellungen zurücksetzen",
                "Setzt alle Einstellungen von Better Cursor Light auf die Standardwerte zurück.",
                "Auch tagsüber beleuchten",
                "Hält das Cursorlicht auch bei Tageslicht an, damit Bereiche im Schatten von Gebäuden gut sichtbar sind.",
                "Lichtabstand",
                "Verschiebt das Licht vom Cursor weiter in Richtung Kamera (in Metern). Ein größerer Abstand verteilt das Licht auf eine größere Fläche, wodurch es weicher und weniger konzentriert wirkt.",
                "Lichtreflexionen",
                "Zeigt Glanzlichter des Lichts auf Straßen, Wasser, Glas und anderen glänzenden Oberflächen. Ausschalten für weicheres, mattes Licht.",
                "Warmer Farbton",
                "Tönt das Licht wie eine echte Glühbirne über eine Farbtemperatur.",
                "Farbtemperatur (K)",
                "Niedriger ist wärmer: 1500 K Kerze, 2700-3000 K warme Glühbirne, 4000 K neutral, 6500 K Tageslichtweiß."));
        }

        private static IReadOnlyDictionary<string, string> CreateEntries(
            CursorLightSettings settings,
            string title,
            string tab,
            string group,
            string enabledLabel,
            string enabledDescription,
            string intensityLabel,
            string intensityDescription,
            string rangeLabel,
            string rangeDescription,
            string redLabel,
            string redDescription,
            string greenLabel,
            string greenDescription,
            string blueLabel,
            string blueDescription,
            string resetLabel,
            string resetDescription,
            string daytimeLabel,
            string daytimeDescription,
            string distanceLabel,
            string distanceDescription,
            string reflectionsLabel,
            string reflectionsDescription,
            string warmLabel,
            string warmDescription,
            string temperatureLabel,
            string temperatureDescription)
        {
            // 所有语言共用 ModSetting 生成的稳定本地化键。
            return new Dictionary<string, string>
            {
                { settings.GetSettingsLocaleID(), title },
                { settings.GetOptionTabLocaleID(CursorLightSettings.GeneralSection), tab },
                { settings.GetOptionGroupLocaleID(CursorLightSettings.GeneralGroup), group },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.EnableCursorLight)), enabledLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.EnableCursorLight)), enabledDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.IntensityMultiplier)), intensityLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.IntensityMultiplier)), intensityDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.RangeMultiplier)), rangeLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.RangeMultiplier)), rangeDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.ColorRed)), redLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.ColorRed)), redDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.ColorGreen)), greenLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.ColorGreen)), greenDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.ColorBlue)), blueLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.ColorBlue)), blueDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.ResetSettings)), resetLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.ResetSettings)), resetDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.EnableInDaytime)), daytimeLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.EnableInDaytime)), daytimeDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.LightDistance)), distanceLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.LightDistance)), distanceDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.LightReflections)), reflectionsLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.LightReflections)), reflectionsDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.WarmTone)), warmLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.WarmTone)), warmDescription },
                { settings.GetOptionLabelLocaleID(nameof(CursorLightSettings.ColorTemperature)), temperatureLabel },
                { settings.GetOptionDescLocaleID(nameof(CursorLightSettings.ColorTemperature)), temperatureDescription }
            };
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return m_Entries;
        }

        public void Unload()
        {
        }
    }
}
