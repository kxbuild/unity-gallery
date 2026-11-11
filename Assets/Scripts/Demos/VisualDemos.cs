using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace KXGallery
{
    /// <summary>ParticleSystems configured entirely from code.</summary>
    public class ParticlesDemo : Demo
    {
        Camera cam;
        Material additive;

        public override void Build()
        {
            cam = MakeCamera(new Vector3(0, 3, -12), new Vector3(0, 3, 0), new Color32(11, 16, 32, 255));
            additive = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            additive.mainTexture = UIFactory.Circle.texture;

            // Fountain
            var fountain = MakeSystem("Fountain", new Vector3(-2.2f, 0, 0));
            var main = fountain.main;
            main.startLifetime = 2.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(7, 9);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.gravityModifier = 1.2f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color32(116, 192, 252, 255), new Color32(77, 171, 247, 255));
            var shape = fountain.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8;
            shape.rotation = new Vector3(-90, 0, 0);
            var emission = fountain.emission;
            emission.rateOverTime = 120;

            // Flame
            var flame = MakeSystem("Flame", new Vector3(2.2f, 0.3f, 0));
            main = flame.main;
            main.startLifetime = 1.1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12;
            shape.radius = 0.35f;
            shape.rotation = new Vector3(-90, 0, 0);
            emission = flame.emission;
            emission.rateOverTime = 90;
            var col = flame.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color32(255, 243, 191, 255), 0), new GradientColorKey(new Color32(255, 169, 77, 255), 0.35f), new GradientColorKey(new Color32(240, 62, 62, 255), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var size = flame.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));

            var hint = UIFactory.Label(Overlay, "Tap anywhere for fireworks", 15, new Color(1, 1, 1, 0.75f));
            var rt = hint.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(16, -12);
            rt.sizeDelta = new Vector2(380, 24);
        }

        ParticleSystem MakeSystem(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            go.GetComponent<ParticleSystemRenderer>().material = additive;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            return ps;
        }

        void Burst(Vector2 screen)
        {
            var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 12));
            var ps = MakeSystem("Firework", world);
            ps.Stop();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2, 5);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.gravityModifier = 0.4f;
            float hue = Random.value;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.HSVToRGB(hue, 0.4f, 1), Color.HSVToRGB((hue + 0.1f) % 1, 0.9f, 1));
            main.stopAction = ParticleSystemStopAction.Destroy;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 160) });
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.08f;
            limit.limit = 1;
            ps.Play();
        }

        void Update()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && IsFreeTouch(t.position, t.fingerId)) Burst(t.position);
            }
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0) && IsFreeTouch(Input.mousePosition, -1)) Burst(Input.mousePosition);
        }
    }

    /// <summary>Standard-shader spheres from rough to smooth and dielectric to metal, lit by moving point lights.</summary>
    public class MaterialsDemo : Demo
    {
        Transform[] lights;
        Transform pivot;
        Camera cam;
        float yaw = 20, pitch = 25, distance = 11;

        public override void Build()
        {
            cam = MakeCamera(Vector3.zero, Vector3.zero, new Color32(24, 28, 36, 255));
            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(transform, false);
            cam.transform.SetParent(pivot, false);
            UpdateCamera();
            RenderSettings.ambientLight = new Color(0.2f, 0.22f, 0.26f);
            Materials.AddSun(transform, new Vector3(45, -30, 0), 0.8f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(transform, false);
            floor.transform.localScale = new Vector3(3, 1, 3);
            floor.GetComponent<Renderer>().material = Materials.Standard(new Color32(52, 58, 64, 255), 0, 0.6f);

            for (int row = 0; row < 3; row++)
            for (int colIndex = 0; colIndex < 5; colIndex++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(transform, false);
                s.transform.position = new Vector3(-4 + colIndex * 2, 0.6f, -2 + row * 2);
                s.transform.localScale = Vector3.one * 1.2f;
                Color c = Color.HSVToRGB(0.58f - row * 0.15f, 0.55f, 0.95f);
                s.GetComponent<Renderer>().material = Materials.Standard(c, colIndex / 4f, 0.15f + row * 0.4f);
            }

            Color[] colors = { new Color32(255, 107, 107, 255), new Color32(77, 171, 247, 255), new Color32(252, 196, 25, 255) };
            lights = new Transform[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var l = new GameObject("Point Light").AddComponent<Light>();
                l.transform.SetParent(transform, false);
                l.type = LightType.Point;
                l.color = colors[i];
                l.range = 7;
                l.intensity = 2.5f;
                var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(bulb.GetComponent<Collider>());
                bulb.transform.SetParent(l.transform, false);
                bulb.transform.localScale = Vector3.one * 0.2f;
                var m = new Material(Shader.Find("Unlit/Color")) { color = colors[i] };
                bulb.GetComponent<Renderer>().material = m;
                lights[i] = l.transform;
            }

            var note = UIFactory.Label(Overlay, "Left → right: metallic 0 → 1   Front → back: smoothness", 13, new Color(1, 1, 1, 0.7f));
            var rt = note.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -12);
            rt.sizeDelta = new Vector2(-32, 20);
            note.alignment = TextAnchor.MiddleCenter;
        }

        void UpdateCamera()
        {
            pitch = Mathf.Clamp(pitch, 8, 80);
            pivot.rotation = Quaternion.Euler(pitch, yaw, 0);
            cam.transform.localPosition = new Vector3(0, 0, -distance);
            cam.transform.localRotation = Quaternion.identity;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < lights.Length; i++)
            {
                float a = t * (0.6f + i * 0.25f) + i * 2.1f;
                lights[i].position = new Vector3(Mathf.Cos(a) * 4.5f, 1.6f + Mathf.Sin(t + i) * 0.5f, Mathf.Sin(a) * 3);
            }
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                var d = Input.GetTouch(0).deltaPosition;
                yaw += d.x * 0.2f;
                pitch -= d.y * 0.2f;
            }
            else if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                float before = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                float after = Vector2.Distance(a.position, b.position);
                if (after > 1) distance = Mathf.Clamp(distance * before / after, 5, 20);
            }
            else if (Input.touchCount == 0 && Input.GetMouseButton(0))
            {
                yaw += Input.GetAxis("Mouse X") * 4;
                pitch -= Input.GetAxis("Mouse Y") * 4;
            }
            else
            {
                yaw += Time.deltaTime * 6;
            }
            UpdateCamera();
        }
    }

    /// <summary>A grid mesh whose vertices follow a moving wave; normals and colors are recomputed every frame.</summary>
    public class ProceduralMeshDemo : Demo
    {
        Mesh mesh;
        Vector3[] baseVertices, vertices;
        Color[] colors;
        const int N = 60;

        public override void Build()
        {
            MakeCamera(new Vector3(0, 7, -10), new Vector3(0, 0, 1), new Color32(20, 22, 30, 255));
            Materials.AddSun(transform, new Vector3(55, 20, 0), 1.0f);
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f);

            mesh = new Mesh { name = "Waves" };
            baseVertices = new Vector3[(N + 1) * (N + 1)];
            colors = new Color[baseVertices.Length];
            var tris = new int[N * N * 6];
            for (int z = 0, i = 0; z <= N; z++)
            for (int x = 0; x <= N; x++, i++)
                baseVertices[i] = new Vector3((x - N / 2f) * 0.2f, 0, (z - N / 2f) * 0.2f);
            for (int z = 0, t = 0; z < N; z++)
            for (int x = 0; x < N; x++)
            {
                int v = z * (N + 1) + x;
                tris[t++] = v; tris[t++] = v + N + 1; tris[t++] = v + 1;
                tris[t++] = v + 1; tris[t++] = v + N + 1; tris[t++] = v + N + 2;
            }
            vertices = (Vector3[])baseVertices.Clone();
            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.MarkDynamic();

            var go = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mat = new Material(Shader.Find("Particles/Standard Surface"));
            mat.SetFloat("_Glossiness", 0.6f);
            go.GetComponent<MeshRenderer>().material = mat;

            var note = UIFactory.Label(Overlay, $"{baseVertices.Length} vertices, {tris.Length / 3} triangles, updated every frame", 13, new Color(1, 1, 1, 0.7f));
            var rt = note.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -12);
            rt.sizeDelta = new Vector2(-32, 20);
            note.alignment = TextAnchor.MiddleCenter;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = baseVertices[i];
                float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                float y = Mathf.Sin(r * 1.6f - t * 2.2f) * 0.6f / (1 + r * 0.25f) + Mathf.Sin(p.x * 0.8f + t) * 0.25f;
                vertices[i] = new Vector3(p.x, y, p.z);
                colors[i] = Color.HSVToRGB(0.55f + y * 0.25f, 0.65f, 0.9f);
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            transform.Rotate(0, 6 * Time.deltaTime, 0);
        }
    }

    /// <summary>Easing curves drawn with AnimationCurve and played with coroutines.</summary>
    public class AnimationDemo : Demo
    {
        static readonly (string name, AnimationCurve curve)[] Curves =
        {
            ("Linear", AnimationCurve.Linear(0, 0, 1, 1)),
            ("Ease in-out", AnimationCurve.EaseInOut(0, 0, 1, 1)),
            ("Ease in", new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 2.5f, 0))),
            ("Ease out", new AnimationCurve(new Keyframe(0, 0, 0, 2.5f), new Keyframe(1, 1, 0, 0))),
            ("Overshoot", new AnimationCurve(new Keyframe(0, 0, 0, 3), new Keyframe(0.7f, 1.15f, 0, 0), new Keyframe(1, 1, 0, 0))),
            ("Bounce", new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.45f, 1, 3, -2.5f), new Keyframe(0.7f, 0.82f, 0, 0), new Keyframe(0.85f, 1, 2, -1.5f), new Keyframe(0.93f, 0.95f, 0, 0), new Keyframe(1, 1, 1, 0))),
            ("Anticipate", new AnimationCurve(new Keyframe(0, 0, 0, -1.5f), new Keyframe(0.3f, -0.12f, 0, 0), new Keyframe(1, 1, 2.2f, 0))),
        };

        RectTransform[] dots;
        float trackWidth;
        RectTransform hero;

        public override void Build()
        {
            var bg = UIFactory.Panel(Overlay, UIFactory.Background, "Background", false);
            UIFactory.Stretch(bg.rectTransform);
            var page = UIFactory.ScrollPage(Overlay, 0, Gallery.BottomInset);
            var section = UIFactory.Section(page, "AnimationCurve easing", "Each dot evaluates its curve every frame in a coroutine.");
            dots = new RectTransform[Curves.Length];
            for (int i = 0; i < Curves.Length; i++)
            {
                var row = UIFactory.Row(section, 30);
                var label = UIFactory.Label(row, Curves[i].name, 13, UIFactory.Secondary);
                label.gameObject.AddComponent<LayoutElement>().preferredWidth = 86;
                var track = UIFactory.Panel(row, UIFactory.Background, "Track");
                var le = track.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.preferredHeight = 26;
                var dot = UIFactory.Rect("Dot", track.transform);
                var img = dot.gameObject.AddComponent<Image>();
                img.sprite = UIFactory.Circle;
                img.color = Color.HSVToRGB(i / (float)Curves.Length, 0.6f, 0.95f);
                dot.anchorMin = dot.anchorMax = new Vector2(0, 0.5f);
                dot.sizeDelta = new Vector2(22, 22);
                dot.anchoredPosition = new Vector2(13, 0);
                dots[i] = dot;
            }
            UIFactory.Button(section, "Replay", () => { StopAllCoroutines(); StartCoroutine(PlayAll()); }, true);

            var seq = UIFactory.Section(page, "Coroutine sequence", "Move, rotate, color and scale steps run one after another.");
            var stageRt = UIFactory.Rect("Stage", seq);
            stageRt.gameObject.AddComponent<LayoutElement>().minHeight = 180;
            var h = UIFactory.Panel(stageRt, UIFactory.Accent, "Hero");
            hero = h.rectTransform;
            hero.anchorMin = hero.anchorMax = new Vector2(0.5f, 0.5f);
            hero.sizeDelta = new Vector2(64, 64);
            UIFactory.Button(seq, "Play sequence", () => StartCoroutine(Sequence(h)));

            StartCoroutine(PlayAll());
        }

        IEnumerator PlayAll()
        {
            yield return null; // wait one frame for the layout to size the tracks
            trackWidth = ((RectTransform)dots[0].parent).rect.width - 26;
            while (true)
            {
                yield return Animate(1.4f, k => { for (int i = 0; i < dots.Length; i++) dots[i].anchoredPosition = new Vector2(13 + trackWidth * Curves[i].curve.Evaluate(k), 0); });
                yield return new WaitForSeconds(0.5f);
                yield return Animate(1.4f, k => { for (int i = 0; i < dots.Length; i++) dots[i].anchoredPosition = new Vector2(13 + trackWidth * Curves[i].curve.Evaluate(1 - k), 0); });
                yield return new WaitForSeconds(0.5f);
            }
        }

        static IEnumerator Animate(float duration, System.Action<float> step)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                step(t / duration);
                yield return null;
            }
            step(1);
        }

        IEnumerator Sequence(Image img)
        {
            var ease = Curves[1].curve;
            var back = Curves[4].curve;
            Vector2 start = hero.anchoredPosition;
            yield return Animate(0.5f, k => hero.anchoredPosition = Vector2.LerpUnclamped(start, new Vector2(-120, 50), back.Evaluate(k)));
            yield return Animate(0.5f, k => hero.anchoredPosition = Vector2.LerpUnclamped(new Vector2(-120, 50), new Vector2(120, 50), ease.Evaluate(k)));
            yield return Animate(0.4f, k => hero.localEulerAngles = new Vector3(0, 0, 180 * ease.Evaluate(k)));
            yield return Animate(0.4f, k => img.color = Color.Lerp(UIFactory.Accent, new Color32(255, 107, 107, 255), k));
            yield return Animate(0.5f, k => hero.anchoredPosition = Vector2.LerpUnclamped(new Vector2(120, 50), new Vector2(120, -50), Curves[5].curve.Evaluate(k)));
            yield return Animate(0.3f, k => hero.localScale = Vector3.one * Mathf.Lerp(1, 1.5f, k));
            yield return Animate(0.3f, k => hero.localScale = Vector3.one * Mathf.Lerp(1.5f, 1, k));
            yield return Animate(0.6f, k =>
            {
                hero.anchoredPosition = Vector2.LerpUnclamped(new Vector2(120, -50), start, back.Evaluate(k));
                hero.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(180, 360, k));
                img.color = Color.Lerp(new Color32(255, 107, 107, 255), UIFactory.Accent, k);
            });
        }
    }

    /// <summary>A TrailRenderer that follows each finger, over LineRenderer shapes.</summary>
    public class TrailsDemo : Demo
    {
        Camera cam;
        readonly System.Collections.Generic.Dictionary<int, TrailRenderer> trails = new System.Collections.Generic.Dictionary<int, TrailRenderer>();
        Material lineMat;
        LineRenderer spiral;

        public override void Build()
        {
            cam = MakeCamera(new Vector3(0, 0, -10), Vector3.zero, new Color32(16, 18, 28, 255));
            cam.orthographic = true;
            cam.orthographicSize = 8;
            lineMat = new Material(Shader.Find("Sprites/Default"));

            spiral = NewLine("Spiral", 0.06f, new Color32(77, 171, 247, 255), new Color32(204, 93, 232, 255));
            spiral.positionCount = 400;

            var star = NewLine("Star", 0.08f, new Color32(252, 196, 25, 255), new Color32(255, 146, 43, 255));
            star.loop = true;
            star.positionCount = 10;
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float r = i % 2 == 0 ? 2f : 0.9f;
                star.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 4.5f, 0));
            }

            var hint = UIFactory.Label(Overlay, "Draw with one or more fingers", 15, new Color(1, 1, 1, 0.75f));
            var rt = hint.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(16, -12);
            rt.sizeDelta = new Vector2(380, 24);
        }

        LineRenderer NewLine(string name, float width, Color a, Color b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.widthMultiplier = width;
            lr.startColor = a;
            lr.endColor = b;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            return lr;
        }

        TrailRenderer NewTrail(int finger)
        {
            var go = new GameObject("Trail " + finger);
            go.transform.SetParent(transform, false);
            var tr = go.AddComponent<TrailRenderer>();
            tr.material = lineMat;
            tr.time = 0.8f;
            tr.minVertexDistance = 0.05f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.35f), new Keyframe(1, 0));
            float hue = (finger * 0.17f + Time.time * 0.1f) % 1;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.HSVToRGB(hue, 0.6f, 1), 0), new GradientColorKey(Color.HSVToRGB((hue + 0.15f) % 1, 0.8f, 1), 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            tr.colorGradient = g;
            tr.numCapVertices = 6;
            return tr;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < spiral.positionCount; i++)
            {
                float k = i / (float)spiral.positionCount;
                float a = k * Mathf.PI * 12 + t;
                float r = 0.2f + k * 3.6f;
                spiral.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.6f + 2.5f, 0));
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                var world = cam.ScreenToWorldPoint(touch.position);
                world.z = 0;
                if (touch.phase == TouchPhase.Began && IsFreeTouch(touch.position, touch.fingerId))
                {
                    trails[touch.fingerId] = NewTrail(touch.fingerId);
                    trails[touch.fingerId].transform.position = world;
                }
                else if (trails.TryGetValue(touch.fingerId, out var tr))
                {
                    tr.transform.position = world;
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        Destroy(tr.gameObject, tr.time);
                        trails.Remove(touch.fingerId);
                    }
                }
            }
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0) && IsFreeTouch(Input.mousePosition, -1)) trails[-1] = NewTrail(0);
                if (trails.TryGetValue(-1, out var mt))
                {
                    var w = cam.ScreenToWorldPoint(Input.mousePosition);
                    w.z = 0;
                    mt.transform.position = w;
                    if (Input.GetMouseButtonUp(0)) { Destroy(mt.gameObject, mt.time); trails.Remove(-1); }
                }
            }
        }
    }
}
