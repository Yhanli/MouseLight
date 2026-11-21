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

        private float GetBrightnessFactor(CursorLightSettings settings)
        {
            float night = GetNightFactor();
            if (!settings.EnableInDaytime)
            {
                // Night only: fade in at dusk, off in daylight.
                return night * NightScale;
            }

            // Full daylight uses the day scale, full night the night scale, and dusk/dawn blend between them.
            return math.lerp(DayScale, NightScale, night);
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
