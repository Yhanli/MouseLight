using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace BetterCursorLight
{
    public class Mod : IMod
    {
        // 运行时设置由更新系统共享，并通过游戏设置接口持久化。
        internal static CursorLightSettings Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Logger.Info("Better Cursor Light loading.");

            // 在游戏系统开始更新前注册并加载持久化设置。
            Settings = new CursorLightSettings(this);
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(BetterCursorLight), Settings, new CursorLightSettings(this));
            // 注册英语、简体中文和德语；英语作为默认文本基准。
            GameManager.instance.localizationManager.AddSource("en-US", CursorLightLocale.CreateEnglish(Settings));
            GameManager.instance.localizationManager.AddSource("zh-HANS", CursorLightLocale.CreateChinese(Settings));
            GameManager.instance.localizationManager.AddSource("de-DE", CursorLightLocale.CreateGerman(Settings));

            // RaycastSystem 会先执行 PreTool 阶段，再统一处理各系统提交的射线。
            updateSystem.UpdateAt<CursorGlowSystem>(SystemUpdatePhase.PreTool);

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                Logger.Info($"Better Cursor Light loaded from {asset.path}");
            }
        }

        public void OnDispose()
        {
            // 模组卸载时立即销毁 Unity 光源并注销设置页面。
            CursorLightController.DisposeShared();
            Settings?.UnregisterInOptionsUI();
            Settings = null;
            Logger.Info("Better Cursor Light disposed.");
        }
    }
}
