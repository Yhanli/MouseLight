using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Input;
using Game.Rendering;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterCursorLight
{
    internal partial class CursorGlowSystem : GameSystemBase
    {
        private RaycastSystem m_RaycastSystem;
        private LightingSystem m_LightingSystem;
        private Camera m_Camera;
        private readonly ExposureProbe m_Exposure = new ExposureProbe();
        private bool? m_LoggedLive;

        protected override void OnCreate()
        {
            base.OnCreate();
            // 使用独立射线检测，避免受到当前原版工具射线遮罩的影响。
            m_RaycastSystem = World.GetOrCreateSystemManaged<RaycastSystem>();
            m_LightingSystem = World.GetOrCreateSystemManaged<LightingSystem>();
        }

        protected override void OnUpdate()
        {
            CursorLightSettings settings = Mod.Settings;
            GameManager manager = GameManager.instance;

            // Keep the live exposure reading fresh even while the light is hidden (cursor over the UI, etc.),
            // so there is no jump to the fallback estimate when the cursor returns. Async and very cheap.
            m_Camera = m_Camera != null ? m_Camera : Camera.main;
            if (settings != null && settings.EnableCursorLight && m_Camera != null && m_Camera.isActiveAndEnabled)
            {
                m_Exposure.Request(m_Camera);
            }

            if (settings == null || !settings.EnableCursorLight || manager == null || manager.isGameLoading ||
                (manager.gameMode & GameMode.GameOrEditor) == 0 || Mouse.current == null || !InputManager.instance.controlOverWorld)
            {
                CursorLightController.Shared.Hide();
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (mousePosition.x < 0f || mousePosition.y < 0f || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
            {
                CursorLightController.Shared.Hide();
                return;
            }

            m_Camera = m_Camera != null ? m_Camera : Camera.main;
            if (m_Camera == null || !m_Camera.isActiveAndEnabled || m_RaycastSystem == null || m_LightingSystem == null)
            {
                CursorLightController.Shared.Hide();
                return;
            }

            // PreTool 阶段读取上一帧已完成的射线结果。
            NativeArray<RaycastResult> results = m_RaycastSystem.GetResult(this);
            if (results.IsCreated && results.Length > 0)
            {
                Color tint = GetTemperatureTint(settings);
                CursorLightController.Shared.Update(
                    results[0].m_Hit.m_HitPosition,
                    m_Camera.transform.position,
                    GetBrightnessFactor(settings),
                    settings.IntensityMultiplier,
                    settings.RangeMultiplier,
                    settings.LightDistance,
                    settings.LightReflections,
                    settings.ColorRed / 255f * tint.r,
                    settings.ColorGreen / 255f * tint.g,
                    settings.ColorBlue / 255f * tint.b);
            }
            else
            {
                CursorLightController.Shared.Hide();
            }

            // 按原版 ToolRaycastSystem 的算法计算摄像机射线，但不复用当前工具的遮罩。
            Ray ray = m_Camera.ScreenPointToRay(InputManager.instance.mousePosition);
            float3 direction = ray.direction;
            float3 forward = m_Camera.transform.forward;
            Line3.Segment line = new Line3.Segment
            {
                a = ray.origin,
                b = (float3)ray.origin + direction * m_Camera.farClipPlane /
                    math.clamp(math.dot(direction, forward), 0.25f, 1f)
            };

            RaycastInput input = new RaycastInput
            {
                m_Line = line,
                m_TypeMask = TypeMask.Terrain | TypeMask.StaticObjects | TypeMask.Net,
                m_CollisionMask = CollisionMask.OnGround | CollisionMask.Overground,
                m_Flags = RaycastFlags.SubElements | RaycastFlags.BuildingLots
            };
            m_RaycastSystem.AddInput(this, input);
        }

        // Scene exposure is very different by day and by night, so the same lumens look far brighter at night.
        // These scales were tuned in game so one intensity setting looks about the same in both:
        // intensity 2 looked right by day with the 2000 day scale, and ~0.35 looked right at night at the old
        // night scale of 1, i.e. night needs 0.35 / 2 = 0.175.
        private const float DayScale = 2000f;
        private const float NightScale = 0.175f;

        // On-screen brightness of a light = lumens x camera exposure multiplier, so scaling by the inverse of the
        // live exposure keeps the light consistent at any time of day, in any weather and at any zoom.
        // A pure inverse made daytime look too bright, so the curve is softened: factor = K x exposure^-P.
        // Fitted to in-game measurements (game 1.6.4):
        //   night: exposure 0.833   (EV100 0)  -> 0.175  (looked right at night)
        //   noon:  exposure 5.09e-5 (EV100 14) -> ~1500  (the 1966 of a pure inverse looked a little too bright)
        private const float ExposureCurveK = 0.1476f;
        private const float ExposureCurveP = 0.933f;
        private const float MinFactor = 0.01f;
        private const float MaxFactor = 50000f;

        private float GetBrightnessFactor(CursorLightSettings settings)
        {
            float factor;
            bool live = m_Exposure.TryGet(out float exposure);
            if (live)
            {
                factor = math.clamp(ExposureCurveK * math.pow(exposure, -ExposureCurveP), MinFactor, MaxFactor);
                if (!settings.EnableInDaytime)
                {
                    // Night only: fade out as the sun comes up.
                    factor *= GetNightFactor();
                }
            }
            else
            {
                factor = GetEstimatedBrightnessFactor(settings);
            }

            LogSourceChange(live, exposure, factor);
            return factor;
        }

        // Logs only when switching between the live exposure and the fallback estimate (useful after game updates).
        private void LogSourceChange(bool live, float exposure, float factor)
        {
            if (m_LoggedLive.HasValue && m_LoggedLive.Value == live)
            {
                return;
            }

            m_LoggedLive = live;
            float ev100 = exposure > 0f ? math.log2(1f / (1.2f * exposure)) : float.NaN;
            Logger.Info(live
                ? $"Brightness follows live camera exposure (EV100={ev100:F2}, factor={factor:G4})."
                : $"Live camera exposure unavailable; using the time-of-day estimate (factor={factor:G4}).");
        }

        // Fallback when the live exposure cannot be read: estimate from the time of day.
        private float GetEstimatedBrightnessFactor(CursorLightSettings settings)
        {
            float night = GetNightFactor();
            if (!settings.EnableInDaytime)
            {
                // Night only: fade in at dusk, off in daylight.
                return night * NightScale;
            }

            // Full daylight uses the day scale, full night the night scale, and dusk/dawn blend between them.
            // The blend is logarithmic (equal ratios per step), matching how scene exposure changes. A linear
            // blend between 2000 and 0.175 stays hundreds of times too bright until it is almost fully dark.
            float t = GetDaylightBlend();
            return math.exp(math.lerp(math.log(NightScale), math.log(DayScale), t));
        }

        // 0 = full night, 1 = full day, smooth in between. Wider than the night-only fade so the light
        // starts dimming earlier in the late afternoon and brightens more gradually in the morning.
        private float GetDaylightBlend()
        {
            float daylight = math.saturate(m_LightingSystem.dayLightBrightness);
            return math.smoothstep(0.05f, 0.6f, daylight);
        }

        private static Color GetTemperatureTint(CursorLightSettings settings)
        {
            if (!settings.WarmTone)
            {
                return Color.white;
            }

            // Real light-bulb color for this temperature, scaled so its brightest channel is 1
            // (the tint changes the hue only; brightness stays with the intensity setting).
            Color c = Mathf.CorrelatedColorTemperatureToRGB(Mathf.Clamp(settings.ColorTemperature, 1500f, 6500f));
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return max > 0.0001f ? new Color(c.r / max, c.g / max, c.b / max, 1f) : Color.white;
        }

        private float GetNightFactor()
        {
            // dayLightBrightness 是游戏归一化后的实时日光强度。
            float daylight = math.saturate(m_LightingSystem.dayLightBrightness);
            return math.smoothstep(0.4f, 0.08f, daylight);
        }

        protected override void OnDestroy()
        {
            CursorLightController.DisposeShared();
            base.OnDestroy();
        }
    }
}
