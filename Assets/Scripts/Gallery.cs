using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KXGallery
{
    /// <summary>Base class of every demo. A demo builds its own objects in Build() and is destroyed when the user goes back.</summary>
    public abstract class Demo : MonoBehaviour
    {
        /// <summary>The full-screen UI area below the navigation bar.</summary>
        public RectTransform Overlay { get; set; }
        public Gallery Gallery { get; set; }

        public abstract void Build();

        /// <summary>A camera for 3D demos; it is a child of the demo, so it goes away with it.</summary>
        protected Camera MakeCamera(Vector3 position, Vector3 lookAt, Color background)
        {
            var go = new GameObject("Demo Camera");
            go.transform.SetParent(transform, false);
            var cam = go.AddComponent<Camera>();
            cam.depth = 1;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.transform.position = position;
            go.transform.LookAt(lookAt);
            return cam;
        }

        /// <summary>True when the touch / click at this screen position is not on the navigation bar or a UI control.</summary>
        protected bool IsFreeTouch(Vector2 screenPosition, int pointerId)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId)) return false;
            return screenPosition.y < Screen.height - Gallery.HeaderHeightPixels;
        }
    }

    /// <summary>Entry point: builds the menu and hosts one demo at a time. Nothing is placed in the scene by hand.</summary>
    public class Gallery : MonoBehaviour
    {
        struct Entry
        {
            public string Id, Title, Summary;
            public Color Color;
            public Type Type;
            public Entry(string id, string title, string summary, string color, Type type)
            {
                Id = id; Title = title; Summary = summary; Type = type;
                ColorUtility.TryParseHtmlString(color, out Color);
            }
        }

        static readonly Entry[] Entries =
        {
            new Entry("ui", "UI Controls", "uGUI buttons, toggles, sliders, input field, dropdown, scroll view", "#4dabf7", typeof(UIControlsDemo)),
            new Entry("physics", "3D Physics", "Rigidbodies, colliders and forces; tap to throw a ball", "#51cf66", typeof(Physics3DDemo)),
            new Entry("physics2d", "2D Physics", "Rigidbody2D shapes, bounce and joints; tap to drop", "#20c997", typeof(Physics2DDemo)),
            new Entry("particles", "Particle Systems", "Fire, fountain and tap-to-burst fireworks built in code", "#f06595", typeof(ParticlesDemo)),
            new Entry("materials", "Materials & Lights", "Metallic / smoothness grid, moving point lights, shadows", "#339af0", typeof(MaterialsDemo)),
            new Entry("mesh", "Procedural Mesh", "A mesh rebuilt every frame from a wave function", "#845ef7", typeof(ProceduralMeshDemo)),
            new Entry("tweens", "Animation Curves", "Easing with AnimationCurve, coroutines and lerps", "#ff922b", typeof(AnimationDemo)),
            new Entry("trails", "Trails & Lines", "TrailRenderer following your finger, LineRenderer shapes", "#fcc419", typeof(TrailsDemo)),
            new Entry("input", "Touch & Sensors", "Multi-touch, pinch and rotate, accelerometer, gyroscope", "#ff6b6b", typeof(TouchSensorsDemo)),
            new Entry("audio", "Audio Synth", "A playable keyboard generated in OnAudioFilterRead", "#cc5de8", typeof(AudioSynthDemo)),
        };

        Canvas canvas;
        RectTransform menu, stage, header;
        Text headerTitle;
        Demo current;
        float topInset, bottomInset;

        public static float HeaderHeightPixels { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Gallery>() == null)
                new GameObject("Gallery").AddComponent<Gallery>();
        }

        void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // demos are watched, not touched, for long stretches
            Screen.orientation = ScreenOrientation.Portrait;

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIFactory.Background;
            cam.depth = -1;

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(414, 896);
            scaler.matchWidthOrHeight = 0;

            // Safe area in canvas units (the canvas is 414 units wide).
            float unitsPerPixel = 414f / Screen.width;
            topInset = (Screen.height - Screen.safeArea.yMax) * unitsPerPixel;
            bottomInset = Screen.safeArea.yMin * unitsPerPixel;
            HeaderHeightPixels = (topInset + 52) / unitsPerPixel;

            BuildMenu();
            BuildStage();

            string launch = LaunchDemo();
            foreach (var e in Entries)
                if (e.Id == launch) Open(e);
        }

        static string LaunchDemo()
        {
            // -demo <id> on the command line; on iOS the launch arguments also land in NSUserDefaults (PlayerPrefs).
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-demo" || args[i] == "--demo") return args[i + 1];
            return PlayerPrefs.GetString("demo", "");
        }

        void BuildMenu()
        {
            menu = UIFactory.Rect("Menu", canvas.transform);
            UIFactory.Stretch(menu);
            var content = UIFactory.ScrollPage(menu, 0, bottomInset);
            ((VerticalLayoutGroup)content.GetComponent<VerticalLayoutGroup>()).spacing = 10;

            var spacer = UIFactory.Rect("Top", content);
            spacer.gameObject.AddComponent<LayoutElement>().minHeight = topInset + 8;
            var title = UIFactory.Label(content, "Unity Gallery", 34, null, FontStyle.Bold);
            title.gameObject.AddComponent<LayoutElement>().minHeight = 44;
            var sub = UIFactory.Label(content, $"{Entries.Length} demos of Unity {Application.unityVersion} on this device", 15, UIFactory.Secondary);
            sub.gameObject.AddComponent<LayoutElement>().minHeight = 26;

            foreach (var e in Entries)
            {
                var entry = e;
                var card = UIFactory.Panel(content, UIFactory.Card, "Row");
                var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.padding = new RectOffset(12, 14, 12, 12);
                h.spacing = 12;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                card.gameObject.AddComponent<LayoutElement>().minHeight = 68;

                var swatch = UIFactory.Panel(card.transform, entry.Color, "Swatch");
                var sle = swatch.gameObject.AddComponent<LayoutElement>();
                sle.preferredWidth = sle.minWidth = 36;
                sle.preferredHeight = 36;

                var texts = UIFactory.Rect("Texts", card.transform);
                var tv = texts.gameObject.AddComponent<VerticalLayoutGroup>();
                tv.childControlHeight = true;
                tv.childControlWidth = true;
                tv.childForceExpandHeight = false;
                tv.spacing = 2;
                texts.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UIFactory.Label(texts, entry.Title, 17);
                UIFactory.Label(texts, entry.Summary, 13, UIFactory.Secondary);

                var chevron = UIFactory.Label(card.transform, "›", 26, UIFactory.Secondary);
                chevron.gameObject.AddComponent<LayoutElement>().preferredWidth = 12;

                var button = card.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                button.onClick.AddListener(() => Open(entry));
            }
        }

        void BuildStage()
        {
            stage = UIFactory.Rect("Stage", canvas.transform);
            UIFactory.Stretch(stage);
            stage.gameObject.SetActive(false);

            var overlay = UIFactory.Rect("Overlay", stage);
            UIFactory.Stretch(overlay, 0, topInset + 52, 0, 0);

            var bar = UIFactory.Panel(stage, new Color(1, 1, 1, 0.94f), "NavigationBar", false);
            header = bar.rectTransform;
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, topInset + 52);
            var line = UIFactory.Panel(bar.transform, UIFactory.Separator, "Hairline", false);
            line.rectTransform.anchorMin = Vector2.zero;
            line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.sizeDelta = new Vector2(0, 1);

            var back = UIFactory.Label(bar.transform, "‹ Gallery", 18, UIFactory.Accent);
            back.raycastTarget = true;
            var brt = back.rectTransform;
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(0, 0);
            brt.pivot = new Vector2(0, 0);
            brt.anchoredPosition = new Vector2(12, 4);
            brt.sizeDelta = new Vector2(110, 44);
            back.gameObject.AddComponent<Button>().onClick.AddListener(Close);

            headerTitle = UIFactory.Label(bar.transform, "", 17, null, FontStyle.Bold, TextAnchor.MiddleCenter);
            var trt = headerTitle.rectTransform;
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 0);
            trt.pivot = new Vector2(0.5f, 0);
            trt.anchoredPosition = new Vector2(0, 4);
            trt.sizeDelta = new Vector2(-240, 44);
        }

        void Open(Entry entry)
        {
            Close();
            var go = new GameObject(entry.Title);
            current = (Demo)go.AddComponent(entry.Type);
            current.Gallery = this;
            current.Overlay = (RectTransform)stage.Find("Overlay");
            current.Build();
            headerTitle.text = entry.Title;
            menu.gameObject.SetActive(false);
            stage.gameObject.SetActive(true);
        }

        void Close()
        {
            if (current != null)
            {
                Destroy(current.gameObject);
                current = null;
            }
            if (stage != null)
            {
                var overlay = stage.Find("Overlay");
                for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
                stage.gameObject.SetActive(false);
            }
            if (menu != null) menu.gameObject.SetActive(true);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && current != null) Close();
        }

        public float BottomInset => bottomInset;
    }
}
