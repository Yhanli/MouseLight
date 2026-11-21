using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace BetterCursorLight
{
    /// <summary>
    /// Reads the camera's live HDRP exposure multiplier, i.e. the factor every light's brightness is
    /// multiplied by on screen. HDRP keeps it in a 1x1 GPU texture (x = exposure multiplier); we copy that
    /// pixel back asynchronously, so the value is usually one or two frames old and never stalls the GPU.
    /// The texture getter is internal to HDRP, so it is called through reflection. If anything is missing
    /// (an HDRP change in a game update, no async readback support), <see cref="TryGet"/> returns false and
    /// the caller falls back to the time-of-day estimate.
    /// </summary>
    internal sealed class ExposureProbe
    {
        private const float MaxAgeSeconds = 2f;

        private static readonly MethodInfo s_GetExposureTexture = typeof(HDRenderPipeline).GetMethod(
            "GetExposureTexture",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            new[] { typeof(HDCamera) },
            null);

        private readonly Action<AsyncGPUReadbackRequest> m_OnReadback;
        private bool m_Pending;
        private bool m_Broken;
        private float m_Value;
        private float m_ValueTime = float.NegativeInfinity;

        internal ExposureProbe()
        {
            m_OnReadback = OnReadback;
            if (s_GetExposureTexture == null)
            {
                Logger.Warn("HDRP exposure texture not found; using the time-of-day brightness estimate instead.");
                m_Broken = true;
            }
        }

        /// <summary>Live exposure multiplier, if a recent valid reading exists.</summary>
        internal bool TryGet(out float exposureMultiplier)
        {
            exposureMultiplier = m_Value;
            return !m_Broken && Time.unscaledTime - m_ValueTime <= MaxAgeSeconds;
        }

        /// <summary>Starts a new readback when the previous one has finished. Call once per frame.</summary>
        internal void Request(Camera camera)
        {
            if (m_Broken || m_Pending || camera == null || !SystemInfo.supportsAsyncGPUReadback)
            {
                return;
            }

            try
            {
                if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline pipeline))
                {
                    return;
                }

                HDCamera hdCamera = HDCamera.GetOrCreate(camera, 0);
                Texture texture = (s_GetExposureTexture.Invoke(pipeline, new object[] { hdCamera }) as RTHandle)?.rt;
                if (texture == null)
                {
                    return;
                }

                m_Pending = true;
                AsyncGPUReadback.Request(texture, 0, 0, 1, 0, 1, 0, 1, TextureFormat.RGFloat, m_OnReadback);
            }
            catch (Exception exception)
            {
                m_Pending = false;
                m_Broken = true;
                Logger.Warn($"Reading HDRP exposure failed ({exception.GetType().Name}: {exception.Message}); " +
                            "using the time-of-day brightness estimate instead.");
            }
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            m_Pending = false;
            if (request.hasError || request.layerCount < 1)
            {
                return;
            }

            float value = request.GetData<float>()[0];
            if (!float.IsNaN(value) && !float.IsInfinity(value) && value > 0f)
            {
                m_Value = value;
                m_ValueTime = Time.unscaledTime;
            }
        }
    }
}
