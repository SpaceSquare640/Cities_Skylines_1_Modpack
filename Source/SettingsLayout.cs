using System;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace AIGaming
{
    internal static class SettingsLayout
    {
        private static UIComponent Root(UIHelperBase group)
        {
            UIHelper helper = group as UIHelper;
            UIComponent root = helper == null ? null : helper.self as UIComponent;
            UIPanel panel = root as UIPanel;
            if (panel != null) panel.autoFitChildrenVertically = true;
            return root;
        }

        internal static UILabel AddInfoLabel(UIHelperBase group, string text)
        {
            UIComponent root = Root(group);
            if (root == null) return null;
            UILabel label = root.AddUIComponent<UILabel>();
            label.autoSize = false;
            label.wordWrap = true;
            label.width = Math.Max(120f, root.width - 32f);
            label.autoHeight = true;
            label.textScale = 0.9f;
            label.text = text ?? string.Empty;
            return label;
        }

        internal static void SetInfoLabel(UIHelperBase group, UILabel label, string text)
        {
            if (label == null) return;
            label.text = text ?? string.Empty;
            UIComponent root = Root(group);
            if (root != null) ExpandRow(root, label);
        }

        internal static void FitTextField(UIHelperBase group, UITextField field)
        {
            UIComponent root = Root(group);
            if (root == null || field == null) return;
            float available = root.width - (field.absolutePosition.x - root.absolutePosition.x) - 24f;
            if (available > field.width) field.width = available;
            ExpandRow(root, field);
        }

        internal static void FitButton(UIHelperBase group, UIButton button)
        {
            UIComponent root = Root(group);
            if (root == null || button == null) return;
            button.autoSize = false;
            button.wordWrap = true;
            if (root.width > 40f) button.width = root.width - 24f;
            string text = button.text ?? string.Empty;
            int columns = Math.Max(12, (int)(button.width / 20f));
            int units = 0;
            int lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') { lines++; units = 0; continue; }
                units += text[i] > 255 ? 2 : 1;
                if (units >= columns) { lines++; units = 0; }
            }
            button.height = Math.Max(36f, lines * 24f + 12f);
            ExpandRow(root, button);
        }

        internal static void FitCheckbox(UIHelperBase group, UICheckBox checkbox)
        {
            UIComponent root = Root(group);
            if (root == null || checkbox == null || checkbox.label == null) return;
            UILabel label = checkbox.label;
            label.autoSize = false;
            label.wordWrap = true;
            if (root.width > 60f) label.width = root.width - 48f;
            label.autoHeight = true;
            label.eventSizeChanged += delegate(UIComponent component, Vector2 size) { ExpandRow(root, component); };
            ExpandRow(root, label);
        }

        internal static void SetTextFieldLabel(UIHelperBase group, UITextField field, string text)
        {
            if (field == null || field.parent == null) return;

            UILabel label = field.parent.Find("Label", typeof(UILabel)) as UILabel;
            if (label == null) return;

            label.text = text ?? string.Empty;
            label.autoSize = false;
            label.wordWrap = true;
            label.autoHeight = true;

            UIComponent root = Root(group);
            if (root == null) return;
            if (root.width > 60f) label.width = root.width - 48f;
            ExpandRow(root, label);
        }

        internal static void SetControlLabel(UIComponent control, string text)
        {
            if (control == null || control.parent == null) return;
            UILabel label = control.parent.Find("Label", typeof(UILabel)) as UILabel;
            if (label != null) label.text = text ?? string.Empty;
        }

        private static void ExpandRow(UIComponent root, UIComponent child)
        {
            while (child != null && child.parent != null && child.parent != root)
            {
                UIComponent parent = child.parent;
                float width = child.relativePosition.x + child.width + 8f;
                float height = child.relativePosition.y + child.height + 8f;
                if (parent.width < width) parent.width = width;
                if (parent.height < height) parent.height = height;
                child = parent;
            }
        }
    }
}
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Implementation update signed by OpenAI-Codex.
// Integration verification: OpenAI-Codex_Responsible: AI_Gaming.
