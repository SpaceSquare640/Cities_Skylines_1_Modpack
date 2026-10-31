using System;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace AIGaming
{
    internal sealed class SectionNavigation
    {
        private static readonly string[] Keys = { "ui.ask", "ui.data", "ui.review" };
        private readonly UIPanel[] _pages = new UIPanel[3];
        private readonly UIButton[] _buttons = new UIButton[3];
        private readonly UIComponent _root;
        private readonly UIPanel _navigation;
        private int _selected;

        internal SectionNavigation(UIHelperBase helper)
        {
            UIHelper native = helper as UIHelper;
            UIComponent root = native == null ? null : native.self as UIComponent;
            if (root == null) throw new InvalidOperationException("AI Gaming UI root is unavailable.");
            _root = root;
            float width = root.width > 200f ? root.width - 16f : 620f;
            UIPanel navigation = root.AddUIComponent<UIPanel>();
            _navigation = navigation;
            navigation.width = width;
            navigation.height = 42f;
            navigation.autoLayout = false;
            float buttonWidth = (width - 12f) / 3f;
            for (int i = 0; i < _pages.Length; i++)
            {
                int selected = i;
                UIButton button = navigation.AddUIComponent<UIButton>();
                button.width = buttonWidth;
                button.height = 38f;
                button.relativePosition = new Vector3(i * (buttonWidth + 6f), 0f);
                button.normalBgSprite = "ButtonMenu";
                button.hoveredBgSprite = "ButtonMenuHovered";
                button.pressedBgSprite = "ButtonMenuPressed";
                button.wordWrap = true;
                button.eventClick += delegate { Select(selected); };
                _buttons[i] = button;

                UIPanel page = root.AddUIComponent<UIPanel>();
                page.width = width;
                page.autoLayout = true;
                page.autoLayoutDirection = LayoutDirection.Vertical;
                page.autoLayoutPadding = new RectOffset(0, 0, 0, 6);
                page.autoFitChildrenVertically = true;
                _pages[i] = page;
            }
            RefreshLanguage();
            Select(0);
            root.eventSizeChanged += OnRootSizeChanged;
        }

        internal UIHelperBase Ask { get { return new UIHelper(_pages[0]); } }
        internal UIHelperBase Data { get { return new UIHelper(_pages[1]); } }
        internal UIHelperBase Review { get { return new UIHelper(_pages[2]); } }
        internal bool IsAlive { get { return _pages[0] != null && _buttons[0] != null; } }

        internal void ShowReview() { Select(2); }

        internal void Dispose()
        {
            if (_root != null) _root.eventSizeChanged -= OnRootSizeChanged;
        }

        private void OnRootSizeChanged(UIComponent component, Vector2 size) { UpdateWidth(); }

        internal void RefreshLanguage()
        {
            for (int i = 0; i < _buttons.Length; i++)
                if (_buttons[i] != null)
                {
                    _buttons[i].text = Localization.Get(Keys[i]);
                    _buttons[i].tooltip = Localization.Get(Keys[i]);
                }
        }

        private void UpdateWidth()
        {
            if (_root == null || _navigation == null || _root.width <= 32f) return;
            float width = _root.width - 16f;
            float buttonWidth = Math.Max(1f, (width - 12f) / 3f);
            _navigation.width = width;
            for (int i = 0; i < _pages.Length; i++)
            {
                if (_pages[i] != null) _pages[i].width = width;
                if (_buttons[i] == null) continue;
                _buttons[i].width = buttonWidth;
                _buttons[i].relativePosition = new Vector3(i * (buttonWidth + 6f), 0f);
            }
        }

        private void Select(int selected)
        {
            _selected = selected;
            for (int i = 0; i < _pages.Length; i++)
            {
                if (_pages[i] != null) _pages[i].isVisible = i == _selected;
                if (_buttons[i] != null)
                {
                    _buttons[i].textColor = i == _selected ? Color.white : new Color32(190, 203, 208, 255);
                    _buttons[i].tooltip = Localization.Get(Keys[i]);
                }
            }
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
