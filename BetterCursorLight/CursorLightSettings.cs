using Game.Modding;
using Game.Settings;
using Colossal.IO.AssetDatabase;

namespace BetterCursorLight
{
    [FileLocation(nameof(BetterCursorLight))]
    [SettingsUIGroupOrder(GeneralGroup)]
    internal sealed class CursorLightSettings : ModSetting
    {
        internal const string GeneralSection = "General";
        internal const string GeneralGroup = "CursorLight";

        private bool m_IsLoadingDefaults;
        private bool m_EnableCursorLight;
        private bool m_EnableInDaytime;
        private float m_IntensityMultiplier;
        private float m_RangeMultiplier;
        private float m_LightDistance;
        private bool m_LightReflections;
        private bool m_WarmTone;
        private float m_ColorTemperature;
        private float m_Red;
        private float m_Green;
        private float m_Blue;

        internal CursorLightSettings(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        // 启用或关闭鼠标灯光，修改后在下一次系统更新时生效。
        [SettingsUISection(GeneralSection, GeneralGroup)]
        public bool EnableCursorLight
        {
            get => m_EnableCursorLight;
            set => SetAndSave(ref m_EnableCursorLight, value);
        }

        // Also light the cursor during the day (e.g. inside building shadows).
        [SettingsUISection(GeneralSection, GeneralGroup)]
        public bool EnableInDaytime
        {
            get => m_EnableInDaytime;
            set => SetAndSave(ref m_EnableInDaytime, value);
        }

        // 控制聚光灯的照度倍率。
        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUISlider(min = 0.1f, max = 10f, step = 0.1f, scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 1, maxValueWithFraction = 10f, separateThousands = false)]
        public float IntensityMultiplier
        {
            get => m_IntensityMultiplier;
            set => SetAndSave(ref m_IntensityMultiplier, value);
        }

        // 控制随摄像机距离动态变化的目标光斑半径倍率。
        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUISlider(min = 1f, max = 20f, step = 1f, scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 0, maxValueWithFraction = 0f, separateThousands = false)]
        public float RangeMultiplier
        {
            get => m_RangeMultiplier;
            set => SetAndSave(ref m_RangeMultiplier, value);
        }

        // Extra distance (metres) the light is pulled back from the cursor towards the camera.
        // A more distant light spreads over a wider area, so it looks softer and less concentrated.
        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUISlider(min = 0f, max = 500f, step = 5f, scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 0, maxValueWithFraction = 0f, separateThousands = false)]
        public float LightDistance
        {
            get => m_LightDistance;
            set => SetAndSave(ref m_LightDistance, value);
        }

        // When off, the light only brightens surfaces and adds no shiny highlights (specular reflections)
        // on roads, water, glass and other glossy materials.
        [SettingsUISection(GeneralSection, GeneralGroup)]
        public bool LightReflections
        {
            get => m_LightReflections;
            set => SetAndSave(ref m_LightReflections, value);
        }

        // Color channels on the standard 0-255 scale (255/255/255 = white).
        // Named Color* (not Red/Green/Blue) so values saved by older versions on the 0-1 scale are not
        // read as near-black; those old keys are simply ignored and these defaults are used instead.
        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUISlider(min = 0f, max = 255f, step = 1f, scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 0, maxValueWithFraction = 0f, separateThousands = false)]
        public float ColorRed
        {
            get => m_Red;
            set => SetAndSave(ref m_Red, value);
        }

        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUISlider(min = 0f, max = 255f, step = 1f, scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 0, maxValueWithFraction = 0f, separateThousands = false)]
        public float ColorGreen
        {
            get => m_Green;
            set => SetAndSave(ref m_Green, value);
        }

        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUISlider(min = 0f, max = 255f, step = 1f, scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 0, maxValueWithFraction = 0f, separateThousands = false)]
        public float ColorBlue
        {
            get => m_Blue;
            set => SetAndSave(ref m_Blue, value);
        }

        // Tint the light with a real-world color temperature (like a light bulb), on top of the RGB color.
        [SettingsUISection(GeneralSection, GeneralGroup)]
        public bool WarmTone
        {
            get => m_WarmTone;
            set => SetAndSave(ref m_WarmTone, value);
        }

        // Kelvin: 1500 = candle, 2700-3000 = warm bulb, 4000 = neutral, 6500 = daylight white.
        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUISlider(min = 1500f, max = 6500f, step = 100f, scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 0, maxValueWithFraction = 0f, separateThousands = false)]
        [SettingsUIHideByCondition(typeof(CursorLightSettings), nameof(IsWarmToneOff))]
        public float ColorTemperature
        {
            get => m_ColorTemperature;
            set => SetAndSave(ref m_ColorTemperature, value);
        }

        public bool IsWarmToneOff() => !m_WarmTone;

        // 点击后恢复全部默认值，并立即刷新界面与持久化文件。
        [SettingsUISection(GeneralSection, GeneralGroup)]
        [SettingsUIButton]
        public bool ResetSettings
        {
            set
            {
                SetDefaults();
                ApplyAndSave();
            }
        }

        public override void SetDefaults()
        {
            // 批量恢复默认值时只在最后保存一次，避免连续写入设置文件。
            m_IsLoadingDefaults = true;
            m_EnableCursorLight = true;
            m_EnableInDaytime = true;
            m_IntensityMultiplier = 3f;
            m_RangeMultiplier = 2f;
            m_LightDistance = 50f;
            m_LightReflections = false;
            m_Red = 255f;
            m_Green = 255f;
            m_Blue = 255f;
            m_WarmTone = true;
            m_ColorTemperature = 6000f;
            m_IsLoadingDefaults = false;
        }

        private void SetAndSave<T>(ref T field, T value)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            if (!m_IsLoadingDefaults)
            {
                ApplyAndSave();
            }
        }
    }
}
