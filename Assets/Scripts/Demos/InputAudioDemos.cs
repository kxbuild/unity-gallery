using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KXGallery
{
    /// <summary>Touch points, a pinch / rotate card, a bubble level and raw sensor readings.</summary>
    public class TouchSensorsDemo : Demo
    {
        RectTransform card, bubble;
        Text readout;
        readonly Dictionary<int, RectTransform> markers = new Dictionary<int, RectTransform>();
        float scale = 1, angle;

        public override void Build()
        {
            Input.gyro.enabled = true;
            var bg = UIFactory.Panel(Overlay, new Color32(241, 243, 245, 255), "Background", false);
            UIFactory.Stretch(bg.rectTransform);

            readout = UIFactory.Label(Overlay, "", 13, new Color32(73, 80, 87, 255), FontStyle.Normal, TextAnchor.UpperLeft);
            var rrt = readout.rectTransform;
            rrt.anchorMin = new Vector2(0, 1);
            rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0, 1);
            rrt.anchoredPosition = new Vector2(16, -12);
            rrt.sizeDelta = new Vector2(-32, 90);

            var c = UIFactory.Panel(Overlay, UIFactory.Accent, "Card");
            card = c.rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.55f);
            card.sizeDelta = new Vector2(180, 120);
            var l = UIFactory.Label(card, "Pinch & rotate", 18, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIFactory.Stretch(l.rectTransform);

            var level = UIFactory.Rect("Level", Overlay);
            var levelImg = level.gameObject.AddComponent<Image>();
            levelImg.sprite = UIFactory.Circle;
            levelImg.color = new Color32(222, 226, 230, 255);
            level.anchorMin = level.anchorMax = new Vector2(0.5f, 0);
            level.anchoredPosition = new Vector2(0, Gallery.BottomInset + 110);
            level.sizeDelta = new Vector2(160, 160);
            bubble = UIFactory.Rect("Bubble", level);
            var bImg = bubble.gameObject.AddComponent<Image>();
            bImg.sprite = UIFactory.Circle;
            bImg.color = new Color32(81, 207, 102, 255);
            bubble.sizeDelta = new Vector2(28, 28);
        }

        void Update()
        {
            // Touch markers
            var seen = new HashSet<int>();
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                seen.Add(t.fingerId);
                if (!markers.TryGetValue(t.fingerId, out var m))
                {
                    m = UIFactory.Rect("Touch", Overlay);
                    var img = m.gameObject.AddComponent<Image>();
                    img.sprite = UIFactory.Circle;
                    img.color = Color.HSVToRGB(t.fingerId * 0.19f % 1, 0.6f, 1) * new Color(1, 1, 1, 0.5f);
                    img.raycastTarget = false;
                    m.sizeDelta = new Vector2(88, 88);
                    markers[t.fingerId] = m;
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Overlay, t.position, null, out var local);
                m.anchorMin = m.anchorMax = new Vector2(0.5f, 0.5f);
                m.anchoredPosition = local;
            }
            foreach (var id in new List<int>(markers.Keys))
                if (!seen.Contains(id)) { Destroy(markers[id].gameObject); markers.Remove(id); }

            // Two fingers: scale and rotate the card; one finger: move it.
            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                Vector2 before = (a.position - a.deltaPosition) - (b.position - b.deltaPosition);
                Vector2 after = a.position - b.position;
                if (before.magnitude > 1)
                {
                    scale = Mathf.Clamp(scale * after.magnitude / before.magnitude, 0.4f, 3);
                    angle += Vector2.SignedAngle(before, after);
                }
            }
            else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved && IsFreeTouch(Input.GetTouch(0).position, Input.GetTouch(0).fingerId))
            {
                card.anchoredPosition += Input.GetTouch(0).deltaPosition * (414f / Screen.width);
            }
            card.localScale = Vector3.one * scale;
            card.localEulerAngles = new Vector3(0, 0, angle);

            Vector3 acc = Input.acceleration;
            Vector3 g = Input.gyro.gravity;
            Vector3 rate = Input.gyro.rotationRateUnbiased;
            readout.text = $"Touches: {Input.touchCount}   Scale: {scale:0.00}   Angle: {angle:0}°\n" +
                           $"Accelerometer  x {acc.x,6:0.00}  y {acc.y,6:0.00}  z {acc.z,6:0.00}\n" +
                           $"Gravity        x {g.x,6:0.00}  y {g.y,6:0.00}  z {g.z,6:0.00}\n" +
                           $"Gyro (rad/s)   x {rate.x,6:0.00}  y {rate.y,6:0.00}  z {rate.z,6:0.00}";
            var tilt = new Vector2(acc.x, acc.y);
            bubble.anchoredPosition = Vector2.ClampMagnitude(tilt, 1) * 62;
        }
    }

    /// <summary>A two-octave keyboard; the waveform is generated sample by sample in OnAudioFilterRead.</summary>
    public class AudioSynthDemo : Demo
    {
        class Voice { public double Phase; public float Amp; public bool Held; }

        static readonly int[] WhiteKeys = { 0, 2, 4, 5, 7, 9, 11 };
        static readonly Dictionary<int, int> BlackKeys = new Dictionary<int, int> { { 1, 0 }, { 3, 1 }, { 6, 3 }, { 8, 4 }, { 10, 5 } };
        const int BaseMidi = 60;

        readonly Dictionary<int, Voice> voices = new Dictionary<int, Voice>();
        readonly object gate = new object();
        readonly List<int> finished = new List<int>();
        readonly Dictionary<int, int> pointerNote = new Dictionary<int, int>();
        readonly Dictionary<int, Image> keyImages = new Dictionary<int, Image>();
        int wave;
        double sampleRate;
        RectTransform keyboard;
        Text info;

        public override void Build()
        {
            sampleRate = AudioSettings.outputSampleRate;
            gameObject.AddComponent<AudioSource>().Play(); // an AudioSource makes Unity call OnAudioFilterRead

            var bg = UIFactory.Panel(Overlay, new Color32(28, 28, 30, 255), "Background", false);
            UIFactory.Stretch(bg.rectTransform);

            var waves = UIFactory.Row(Overlay, 40, 8);
            waves.anchorMin = waves.anchorMax = new Vector2(0, 1);
            waves.pivot = new Vector2(0, 1);
            waves.anchoredPosition = new Vector2(16, -12);
            waves.sizeDelta = new Vector2(382, 40);
            string[] names = { "Sine", "Square", "Saw", "Triangle" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                UIFactory.Button(waves, names[i], () => { wave = index; info.text = $"{names[index]} wave"; }, i == 0);
            }
            info = UIFactory.Label(Overlay, "Sine wave", 15, new Color(1, 1, 1, 0.7f));
            var irt = info.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, 1);
            irt.pivot = new Vector2(0, 1);
            irt.anchoredPosition = new Vector2(16, -64);
            irt.sizeDelta = new Vector2(300, 24);

            keyboard = UIFactory.Rect("Keyboard", Overlay);
            keyboard.anchorMin = new Vector2(0, 0);
            keyboard.anchorMax = new Vector2(1, 0);
            keyboard.pivot = new Vector2(0.5f, 0);
            keyboard.anchoredPosition = new Vector2(0, Gallery.BottomInset + 12);
            keyboard.sizeDelta = new Vector2(-16, 280);
            float w = 1f / 14;
            for (int i = 0; i < 14; i++)
            {
                int note = BaseMidi + 12 * (i / 7) + WhiteKeys[i % 7];
                var key = UIFactory.Panel(keyboard, Color.white, "White", false);
                key.rectTransform.anchorMin = new Vector2(i * w, 0);
                key.rectTransform.anchorMax = new Vector2((i + 1) * w, 1);
                key.rectTransform.offsetMin = new Vector2(1, 0);
                key.rectTransform.offsetMax = new Vector2(-1, 0);
                keyImages[note] = key;
            }
            for (int octave = 0; octave < 2; octave++)
            foreach (var kv in BlackKeys)
            {
                int note = BaseMidi + 12 * octave + kv.Key;
                float x = (octave * 7 + kv.Value + 1) * w;
                var key = UIFactory.Panel(keyboard, new Color32(33, 37, 41, 255), "Black", false);
                key.rectTransform.anchorMin = new Vector2(x - w * 0.3f, 0.4f);
                key.rectTransform.anchorMax = new Vector2(x + w * 0.3f, 1);
                key.rectTransform.offsetMin = key.rectTransform.offsetMax = Vector2.zero;
                keyImages[note] = key;
            }
        }

        int NoteAt(Vector2 screen)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(keyboard, screen, null, out var local)) return -1;
            var r = keyboard.rect;
            float u = (local.x - r.xMin) / r.width, v = (local.y - r.yMin) / r.height;
            if (u < 0 || u > 1 || v < 0 || v > 1) return -1;
            float w = 1f / 14;
            if (v > 0.4f)
                for (int octave = 0; octave < 2; octave++)
                foreach (var kv in BlackKeys)
                {
                    float x = (octave * 7 + kv.Value + 1) * w;
                    if (u > x - w * 0.3f && u < x + w * 0.3f) return BaseMidi + 12 * octave + kv.Key;
                }
            int i = Mathf.Min(13, (int)(u / w));
            return BaseMidi + 12 * (i / 7) + WhiteKeys[i % 7];
        }

        void Press(int pointer, Vector2 screen)
        {
            int note = NoteAt(screen);
            if (pointerNote.TryGetValue(pointer, out int old) && old == note) return;
            Release(pointer);
            if (note < 0) return;
            pointerNote[pointer] = note;
            lock (gate)
            {
                if (!voices.TryGetValue(note, out var voice)) voices[note] = voice = new Voice();
                voice.Held = true;
            }
        }

        void Release(int pointer)
        {
            if (!pointerNote.TryGetValue(pointer, out int note)) return;
            pointerNote.Remove(pointer);
            if (pointerNote.ContainsValue(note)) return;
            lock (gate) { if (voices.TryGetValue(note, out var voice)) voice.Held = false; }
        }

        void Update()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) Release(t.fingerId);
                else Press(t.fingerId, t.position);
            }
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButton(0)) Press(-1, Input.mousePosition);
                else Release(-1);
            }
            foreach (var kv in keyImages)
            {
                bool down = pointerNote.ContainsValue(kv.Key);
                bool black = BlackKeys.ContainsKey((kv.Key - BaseMidi) % 12);
                kv.Value.color = down ? (black ? new Color32(51, 154, 240, 255) : new Color32(116, 192, 252, 255)) : (black ? new Color32(33, 37, 41, 255) : Color.white);
            }
        }

        // Runs on the audio thread.
        void OnAudioFilterRead(float[] data, int channels)
        {
            lock (gate)
            {
                finished.Clear();
                for (int i = 0; i < data.Length; i += channels)
                {
                    float s = 0;
                    foreach (var kv in voices)
                    {
                        var v = kv.Value;
                        v.Amp += ((v.Held ? 0.2f : 0f) - v.Amp) * (v.Held ? 0.004f : 0.0015f);
                        double freq = 440.0 * System.Math.Pow(2, (kv.Key - 69) / 12.0);
                        v.Phase = (v.Phase + freq / sampleRate) % 1.0;
                        s += Osc((float)v.Phase) * v.Amp;
                    }
                    s = Mathf.Clamp(s, -1, 1);
                    for (int c = 0; c < channels; c++) data[i + c] = s;
                }
                // Drop voices that have faded out (done once per buffer, not per sample).
                foreach (var kv in voices)
                    if (!kv.Value.Held && kv.Value.Amp < 0.0005f) finished.Add(kv.Key);
                foreach (var n in finished) voices.Remove(n);
            }
        }

        float Osc(float p)
        {
            switch (wave)
            {
                case 1: return p < 0.5f ? 1 : -1;
                case 2: return 2 * p - 1;
                case 3: return 4 * Mathf.Abs(p - 0.5f) - 1;
                default: return Mathf.Sin(p * Mathf.PI * 2);
            }
        }
    }
}
