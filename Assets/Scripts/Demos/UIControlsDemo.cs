using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KXGallery
{
    /// <summary>uGUI controls built in code.</summary>
    public class UIControlsDemo : Demo
    {
        Text status;

        public override void Build()
        {
            var bg = UIFactory.Panel(Overlay, UIFactory.Background, "Background", false);
            UIFactory.Stretch(bg.rectTransform);
            var page = UIFactory.ScrollPage(Overlay, 0, Gallery.BottomInset);

            var s = UIFactory.Section(page, "Status");
            status = UIFactory.Label(s, "Interact with any control.", 15, UIFactory.Secondary);

            var buttons = UIFactory.Section(page, "Buttons");
            var row = UIFactory.Row(buttons, 40);
            int taps = 0;
            UIFactory.Button(row, "Tinted", () => Say($"Tinted tapped ({++taps})"));
            UIFactory.Button(row, "Filled", () => Say($"Filled tapped ({++taps})"), true);
            var disabled = UIFactory.Button(row, "Disabled", () => { });
            disabled.interactable = false;

            var toggles = UIFactory.Section(page, "Toggles");
            UIFactory.Switch(toggles, "Notifications", true, on => Say("Notifications " + (on ? "on" : "off")));
            UIFactory.Switch(toggles, "Dark mode", false, on => Say("Dark mode " + (on ? "on" : "off")));
            var group = toggles.gameObject.AddComponent<ToggleGroup>();
            var radios = UIFactory.Row(toggles, 36, 16);
            foreach (var size in new[] { "Small", "Medium", "Large" })
                Radio(radios, size, size == "Medium", group);

            var sliders = UIFactory.Section(page, "Slider and progress");
            var value = UIFactory.Label(sliders, "Volume 40%", 15);
            var progressTrack = UIFactory.Panel(sliders, UIFactory.Separator, "Progress");
            progressTrack.gameObject.AddComponent<LayoutElement>().minHeight = 8;
            var fill = UIFactory.Panel(progressTrack.transform, new Color32(52, 199, 89, 255), "Fill");
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.4f, 1);
            fill.rectTransform.sizeDelta = Vector2.zero;
            UIFactory.Slider(sliders, 0, 1, 0.4f, v =>
            {
                value.text = $"Volume {Mathf.RoundToInt(v * 100)}%";
                fill.rectTransform.anchorMax = new Vector2(v, 1);
            });

            var input = UIFactory.Section(page, "InputField", "Tap to open the keyboard.");
            var fieldBg = UIFactory.Panel(input, UIFactory.Background, "InputField");
            fieldBg.gameObject.AddComponent<LayoutElement>().minHeight = 44;
            var placeholder = UIFactory.Label(fieldBg.transform, "Type your name", 16, UIFactory.Secondary);
            UIFactory.Stretch(placeholder.rectTransform, 12, 0, 12, 0);
            var text = UIFactory.Label(fieldBg.transform, "", 16);
            UIFactory.Stretch(text.rectTransform, 12, 0, 12, 0);
            text.raycastTarget = true;
            var field = fieldBg.gameObject.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.onValueChanged.AddListener(v => Say(string.IsNullOrEmpty(v) ? "Type your name above." : $"Hello, {v}!"));

            var dropdowns = UIFactory.Section(page, "Dropdown");
            BuildDropdown(dropdowns, new List<string> { "Apple", "Banana", "Cherry", "Mango" });

            var list = UIFactory.Section(page, "ScrollRect inside a page", "A horizontal list of cards.");
            var strip = UIFactory.Rect("Strip", list);
            strip.gameObject.AddComponent<LayoutElement>().minHeight = 110;
            strip.gameObject.AddComponent<RectMask2D>();
            var stripContent = UIFactory.Rect("Content", strip);
            stripContent.anchorMin = new Vector2(0, 0);
            stripContent.anchorMax = new Vector2(0, 1);
            stripContent.pivot = new Vector2(0, 0.5f);
            var hl = stripContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            stripContent.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < 10; i++)
            {
                var c = UIFactory.Panel(stripContent, Color.HSVToRGB(i / 10f, 0.55f, 0.95f), "Card");
                c.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
                var l = UIFactory.Label(c.transform, (i + 1).ToString(), 26, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIFactory.Stretch(l.rectTransform);
            }
            var stripScroll = strip.gameObject.AddComponent<ScrollRect>();
            stripScroll.content = stripContent;
            stripScroll.vertical = false;
            strip.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        }

        void Radio(Transform parent, string label, bool on, ToggleGroup group)
        {
            var row = UIFactory.Row(parent, 30, 6);
            var ring = UIFactory.Rect("Ring", row);
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = UIFactory.Circle;
            ringImg.color = UIFactory.Separator;
            var le = ring.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 22;
            var dot = UIFactory.Rect("Dot", ring);
            var dotImg = dot.gameObject.AddComponent<Image>();
            dotImg.sprite = UIFactory.Circle;
            dotImg.color = UIFactory.Accent;
            dot.sizeDelta = new Vector2(14, 14);
            UIFactory.Label(row, label, 16);
            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.group = group;
            toggle.graphic = dotImg;
            toggle.targetGraphic = ringImg;
            toggle.isOn = on;
            toggle.onValueChanged.AddListener(v => { if (v) Say("Size: " + label); });
        }

        void BuildDropdown(Transform parent, List<string> options)
        {
            var bg = UIFactory.Panel(parent, UIFactory.Background, "Dropdown");
            bg.gameObject.AddComponent<LayoutElement>().minHeight = 44;
            var caption = UIFactory.Label(bg.transform, options[0], 16);
            UIFactory.Stretch(caption.rectTransform, 12, 0, 36, 0);
            var arrow = UIFactory.Label(bg.transform, "▾", 18, UIFactory.Secondary, FontStyle.Normal, TextAnchor.MiddleRight);
            UIFactory.Stretch(arrow.rectTransform, 0, 0, 12, 0);

            // Template: a list that opens under the field.
            var template = UIFactory.Panel(bg.transform, Color.white, "Template");
            var trt = template.rectTransform;
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 0);
            trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, options.Count * 40 + 8);
            var shadow = template.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.2f);
            shadow.effectDistance = new Vector2(0, -3);
            var viewport = UIFactory.Rect("Viewport", template.transform);
            UIFactory.Stretch(viewport, 0, 4, 0, 4);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UIFactory.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 40);
            var item = UIFactory.Rect("Item", content);
            item.anchorMin = new Vector2(0, 0.5f);
            item.anchorMax = new Vector2(1, 0.5f);
            item.sizeDelta = new Vector2(0, 40);
            var itemBg = item.gameObject.AddComponent<Image>();
            itemBg.color = new Color(0, 0, 0, 0);
            var check = UIFactory.Label(item, "✓", 16, UIFactory.Accent);
            UIFactory.Stretch(check.rectTransform, 12, 0, 0, 0);
            var itemLabel = UIFactory.Label(item, "Option", 16);
            UIFactory.Stretch(itemLabel.rectTransform, 36, 0, 12, 0);
            var itemToggle = item.gameObject.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBg;
            itemToggle.graphic = check;
            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            template.gameObject.SetActive(false);

            var dropdown = bg.gameObject.AddComponent<Dropdown>();
            dropdown.template = trt;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.AddOptions(options);
            dropdown.onValueChanged.AddListener(i => Say("Picked " + options[i]));
        }

        void Say(string text) => status.text = text;
    }
}
