using System.Collections.Generic;
using UnityEngine;

// 힐링 과일 썰기 - 모든 오브젝트를 코드로 생성합니다 (외부 에셋 불필요)
public class GameManager : MonoBehaviour
{
    public Shader[] shaderRefs; // 빌드에서 셰이더가 제거되지 않도록 씬에 참조 보관

    class FruitDef { public string name; public Color outer, inner; public float r; public Vector3 shape = Vector3.one; }
    class Item { public Transform t; public FruitDef d; public float spin; }
    class Fx { public Transform t; public float born, life; public Vector3 s0; }
    class Pop { public string text; public Vector3 pos; public float born; public Color col; }
    class KnifeDef { public string name; public Vector3 blade; public Color bladeCol, handleCol; public float metal, pitch; }

    static Color C(int h) { return new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f); }

    FruitDef[] fruits;
    KnifeDef[] knives;
    readonly List<Item> items = new List<Item>();
    readonly List<Fx> fxs = new List<Fx>();
    readonly List<Pop> pops = new List<Pop>();
    readonly List<Transform> planks = new List<Transform>();
    readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    readonly Dictionary<int, Mesh> caps = new Dictionary<int, Mesh>();

    Camera cam;
    AudioSource sfx, whooshSrc, chimeSrc, music;
    AudioClip[] sliceClips; AudioClip whoosh, chime;
    GameObject knifeGO; int knifeIdx;
    float knifeT = -1, knifeX; bool pendingCut;
    float spawnTimer, speed = 2.2f, lastSlice;
    int score, combo; bool soundOn = true;

    float uiScale = 1, vw; Rect knifeBtn, soundBtn;
    GUIStyle big, mid, btn;

    const float RailTop = 0.09f;

    void Start()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        fruits = new[] {
            new FruitDef{ name="Apple",       outer=C(0xD9342B), inner=C(0xFFF3C9), r=0.50f },
            new FruitDef{ name="Orange",      outer=C(0xF28C1B), inner=C(0xFFB84D), r=0.52f },
            new FruitDef{ name="Lemon",       outer=C(0xF7DC3C), inner=C(0xFFF59D), r=0.42f, shape=new Vector3(1.3f,0.95f,0.95f) },
            new FruitDef{ name="Watermelon",  outer=C(0x2E8B57), inner=C(0xFF4F5E), r=0.72f, shape=new Vector3(1.25f,1f,1f) },
            new FruitDef{ name="Kiwi",        outer=C(0x8B6B3E), inner=C(0x8FD14F), r=0.42f, shape=new Vector3(1.2f,0.95f,0.95f) },
            new FruitDef{ name="Peach",       outer=C(0xFFA98F), inner=C(0xFFD27A), r=0.50f },
            new FruitDef{ name="Plum",        outer=C(0x6A3D9A), inner=C(0xE8D86A), r=0.42f },
            new FruitDef{ name="Lime",        outer=C(0x6FBF3A), inner=C(0xD7F2A0), r=0.42f },
            new FruitDef{ name="Dragonfruit", outer=C(0xE8318F), inner=C(0xFFFFFF), r=0.50f, shape=new Vector3(1.2f,1f,1f) },
        };
        knives = new[] {
            new KnifeDef{ name="Chef Knife",  blade=new Vector3(0.03f,0.45f,1.5f), bladeCol=C(0xDDE3E8), handleCol=C(0x5B3A29), metal=0.8f, pitch=1.0f },
            new KnifeDef{ name="Cleaver",     blade=new Vector3(0.07f,0.60f,1.7f), bladeCol=C(0x9AA3AB), handleCol=C(0x222222), metal=0.8f, pitch=0.8f },
            new KnifeDef{ name="Golden Blade",blade=new Vector3(0.03f,0.40f,1.4f), bladeCol=C(0xFFC83D), handleCol=C(0xB3261E), metal=1.0f, pitch=1.15f },
            new KnifeDef{ name="Katana",      blade=new Vector3(0.025f,0.22f,2.2f),bladeCol=C(0xF2F2F2), handleCol=C(0x1B1B3A), metal=0.9f, pitch=1.3f },
            new KnifeDef{ name="Ice Blade",   blade=new Vector3(0.04f,0.45f,1.5f), bladeCol=C(0x9FE8FF), handleCol=C(0xFFFFFF), metal=0.3f, pitch=1.5f },
        };

        SetupScene();
        SetupAudio();
        BuildKnife();
        Spawn(-4.5f); Spawn(-0.5f);
    }

    // ───────── 씬 구성 ─────────
    void SetupScene()
    {
        var cg = new GameObject("Camera"); cg.tag = "MainCamera";
        cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C(0xCFEFF5);
        cam.transform.position = new Vector3(-2.8f, 4.4f, -6.8f);
        cam.transform.LookAt(new Vector3(0.3f, 0.3f, 0f));
        cam.fieldOfView = Mathf.Clamp(45f * Mathf.Max(1f, 1.9f / cam.aspect), 45f, 70f);

        var lg = new GameObject("Light"); var l = lg.AddComponent<Light>();
        l.type = LightType.Directional; l.intensity = 1.1f; l.shadows = LightShadows.Soft;
        l.color = C(0xFFF4E0); lg.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.68f);

        var wood = Mat(C(0x8B5A2B), 0.2f); var wood2 = Mat(C(0xC89B63), 0.15f);
        Prim(PrimitiveType.Cube, new Vector3(30, 0.14f, 0.14f), wood, new Vector3(0, 0, -0.3f));
        Prim(PrimitiveType.Cube, new Vector3(30, 0.14f, 0.14f), wood, new Vector3(0, 0, 0.3f));
        Prim(PrimitiveType.Cube, new Vector3(80, 0.2f, 60), Mat(C(0xA8D5A2), 0.05f), new Vector3(0, -1.3f, 0));
        for (int i = 0; i < 17; i++)
            planks.Add(Prim(PrimitiveType.Cube, new Vector3(0.45f, 0.1f, 1.3f), wood2, new Vector3(-10 + i * 1.2f, -0.1f, 0), false).transform);
    }

    void SetupAudio()
    {
        sliceClips = new[] { ProcAudio.Slice(1), ProcAudio.Slice(2), ProcAudio.Slice(3), ProcAudio.Slice(4) };
        whoosh = ProcAudio.Whoosh(); chime = ProcAudio.Chime();
        sfx = gameObject.AddComponent<AudioSource>();
        whooshSrc = gameObject.AddComponent<AudioSource>(); whooshSrc.volume = 0.5f;
        chimeSrc = gameObject.AddComponent<AudioSource>(); chimeSrc.volume = 0.6f;
        music = gameObject.AddComponent<AudioSource>();
        music.clip = ProcAudio.Music(); music.loop = true; music.volume = 0.35f; music.Play();
    }

    // ───────── 헬퍼 ─────────
    Material Mat(Color c, float gloss = 0.4f, float metal = 0f)
    {
        string key = c.ToString() + gloss + metal;
        Material m;
        if (mats.TryGetValue(key, out m)) return m;
        var sh = Shader.Find("Standard");
        if (sh == null && shaderRefs != null && shaderRefs.Length > 0) sh = shaderRefs[0];
        m = new Material(sh); m.color = c;
        m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", metal);
        mats[key] = m; return m;
    }

    GameObject Prim(PrimitiveType t, Vector3 s, Material m, Vector3 p, bool collider = true)
    {
        var g = GameObject.CreatePrimitive(t);
        g.transform.position = p; g.transform.localScale = s;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Destroy(g.GetComponent<Collider>());
        return g;
    }

    // 구의 일부(x >= c)와 단면(절단면 + 껍질 링)을 가진 메쉬
    Mesh Cap(float c)
    {
        int key = Mathf.RoundToInt(c * 20f);
        Mesh mesh;
        if (caps.TryGetValue(key, out mesh)) return mesh;
        c = key / 20f;
        int N = 14, M = 24; float tm = Mathf.Acos(c), sr = Mathf.Sin(tm);
        var v = new List<Vector3>(); var n = new List<Vector3>();
        var t0 = new List<int>(); var t1 = new List<int>(); var t2 = new List<int>();
        for (int i = 0; i <= N; i++)
        {
            float th = tm * i / N, x = Mathf.Cos(th), r = Mathf.Sin(th);
            for (int j = 0; j <= M; j++)
            {
                float ph = 2 * Mathf.PI * j / M;
                var p = new Vector3(x, r * Mathf.Cos(ph), r * Mathf.Sin(ph)); v.Add(p); n.Add(p);
            }
        }
        for (int i = 0; i < N; i++)
            for (int j = 0; j < M; j++)
            {
                int a = i * (M + 1) + j, b = a + 1, c2 = a + M + 1, d = c2 + 1;
                t0.Add(a); t0.Add(c2); t0.Add(b); t0.Add(b); t0.Add(c2); t0.Add(d);
            }
        int s = v.Count; Vector3 nn = -Vector3.right;
        v.Add(new Vector3(c, 0, 0)); n.Add(nn);
        for (int j = 0; j <= M; j++) { float ph = 2 * Mathf.PI * j / M; v.Add(new Vector3(c, sr * 0.84f * Mathf.Cos(ph), sr * 0.84f * Mathf.Sin(ph))); n.Add(nn); }
        for (int j = 0; j <= M; j++) { float ph = 2 * Mathf.PI * j / M; v.Add(new Vector3(c, sr * Mathf.Cos(ph), sr * Mathf.Sin(ph))); n.Add(nn); }
        for (int j = 0; j < M; j++) { t1.Add(s); t1.Add(s + 2 + j); t1.Add(s + 1 + j); }
        int o = s + 1 + (M + 1);
        for (int j = 0; j < M; j++)
        {
            int a = s + 1 + j, b = a + 1, c2 = o + j, d = c2 + 1;
            t2.Add(a); t2.Add(b); t2.Add(c2); t2.Add(b); t2.Add(d); t2.Add(c2);
        }
        mesh = new Mesh(); mesh.SetVertices(v); mesh.SetNormals(n);
        mesh.subMeshCount = 3; mesh.SetTriangles(t0, 0); mesh.SetTriangles(t1, 1); mesh.SetTriangles(t2, 2);
        mesh.RecalculateBounds(); caps[key] = mesh; return mesh;
    }

    // ───────── 칼 ─────────
    void BuildKnife()
    {
        if (knifeGO) Destroy(knifeGO);
        var k = knives[knifeIdx];
        knifeGO = new GameObject("Knife");
        var bl = Prim(PrimitiveType.Cube, k.blade, Mat(k.bladeCol, 0.85f, k.metal), Vector3.zero, false);
        bl.transform.SetParent(knifeGO.transform); bl.transform.localPosition = new Vector3(0, k.blade.y / 2, 0);
        var hd = Prim(PrimitiveType.Cube, new Vector3(0.09f, 0.16f, 0.75f), Mat(k.handleCol, 0.3f), Vector3.zero, false);
        hd.transform.SetParent(knifeGO.transform); hd.transform.localPosition = new Vector3(0, k.blade.y * 0.7f, -(k.blade.z / 2 + 0.36f));
        var gd = Prim(PrimitiveType.Cube, new Vector3(0.08f, k.blade.y, 0.06f), Mat(C(0xC9A227), 0.8f, 1f), Vector3.zero, false);
        gd.transform.SetParent(knifeGO.transform); gd.transform.localPosition = new Vector3(0, k.blade.y / 2, -(k.blade.z / 2 + 0.02f));
        knifeGO.SetActive(false); knifeT = -1; pendingCut = false;
    }

    // ───────── 과일 ─────────
    void Spawn(float x)
    {
        var d = fruits[Random.Range(0, fruits.Length)];
        float y = RailTop + Mathf.Sqrt(d.r * d.r - 0.09f) * d.shape.y;
        var g = Prim(PrimitiveType.Sphere, d.r * 2f * d.shape, Mat(d.outer, 0.55f), new Vector3(x, y, 0), false);
        g.transform.rotation = Quaternion.Euler(Random.Range(0, 360f), 0, 0);
        items.Add(new Item { t = g.transform, d = d, spin = Random.Range(-25f, 25f) });
    }

    void Slice(Item it, float cutX)
    {
        var d = it.d; Vector3 sc = d.r * d.shape; Vector3 pos = it.t.position; Quaternion rot = it.t.rotation;
        float c = Mathf.Clamp((cutX - pos.x) / sc.x, -0.8f, 0.8f);
        bool perfect = Mathf.Abs(c) < 0.1f;
        var om = Mat(d.outer, 0.55f); var im = Mat(d.inner, 0.35f); var rm = Mat(Color.Lerp(d.outer, d.inner, 0.45f), 0.3f);

        GameObject a = Piece(Cap(c), pos, rot, sc, om, im, rm);
        GameObject b = Piece(Cap(-c), pos, rot * Quaternion.Euler(0, 180, 0), sc, om, im, rm);
        Physics.IgnoreCollision(a.GetComponent<Collider>(), b.GetComponent<Collider>());
        var ra = a.GetComponent<Rigidbody>(); var rb = b.GetComponent<Rigidbody>();
        ra.velocity = new Vector3(Random.Range(0.7f, 1.5f), Random.Range(1.5f, 3f), Random.Range(-0.8f, 0.8f));
        rb.velocity = new Vector3(-Random.Range(0.7f, 1.5f), Random.Range(1.5f, 3f), Random.Range(-0.8f, 0.8f));
        ra.angularVelocity = new Vector3(0, 0, -Random.Range(1f, 3.5f));
        rb.angularVelocity = new Vector3(0, 0, Random.Range(1f, 3.5f));
        AddFx(a.transform, 4f); AddFx(b.transform, 4f);

        // 과일 파편
        for (int i = 0; i < 10; i++)
        {
            var p = pos + new Vector3(c * sc.x, Random.Range(-0.3f, 0.3f) * sc.y, Random.Range(-0.3f, 0.3f) * sc.z);
            float s = Random.Range(0.07f, 0.16f);
            var g = Prim(i % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube, Vector3.one * s, i % 3 == 0 ? om : im, p);
            var r = g.AddComponent<Rigidbody>();
            r.velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(1.5f, 4.5f), Random.Range(-2f, 2f));
            r.angularVelocity = Random.insideUnitSphere * 8f;
            AddFx(g.transform, Random.Range(2.5f, 3.5f));
        }
        // 과즙 방울
        var jm = Mat(Color.Lerp(d.inner, d.outer, 0.3f), 0.9f);
        for (int i = 0; i < 20; i++)
        {
            var p = pos + new Vector3(c * sc.x, Random.Range(-0.2f, 0.4f) * sc.y, Random.Range(-0.3f, 0.3f) * sc.z);
            var g = Prim(PrimitiveType.Sphere, Vector3.one * Random.Range(0.04f, 0.09f), jm, p);
            var r = g.AddComponent<Rigidbody>();
            r.velocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(2f, 5.5f), Random.Range(-2.5f, 2.5f));
            AddFx(g.transform, Random.Range(1.0f, 1.8f));
        }

        // 소리
        sfx.pitch = knives[knifeIdx].pitch * Random.Range(0.92f, 1.1f);
        if (soundOn) sfx.PlayOneShot(sliceClips[Random.Range(0, sliceClips.Length)], 1f);
        if (perfect && soundOn) chimeSrc.PlayOneShot(chime);

        // 점수
        combo = (Time.time - lastSlice < 2f) ? combo + 1 : 1; lastSlice = Time.time;
        int pts = (perfect ? 30 : 10) * Mathf.Max(1, combo);
        score += pts;
        pops.Add(new Pop { text = (perfect ? "Perfect! +" : "+") + pts, pos = pos + Vector3.up * 1.0f, born = Time.time, col = perfect ? C(0xFFE14D) : Color.white });

        items.Remove(it); Destroy(it.t.gameObject);
    }

    GameObject Piece(Mesh m, Vector3 pos, Quaternion rot, Vector3 sc, Material om, Material im, Material rm)
    {
        var g = new GameObject("Piece");
        g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = sc;
        g.AddComponent<MeshFilter>().sharedMesh = m;
        var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { om, im, rm };
        var col = g.AddComponent<SphereCollider>(); col.radius = 0.6f;
        var rb = g.AddComponent<Rigidbody>(); rb.mass = 1f;
        return g;
    }

    void AddFx(Transform t, float life) { fxs.Add(new Fx { t = t, born = Time.time, life = life, s0 = t.localScale }); }

    // ───────── 입력 ─────────
    void Layout()
    {
        uiScale = Screen.height / 600f; vw = Screen.width / uiScale;
        knifeBtn = new Rect(vw - 330, 16, 310, 58);
        soundBtn = new Rect(vw - 330, 84, 310, 58);
    }

    void Tap(Vector2 sp)
    {
        Layout();
        var v = new Vector2(sp.x / uiScale, (Screen.height - sp.y) / uiScale);
        if (knifeBtn.Contains(v)) { knifeIdx = (knifeIdx + 1) % knives.Length; BuildKnife(); return; }
        if (soundBtn.Contains(v)) { soundOn = !soundOn; music.mute = !soundOn; return; }

        var ray = cam.ScreenPointToRay(sp);
        var plane = new Plane(Vector3.up, new Vector3(0, 0.7f, 0));
        float e; if (!plane.Raycast(ray, out e)) return;
        if (pendingCut) DoCut(knifeX);
        knifeX = ray.GetPoint(e).x; knifeT = 0; pendingCut = true; knifeGO.SetActive(true);
        if (soundOn) whooshSrc.PlayOneShot(whoosh);
    }

    void DoCut(float x)
    {
        pendingCut = false;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            if (Mathf.Abs(x - it.t.position.x) < it.d.r * it.d.shape.x * 0.97f) Slice(it, x);
        }
    }

    // ───────── 루프 ─────────
    void Update()
    {
        float dt = Time.deltaTime;
        if (Input.touchSupported && Input.touchCount > 0)
        { foreach (var t in Input.touches) if (t.phase == TouchPhase.Began) Tap(t.position); }
        else if (Input.GetMouseButtonDown(0)) Tap(Input.mousePosition);

        spawnTimer -= dt;
        if (spawnTimer <= 0) { Spawn(-7f); spawnTimer = Random.Range(1.1f, 1.9f); }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            it.t.position += Vector3.right * speed * dt;
            it.t.Rotate(Vector3.right * it.spin * dt, Space.World);
            if (it.t.position.x > 9f) { Destroy(it.t.gameObject); items.RemoveAt(i); }
        }
        foreach (var p in planks)
        {
            var q = p.position; q.x += speed * dt; if (q.x > 10.2f) q.x -= 20.4f; p.position = q;
        }

        // 칼 애니메이션
        if (knifeT >= 0)
        {
            knifeT += dt;
            const float down = 0.07f, hold = 0.12f, up = 0.25f, y0 = 2.4f, y1 = 0.05f;
            float y;
            if (knifeT < down) y = Mathf.Lerp(y0, y1, knifeT / down);
            else if (knifeT < down + hold) y = y1;
            else if (knifeT < down + hold + up) y = Mathf.Lerp(y1, y0, (knifeT - down - hold) / up);
            else { y = y0; knifeT = -1; knifeGO.SetActive(false); }
            knifeGO.transform.position = new Vector3(knifeX, y, 0);
            if (pendingCut && knifeT >= down) DoCut(knifeX);
        }
        if (Time.time - lastSlice > 2f) combo = 0;

        // 파편/조각 서서히 사라지기
        for (int i = fxs.Count - 1; i >= 0; i--)
        {
            var f = fxs[i];
            if (f.t == null) { fxs.RemoveAt(i); continue; }
            float age = Time.time - f.born;
            if (age >= f.life) { Destroy(f.t.gameObject); fxs.RemoveAt(i); }
            else if (age > f.life * 0.6f) f.t.localScale = f.s0 * (1f - (age - f.life * 0.6f) / (f.life * 0.4f));
        }
        for (int i = pops.Count - 1; i >= 0; i--) if (Time.time - pops[i].born > 1f) pops.RemoveAt(i);
    }

    // ───────── UI ─────────
    void OnGUI()
    {
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
            mid = new GUIStyle(big) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            btn = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
        }
        Layout();
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1));
        Lbl(new Rect(24, 14, 500, 56), "SCORE " + score, big, Color.white);
        if (combo > 1) Lbl(new Rect(24, 62, 500, 56), "COMBO x" + combo, big, C(0xFFE14D));
        GUI.Button(knifeBtn, "Knife: " + knives[knifeIdx].name, btn);
        GUI.Button(soundBtn, "Sound: " + (soundOn ? "ON" : "OFF"), btn);
        if (Time.timeSinceLevelLoad < 8f) Lbl(new Rect(0, 520, vw, 60), "Tap the screen to slice!", mid, Color.white);
        foreach (var p in pops)
        {
            float age = Time.time - p.born;
            var sp = cam.WorldToScreenPoint(p.pos + Vector3.up * age * 0.8f);
            var c = p.col; c.a = 1f - age;
            Lbl(new Rect(sp.x / uiScale - 200, (Screen.height - sp.y) / uiScale - 30, 400, 60), p.text, mid, c);
        }
    }

    void Lbl(Rect r, string s, GUIStyle st, Color col)
    {
        var old = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.45f * col.a); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
        GUI.color = col; GUI.Label(r, s, st);
        GUI.color = old;
    }
}
