using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KXGallery
{
    /// <summary>Builds uGUI elements in code: rounded panels, labels, buttons, sliders and toggles.</summary>
    public static class UIFactory
    {
        public static readonly Color Background = new Color32(242, 242, 247, 255);
        public static readonly Color Card = Color.white;
        public static readonly Color Text = new Color32(28, 28, 30, 255);
        public static readonly Color Secondary = new Color32(108, 108, 112, 255);
        public static readonly Color Accent = new Color32(10, 132, 255, 255);
        public static readonly Color Separator = new Color32(209, 209, 214, 255);

        static Font font;
        static Sprite rounded;
        static Sprite circle;

        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>A white 9-sliced rounded rectangle, tinted by Image.color.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64, radius = 24;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - 0.5f, 0, x + 0.5f - (size - radius));
                    float dy = Mathf.Max(radius - y - 0.5f, 0, y + 0.5f - (size - radius));
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return rounded;
            }
        }

        /// <summary>A white anti-aliased disc.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) - (size / 2f - 1);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255));
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                return circle;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel", bool roundedCorners = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (roundedCorners)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2.4f;
            }
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 17, Color? color = null, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color ?? Text;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string text, UnityAction onClick, bool filled = false)
        {
            var img = Panel(parent, filled ? Accent : new Color(Accent.r, Accent.g, Accent.b, 0.14f), "Button");
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            var label = Label(img.transform, text, 16, filled ? Color.white : Accent, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 40;
            le.preferredWidth = Mathf.Max(80, text.Length * 10 + 28);
            return button;
        }

        public static Slider Slider(Transform parent, float min, float max, float value, UnityAction<float> onChange)
        {
            var root = Rect("Slider", parent);
            root.gameObject.AddComponent<LayoutElement>().minHeight = 36;
            var track = Panel(root, Separator, "Track");
            track.rectTransform.anchorMin = new Vector2(0, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(-24, 6);
            var fillArea = Rect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0, 0.5f);
            fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(-24, 6);
            var fill = Panel(fillArea, Accent, "Fill");
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect("Handle Area", root);
            Stretch(handleArea, 12, 0, 12, 0);
            var handle = Rect("Handle", handleArea);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = Circle;
            handle.sizeDelta = new Vector2(28, 28);
            var shadow = handle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.25f);
            shadow.effectDistance = new Vector2(0, -2);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle;
            slider.targetGraphic = hImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        public static Toggle Switch(Transform parent, string text, bool on, UnityAction<bool> onChange)
        {
            var row = Row(parent, 40);
            var label = Label(row, text);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var bg = Panel(row, on ? new Color32(52, 199, 89, 255) : Separator, "Switch");
            var le = bg.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 51;
            le.preferredHeight = 31;
            bg.sprite = Rounded;
            bg.pixelsPerUnitMultiplier = 1.5f;
            var knob = Rect("Knob", bg.transform);
            var kImg = knob.gameObject.AddComponent<Image>();
            kImg.sprite = Circle;
            knob.anchorMin = knob.anchorMax = new Vector2(on ? 1 : 0, 0.5f);
            knob.anchoredPosition = new Vector2(on ? -15.5f : 15.5f, 0);
            knob.sizeDelta = new Vector2(27, 27);
            var toggle = bg.gameObject.AddComponent<Toggle>();
            toggle.isOn = on;
            toggle.targetGraphic = bg;
            toggle.onValueChanged.AddListener(value =>
            {
                bg.color = value ? new Color32(52, 199, 89, 255) : Separator;
                knob.anchorMin = knob.anchorMax = new Vector2(value ? 1 : 0, 0.5f);
                knob.anchoredPosition = new Vector2(value ? -15.5f : 15.5f, 0);
                onChange?.Invoke(value);
            });
            return toggle;
        }

        public static RectTransform Row(Transform parent, float height, float spacing = 10)
        {
            var rt = Rect("Row", parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            rt.gameObject.AddComponent<LayoutElement>().minHeight = height;
            return rt;
        }

        /// <summary>A scrolling vertical page; returns the content transform to add sections to.</summary>
        public static RectTransform ScrollPage(Transform parent, float top, float bottom)
        {
            var viewport = Rect("Scroll", parent);
            Stretch(viewport, 0, top, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero; // same width as the viewport
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 16, (int)bottom + 24);
            v.spacing = 18;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30;
            // A transparent graphic so drags anywhere in the page scroll it.
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0);
            return content;
        }

        /// <summary>A titled white card inside a ScrollPage; returns the card's vertical layout.</summary>
        public static RectTransform Section(Transform content, string title, string note = null)
        {
            var wrap = Rect("Section", content);
            var v = wrap.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            var heading = Label(wrap, title.ToUpperInvariant(), 13, Secondary);
            heading.gameObject.AddComponent<LayoutElement>().minHeight = 18;
            var card = Panel(wrap, Card, "Card");
            var cv = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cv.padding = new RectOffset(16, 16, 14, 14);
            cv.spacing = 10;
            cv.childControlWidth = true;
            cv.childControlHeight = true;
            cv.childForceExpandHeight = false;
            if (!string.IsNullOrEmpty(note))
            {
                var n = Label(wrap, note, 13, Secondary);
                n.gameObject.AddComponent<LayoutElement>().minHeight = 18;
            }
            return card.rectTransform;
        }
    }
}
