using System;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace AIGaming
{
    internal static class InGameUiHost
    {
        internal static Action OpenRequested;
        internal static Action CityUnloaded;

        private static UIButton _entry;
        private static UIPanel _panel;
        private static UIButton _close;

        internal static void OnCityLoaded()
        {
            if (_entry != null) return;
            UIView view = UIView.GetAView();
            if (view == null) return;

            _entry = view.AddUIComponent(typeof(UIButton)) as UIButton;
            if (_entry == null) return;
            _entry.name = "AIGamingEntry";
            _entry.text = "AI Gaming";
            _entry.tooltip = "AI Gaming";
            _entry.width = 120f;
            _entry.height = 36f;
            _entry.normalBgSprite = "ButtonMenu";
            _entry.hoveredBgSprite = "ButtonMenuHovered";
            _entry.pressedBgSprite = "ButtonMenuPressed";
            _entry.relativePosition = new Vector3(20f, 125f);
            _entry.eventClick += delegate
            {
                Action open = OpenRequested;
                if (open != null) open();
            };
        }

        internal static void Open(Action<UIHelperBase> buildContent)
        {
            if (buildContent == null) throw new ArgumentNullException("buildContent");
            if (_panel == null) CreatePanel(buildContent);
            if (_panel != null) _panel.isVisible = true;
        }

        internal static void Close()
        {
            if (_panel != null) _panel.isVisible = false;
        }

        internal static void RefreshLanguage()
        {
            if (_close != null) _close.tooltip = Localization.Get("ui.close");
        }

        internal static void Destroy()
        {
            if (_panel != null) UnityEngine.Object.Destroy(_panel.gameObject);
            if (_entry != null) UnityEngine.Object.Destroy(_entry.gameObject);
            _panel = null;
            _entry = null;
            _close = null;
        }

        internal static void OnCityUnloading()
        {
            try
            {
                Action unloaded = CityUnloaded;
                if (unloaded != null) unloaded();
            }
            finally { Destroy(); }
        }

        private static void CreatePanel(Action<UIHelperBase> buildContent)
        {
            UIView view = UIView.GetAView();
            if (view == null) return;

            UIPanel panel = view.AddUIComponent(typeof(UIPanel)) as UIPanel;
            if (panel == null) return;
            _panel = panel;
            try
            {
                Vector2 resolution = view.GetScreenResolution();
                float screenWidth = resolution.x > 0f ? resolution.x : 1024f;
                float screenHeight = resolution.y > 0f ? resolution.y : 768f;
                panel.name = "AIGamingPanel";
                panel.backgroundSprite = "MenuPanel2";
                panel.width = Math.Max(240f, Math.Min(720f, screenWidth - 40f));
                panel.height = Math.Max(200f, Math.Min(650f, screenHeight - 80f));
                panel.relativePosition = new Vector3(20f, 60f);
                panel.canFocus = true;
                panel.isInteractive = true;
                panel.isVisible = false;

                UIDragHandle drag = panel.AddUIComponent<UIDragHandle>();
                drag.width = panel.width;
                drag.height = 40f;
                drag.relativePosition = Vector3.zero;
                drag.target = panel;

                UILabel title = panel.AddUIComponent<UILabel>();
                title.text = "AI Gaming";
                title.relativePosition = new Vector3(12f, 12f);

                UIButton close = panel.AddUIComponent<UIButton>();
                close.text = "X";
                close.tooltip = Localization.Get("ui.close");
                close.width = 30f;
                close.height = 30f;
                close.relativePosition = new Vector3(panel.width - 40f, 5f);
                close.normalBgSprite = "buttonclose";
                close.hoveredBgSprite = "buttonclosehover";
                close.pressedBgSprite = "buttonclosepressed";
                close.eventClick += delegate { Close(); };
                _close = close;

                UIScrollablePanel content = panel.AddUIComponent<UIScrollablePanel>();
                content.relativePosition = new Vector3(10f, 44f);
                content.width = panel.width - 34f;
                content.height = panel.height - 54f;
                content.autoLayout = true;
                content.autoLayoutDirection = LayoutDirection.Vertical;
                content.autoLayoutPadding = new RectOffset(0, 0, 0, 6);
                content.clipChildren = true;
                content.scrollWheelDirection = UIOrientation.Vertical;
                content.scrollWheelAmount = 24;
                content.builtinKeyNavigation = true;

                UIScrollbar scrollbar = panel.AddUIComponent<UIScrollbar>();
                scrollbar.width = 10f;
                scrollbar.height = content.height;
                scrollbar.orientation = UIOrientation.Vertical;
                scrollbar.pivot = UIPivotPoint.TopLeft;
                scrollbar.relativePosition = new Vector3(panel.width - 18f, 44f);
                scrollbar.minValue = 0f;
                scrollbar.value = 0f;
                scrollbar.incrementAmount = 24f;
                scrollbar.autoHide = true;

                UIPanel track = scrollbar.AddUIComponent<UIPanel>();
                track.backgroundSprite = "GenericPanel";
                track.color = new Color32(50, 60, 65, 180);
                track.width = scrollbar.width;
                track.height = scrollbar.height;
                scrollbar.trackObject = track;

                UIPanel thumb = track.AddUIComponent<UIPanel>();
                thumb.backgroundSprite = "GenericPanel";
                thumb.color = new Color32(160, 180, 185, 220);
                thumb.width = scrollbar.width;
                scrollbar.thumbObject = thumb;
                content.verticalScrollbar = scrollbar;

                buildContent(new UIHelper(content));
                var bounds = panel.gameObject.AddComponent<AIGamingPanelBounds>();
                bounds.Initialize(view, panel, drag, close, content, scrollbar, track);
            }
            catch
            {
                UnityEngine.Object.Destroy(panel.gameObject);
                _panel = null;
                _close = null;
                throw;
            }
        }
    }

    internal sealed class AIGamingPanelBounds : MonoBehaviour
    {
        private UIView _view;
        private UIPanel _panel;
        private UIDragHandle _drag;
        private UIButton _close;
        private UIScrollablePanel _content;
        private UIScrollbar _scrollbar;
        private UIPanel _track;
        private float _lastWidth = -1f;
        private float _lastHeight = -1f;

        internal void Initialize(UIView view, UIPanel panel, UIDragHandle drag, UIButton close,
            UIScrollablePanel content, UIScrollbar scrollbar, UIPanel track)
        {
            _view = view;
            _panel = panel;
            _drag = drag;
            _close = close;
            _content = content;
            _scrollbar = scrollbar;
            _track = track;
            KeepVisible();
        }

        private void LateUpdate()
        {
            KeepVisible();
        }

        private void KeepVisible()
        {
            if (_view == null || _panel == null) return;
            Vector2 resolution = _view.GetScreenResolution();
            if (resolution.x <= 0f || resolution.y <= 0f) return;
            float screenWidth = resolution.x;
            float screenHeight = resolution.y;
            if (_lastWidth != screenWidth || _lastHeight != screenHeight)
            {
                _lastWidth = screenWidth;
                _lastHeight = screenHeight;
                _panel.width = Math.Max(1f, Math.Min(720f, screenWidth - 20f));
                _panel.height = Math.Max(1f, Math.Min(650f, screenHeight - 20f));
                if (_drag != null) _drag.width = _panel.width;
                if (_close != null) _close.relativePosition = new Vector3(Math.Max(0f, _panel.width - 40f), 5f);
                if (_content != null)
                {
                    _content.width = Math.Max(1f, _panel.width - 34f);
                    _content.height = Math.Max(1f, _panel.height - 54f);
                }
                if (_scrollbar != null)
                {
                    _scrollbar.height = Math.Max(1f, _panel.height - 54f);
                    _scrollbar.relativePosition = new Vector3(Math.Max(0f, _panel.width - 18f), 44f);
                }
                if (_track != null) _track.height = Math.Max(1f, _panel.height - 54f);
            }

            Vector3 position = _panel.relativePosition;
            float x = Mathf.Clamp(position.x, 0f, Math.Max(0f, screenWidth - _panel.width));
            float y = Mathf.Clamp(position.y, 0f, Math.Max(0f, screenHeight - _panel.height));
            if (x != position.x || y != position.y)
                _panel.relativePosition = new Vector3(x, y, position.z);
        }
    }

    public sealed class AIGamingUiLoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario)
                InGameUiHost.OnCityLoaded();
        }

        public override void OnLevelUnloading()
        {
            InGameUiHost.OnCityUnloading();
            base.OnLevelUnloading();
        }
    }
}
// Implementation update signed by OpenAI-Codex_ingame_host.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
