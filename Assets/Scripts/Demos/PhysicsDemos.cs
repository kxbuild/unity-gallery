using UnityEngine;
using UnityEngine.UI;

namespace KXGallery
{
    static class Materials
    {
        /// <summary>A Standard-shader material (the build script keeps the shader in the player).</summary>
        public static Material Standard(Color color, float metallic = 0f, float smoothness = 0.4f)
        {
            var m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        public static void AddSun(Transform parent, Vector3 euler, float intensity = 1.1f)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.type = LightType.Directional;
            sun.intensity = intensity;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(euler);
        }
    }

    /// <summary>A tower of rigidbody boxes; tap to throw a heavy ball at it.</summary>
    public class Physics3DDemo : Demo
    {
        Camera cam;
        Text label;
        int thrown;

        public override void Build()
        {
            cam = MakeCamera(new Vector3(0, 6, -18), new Vector3(0, 4, 0), new Color32(32, 35, 42, 255));
            Materials.AddSun(transform, new Vector3(50, 30, 0));
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.55f);

            // A thick box rather than a Plane: its collider has volume, so nothing tunnels through it.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(40, 1, 40);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.GetComponent<Renderer>().material = Materials.Standard(new Color32(73, 80, 87, 255));

            BuildTower();

            label = UIFactory.Label(Overlay, "", 15, Color.white);
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(16, -12);
            rt.sizeDelta = new Vector2(380, 24);
            UpdateLabel();

            var reset = UIFactory.Button(Overlay, "Rebuild tower", () =>
            {
                foreach (var body in GetComponentsInChildren<Rigidbody>()) Destroy(body.gameObject);
                thrown = 0;
                UpdateLabel();
                BuildTower();
            }, true);
            var brt = (RectTransform)reset.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0);
            brt.pivot = Vector2.zero;
            brt.anchoredPosition = new Vector2(16, Gallery.BottomInset + 16);
            brt.sizeDelta = new Vector2(150, 40);
        }

        void BuildTower()
        {
            Color[] colors = { new Color32(255, 107, 107, 255), new Color32(252, 196, 25, 255), new Color32(81, 207, 102, 255), new Color32(51, 154, 240, 255), new Color32(204, 93, 232, 255) };
            for (int level = 0; level < 9; level++)
            for (int i = -1; i <= 1; i++)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(transform, false);
                box.transform.localScale = Vector3.one * 0.98f;
                box.transform.position = level % 2 == 0 ? new Vector3(i * 1.0f, 0.5f + level, 0) : new Vector3(0, 0.5f + level, i * 1.0f);
                box.GetComponent<Renderer>().material = Materials.Standard(colors[level % colors.Length], 0, 0.3f);
                box.AddComponent<Rigidbody>().mass = 0.6f;
            }
        }

        void Update()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && IsFreeTouch(t.position, t.fingerId)) Throw(t.position);
            }
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0) && IsFreeTouch(Input.mousePosition, -1)) Throw(Input.mousePosition);
        }

        void Throw(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.SetParent(transform, false);
            ball.transform.position = ray.origin + ray.direction * 1.5f;
            ball.transform.localScale = Vector3.one * 0.9f;
            ball.GetComponent<Renderer>().material = Materials.Standard(new Color32(233, 236, 239, 255), 0.9f, 0.8f);
            var rb = ball.AddComponent<Rigidbody>();
            rb.mass = 6;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = ray.direction * 26;
            thrown++;
            UpdateLabel();
        }

        void UpdateLabel() => label.text = $"Tap to throw a ball ({thrown} thrown)";
    }

    /// <summary>Rigidbody2D circles and boxes falling onto angled shelves, plus a hinge-joint chain.</summary>
    public class Physics2DDemo : Demo
    {
        Camera cam;
        Sprite square;
        int count;

        public override void Build()
        {
            cam = MakeCamera(new Vector3(0, 0, -10), Vector3.zero, new Color32(248, 249, 250, 255));
            cam.orthographic = true;
            cam.orthographicSize = 8;
            float halfW = cam.orthographicSize * Screen.width / Screen.height;

            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);

            Wall(new Vector2(0, -7.6f), new Vector2(halfW * 2, 0.6f), 0);
            Wall(new Vector2(-halfW - 0.3f, 0), new Vector2(0.6f, 20), 0);
            Wall(new Vector2(halfW + 0.3f, 0), new Vector2(0.6f, 20), 0);
            Wall(new Vector2(-halfW * 0.35f, 1.5f), new Vector2(halfW * 1.1f, 0.3f), -12);
            Wall(new Vector2(halfW * 0.35f, -2.5f), new Vector2(halfW * 1.1f, 0.3f), 12);

            // A chain of links hanging from a fixed anchor.
            Rigidbody2D previous = null;
            for (int i = 0; i < 8; i++)
            {
                var link = Body(new Vector2(-halfW * 0.6f + i * 0.55f, 6.5f), false, new Color32(173, 181, 189, 255), 0.25f);
                link.transform.localScale = new Vector3(0.5f, 0.2f, 1);
                var hinge = link.gameObject.AddComponent<HingeJoint2D>();
                if (previous == null)
                {
                    hinge.connectedAnchor = link.position + new Vector2(-0.27f, 0);
                    hinge.anchor = new Vector2(-0.55f, 0);
                }
                else
                {
                    hinge.connectedBody = previous;
                    hinge.anchor = new Vector2(-0.55f, 0);
                    hinge.connectedAnchor = new Vector2(0.55f, 0);
                }
                previous = link;
            }

            for (int i = 0; i < 8; i++) Drop(new Vector2(Random.Range(-halfW + 1, halfW - 1), 4 + i));
        }

        void Wall(Vector2 pos, Vector2 size, float angle)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, angle));
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color32(173, 181, 189, 255);
            go.AddComponent<BoxCollider2D>();
        }

        Rigidbody2D Body(Vector2 pos, bool circle, Color color, float size)
        {
            var go = new GameObject(circle ? "Ball" : "Box");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size * 2;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle ? UIFactory.Circle : square;
            sr.color = color;
            if (circle) go.AddComponent<CircleCollider2D>().radius = 0.5f;
            else go.AddComponent<BoxCollider2D>().size = Vector2.one;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.sharedMaterial = new PhysicsMaterial2D { bounciness = circle ? 0.6f : 0.1f, friction = 0.4f };
            return rb;
        }

        void Drop(Vector2 pos)
        {
            bool circle = count % 2 == 0;
            Body(pos, circle, Color.HSVToRGB(count * 0.09f % 1, 0.6f, 0.95f), Random.Range(0.3f, 0.6f));
            count++;
        }

        void Update()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && IsFreeTouch(t.position, t.fingerId)) Drop(cam.ScreenToWorldPoint(t.position));
            }
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0) && IsFreeTouch(Input.mousePosition, -1)) Drop(cam.ScreenToWorldPoint(Input.mousePosition));
        }
    }
}
