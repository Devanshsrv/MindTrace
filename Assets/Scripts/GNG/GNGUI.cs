using UnityEngine;
using UnityEngine.UI;

namespace GNG
{
    public static class GNGUI
    {
        // MindTrace theme
        public static readonly Color BG = new Color(0f, 0f, 0f);
        public static readonly Color CARD = new Color(0.043f, 0.055f, 0.078f);
        public static readonly Color CARD_BORDER = new Color(0f, 0.898f, 1f, 0.35f);
        public static readonly Color BORDER = new Color(0f, 0.898f, 1f, 0.20f);
        public static readonly Color CYAN = new Color(0f, 0.898f, 1f);
        public static readonly Color TEXT_PRIMARY = new Color(1f, 1f, 1f);
        public static readonly Color TEXT_SECONDARY = new Color(0.72f, 0.76f, 0.80f);
        public static readonly Color TEXT_TERTIARY = new Color(0.50f, 0.55f, 0.60f);
        public static readonly Color INPUT_BG = new Color(0.18f, 0.20f, 0.24f);
        public static readonly Color BTN_PRIMARY = new Color(0.118f, 0.247f, 0.749f);

        public static readonly Color GREEN = new Color(0.114f, 0.620f, 0.459f);
        public static readonly Color RED = new Color(0.886f, 0.294f, 0.290f);
        public static readonly Color GREEN_BG = new Color(0.067f, 0.227f, 0.157f);
        public static readonly Color GREEN_FG = new Color(0.31f, 0.85f, 0.55f);
        public static readonly Color AMBER_BG = new Color(0.286f, 0.196f, 0.05f);
        public static readonly Color AMBER_FG = new Color(1f, 0.733f, 0.314f);
        public static readonly Color RED_BG = new Color(0.302f, 0.106f, 0.118f);
        public static readonly Color RED_FG = new Color(1f, 0.231f, 0.235f);

        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return _font;
            }
        }

        public static Color RiskColor(string risk, bool fg)
        {
            if (risk == "High") return fg ? RED_FG : RED_BG;
            if (risk == "Moderate") return fg ? AMBER_FG : AMBER_BG;
            return fg ? GREEN_FG : GREEN_BG;
        }

        public static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Image MakePanel(string name, Transform parent, Color color)
        {
            var go = MakeRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image MakeCard(string name, Transform parent)
        {
            var img = MakePanel(name, parent, CARD);
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = CARD_BORDER;
            outline.effectDistance = new Vector2(1, -1);
            return img;
        }

        public static Text MakeText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = MakeRect(name, parent);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button MakeButton(string name, Transform parent, string label, int fontSize, bool primary, System.Action onClick)
        {
            var go = MakeRect(name, parent);
            var img = go.AddComponent<Image>();
            if (primary)
            {
                img.color = BTN_PRIMARY;
            }
            else
            {
                img.color = new Color(0.10f, 0.12f, 0.16f);
                var ol = go.AddComponent<Outline>();
                ol.effectColor = BORDER;
                ol.effectDistance = new Vector2(1, -1);
            }
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = primary ? new Color(0.85f, 0.90f, 1f) : new Color(0.85f, 0.95f, 1f);
            colors.pressedColor = primary ? new Color(0.65f, 0.72f, 0.95f) : new Color(0.65f, 0.85f, 0.95f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            btn.colors = colors;

            var lbl = MakeText("Label", go.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var lr = lbl.rectTransform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static InputField MakeInputField(string name, Transform parent, string placeholder, bool numeric)
        {
            var go = MakeRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = INPUT_BG;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = BORDER;
            ol.effectDistance = new Vector2(1, -1);

            var textGo = MakeRect("Text", go.transform);
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(24, 8); textRT.offsetMax = new Vector2(-24, -8);
            var text = textGo.AddComponent<Text>();
            text.font = Font;
            text.fontSize = 44;
            text.color = TEXT_PRIMARY;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;

            var phGo = MakeRect("Placeholder", go.transform);
            var phRT = phGo.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(24, 8); phRT.offsetMax = new Vector2(-24, -8);
            var ph = phGo.AddComponent<Text>();
            ph.font = Font;
            ph.fontSize = 44;
            ph.color = TEXT_TERTIARY;
            ph.alignment = TextAnchor.MiddleCenter;
            ph.text = placeholder;

            var input = go.AddComponent<InputField>();
            input.targetGraphic = img;
            input.textComponent = text;
            input.placeholder = ph;
            input.contentType = numeric ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = numeric ? 4 : 32;
            return input;
        }

        public static GameObject MakeMetricRow(Transform parent, string label, string value)
        {
            var row = MakeRect("Row_" + label, parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

            var lab = MakeText("L", row.transform, label + ":", 28, TEXT_SECONDARY, TextAnchor.MiddleLeft);
            var labLE = lab.gameObject.AddComponent<LayoutElement>();
            labLE.preferredWidth = 320;

            var val = MakeText("V", row.transform, value, 28, TEXT_PRIMARY, TextAnchor.MiddleLeft, FontStyle.Bold);
            var valLE = val.gameObject.AddComponent<LayoutElement>();
            valLE.preferredWidth = 200;

            return row;
        }

        public static GameObject MakeRiskCard(Transform parent, string label, string risk, int score, int max)
        {
            var card = MakeRect("Risk_" + label, parent);
            var img = card.AddComponent<Image>();
            img.color = RiskColor(risk, false);
            var ol = card.AddComponent<Outline>();
            ol.effectColor = new Color(RiskColor(risk, true).r, RiskColor(risk, true).g, RiskColor(risk, true).b, 0.5f);
            ol.effectDistance = new Vector2(1, -1);

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 18, 18);
            vlg.spacing = 4;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var fg = RiskColor(risk, true);

            var lab = MakeText("Label", card.transform, label.ToUpper(), 22, fg, TextAnchor.MiddleLeft);
            var labLE = lab.gameObject.AddComponent<LayoutElement>();
            labLE.preferredHeight = 30;

            var val = MakeText("Risk", card.transform, risk + " RISK", 38, fg, TextAnchor.MiddleLeft, FontStyle.Bold);
            var valLE = val.gameObject.AddComponent<LayoutElement>();
            valLE.preferredHeight = 50;

            var scoreT = MakeText("Score", card.transform, "score " + score + "/" + max, 22, fg, TextAnchor.MiddleLeft);
            var scoreLE = scoreT.gameObject.AddComponent<LayoutElement>();
            scoreLE.preferredHeight = 28;

            return card;
        }
    }
}
