using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

public static class ClockJamScenarioBuilder
{
    public const int PieceCount = 11;
    public const int ShadowRefCount = 9;
    public const int SunWaypointCount = 4;

    private const string ArtRoot = "Assets/Wiñaypaq/Art/Clock/";
    private const string BackgroundDir = ArtRoot + "Backgrounds/";
    private const string ElementDir = ArtRoot + "Elements/";
    private const string PieceDir = ArtRoot + "Pieces/";
    private const string SeamDir = ArtRoot + "Seams/";
    private const string LayoutPath = ArtRoot + "clock-layout.json";
    private const string PiecePrefabPath = "Assets/Wiñaypaq/Prefabs/ClockPiece.prefab";
    private const string RealScenePath = "Assets/Wiñaypaq/Scenes/ClockGameplay.unity";

    private const float PPU = 100f;
    public const float CanvasWidth = 489f / PPU;
    public const float CanvasHeight = 275f / PPU;
    public const float SnapRadius = 1.15f;

    private const float AlphaThreshold = 0.5f;
    private const int MaxColliderPoints = 48;
    private const float MinColliderPointDistance = 0.008f;

    public const int OrderCielo = -30;
    public const int OrderSol = -29;
    public const int OrderFondo = -28;
    public const int OrderMarco = -20;
    public const int OrderFigura = -10;
    public const int OrderPieza = 0;
    public const int OrderCostura = 5;
    public const int OrderHoraYRango = 8;
    public const int OrderSombra = 10;
    private static readonly List<string> _temporarilyReadable = new List<string>();
    private static Dictionary<string, Vector2> _artOffsets;

    [Serializable]
    private sealed class LayoutManifest
    {
        public List<LayoutEntry> sprites = new List<LayoutEntry>();
    }

    [Serializable]
    private sealed class LayoutEntry
    {
        public string assetName;
        public float offsetX;
        public float offsetY;
    }

    private static readonly string[] SeamFiles =
    {
        "1-2", "1-3", "1-7", "1-8", "1-10", "1-11", "2-3", "3-4", "3-10", "4-5",
        "4-8", "4-9", "4-10", "5-6", "5-8", "6-7", "7-8", "7-11", "8-9", "8-10", "8-11"
    };

    private static readonly Vector2[] DefaultSunPath =
    {
        new Vector2(-1.90f, -0.60f),
        new Vector2(-0.80f, 0.30f),
        new Vector2(0.55f, 0.45f),
        new Vector2(1.80f, -0.55f)
    };

    public static void BuildRemakeScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        _artOffsets = null;
        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        BuildGlobalLight();
        BuildEventSystem();

        GameObject lienzoGo = new GameObject("Lienzo");
        Transform lienzo = lienzoGo.transform;
        lienzo.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        lienzo.localScale = Vector3.one;

        MakeLayer(lienzo, "Cielo", BackgroundDir + "sky.png", OrderCielo);
        Transform sol = MakePivotedLayer(lienzo, "Sol", ElementDir + "sun.png", OrderSol);
        MakeLayer(lienzo, "Fondo", BackgroundDir + "background.png", OrderFondo);
        MakeLayer(lienzo, "Marco", BackgroundDir + "frame.png", OrderMarco);
        SpriteRenderer figura = MakeLayer(lienzo, "Figura Completa", ElementDir + "figure-complete.png", OrderFigura);

        Transform piezasRoot = NewParent("Piezas", Vector3.zero, lienzo);
        Transform targetsRoot = NewParent("Targets", Vector3.zero, lienzo);
        Transform costurasRoot = NewParent("Costuras", Vector3.zero, lienzo);
        MakeLayer(lienzo, "Hora y Rango", BackgroundDir + "hour-range.png", OrderHoraYRango);
        Transform sombra = MakePivotedLayer(lienzo, "Sombra", ElementDir + "shadow.png", OrderSombra);

        Transform sistema = NewParent("Sistema", Vector3.zero);
        ClockJamSolarClock clock = NewComponent<ClockJamSolarClock>(sistema, "SolarClock");

        GameObject pmGo = new GameObject("PuzzleManager");
        pmGo.transform.SetParent(sistema, false);
        ClockJamPuzzleManager pm = pmGo.AddComponent<ClockJamPuzzleManager>();

        Transform[] origenes = BuildOrigens(piezasRoot);
        Transform[] targets = BuildTargets(targetsRoot);
        ClockJamPiece[] pieces = BuildPieces(piezasRoot, pm, targets, origenes);
        BuildSeams(costurasRoot, pm, pieces);

        Transform sunWaypointsHost = NewParent("Waypoints Sol", Vector3.zero);
        Transform[] sunWaypoints = BuildSunWaypoints(sunWaypointsHost);
        Transform shadowRefsHost = NewParent("Referencias Sombra", Vector3.zero);
        Transform sombraOrigen = NewParent("Sombra Posicion Original", new Vector3(0.9f, -0.45f, 0f), shadowRefsHost);
        Transform[] shadowRefs = BuildShadowRefs(shadowRefsHost);

        GameObject sunCtlGo = new GameObject("Control Sol");
        sunCtlGo.transform.SetParent(sistema, false);
        SolarPathController sunCtl = sunCtlGo.AddComponent<SolarPathController>();
        sunCtl.clock = clock;
        sunCtl.sun = sol;
        sunCtl.waypoints = sunWaypoints;
        sunCtl.curvaTiempo = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        sunCtl.Refresh();

        GameObject shadowCtlGo = new GameObject("Control Sombra");
        shadowCtlGo.transform.SetParent(sistema, false);
        ShadowSweepController shadowCtl = shadowCtlGo.AddComponent<ShadowSweepController>();
        shadowCtl.clock = clock;
        shadowCtl.sombra = sombra;
        shadowCtl.posicionOriginal = sombraOrigen;
        shadowCtl.referencias = shadowRefs;
        shadowCtl.curvaBarrido = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        shadowCtl.CapturarPunta();
        shadowCtl.Refresh();

        GameObject wrathGo = new GameObject("WrathController");
        wrathGo.transform.SetParent(sistema, false);
        ClockJamWrathController wrath = wrathGo.AddComponent<ClockJamWrathController>();
        wrath.puzzleManager = pm;
        wrath.solarClock = clock;
        wrath.warningDuration = 3.5f;
        wrath.respawnTime = 15f;

        GameObject levelGo = new GameObject("LevelController");
        levelGo.transform.SetParent(sistema, false);
        ClockJamLevelController level = levelGo.AddComponent<ClockJamLevelController>();
        level.clock = clock;
        level.wrath = wrath;
        level.puzzleManager = pm;
        level.figura = figura.gameObject;
        level.piezasRoot = piezasRoot.gameObject;
        level.costurasRoot = costurasRoot.gameObject;

        figura.gameObject.SetActive(false);
        RestoreImporterState();

        EditorSceneManager.SaveScene(scene, RealScenePath);
        SetGameplayAsBuildScene(RealScenePath);
        Debug.Log("Escenario Remake generado en: " + RealScenePath);
    }

    public static void RebuildPieceColliders()
    {
        ClockJamPiece[] pieces = UnityEngine.Object.FindObjectsByType<ClockJamPiece>(FindObjectsSortMode.None);
        if (pieces == null || pieces.Length == 0)
        {
            Debug.LogWarning("No hay piezas en la escena abierta.");
            return;
        }

        List<string> touched = new List<string>();
        int updated = 0;

        foreach (ClockJamPiece piece in pieces)
        {
            SpriteRenderer sr = piece != null ? piece.GetComponent<SpriteRenderer>() : null;
            if (sr == null || sr.sprite == null) continue;

            string path = AssetDatabase.GetAssetPath(sr.sprite);
            if (string.IsNullOrEmpty(path)) continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                touched.Add(path);
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            Vector2[][] paths = AlphaOutline(sr.sprite);
            if (paths == null)
            {
                Debug.LogWarning("Sin contorno de alfa para: " + path);
                continue;
            }

            PolygonCollider2D poly = piece.GetComponent<PolygonCollider2D>();
            if (poly == null) poly = piece.gameObject.AddComponent<PolygonCollider2D>();
            SetColliderPaths(poly, paths);
            updated++;
        }

        foreach (string path in touched)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !importer.isReadable) continue;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        if (updated > 0) EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("Colliders de alfa recalculados en " + updated + " pieza(s).");
    }

    private static Transform[] BuildOrigens(Transform piezasRoot)
    {
        Transform[] origenes = new Transform[PieceCount];
        for (int i = 0; i < PieceCount; i++)
        {
            GameObject go = new GameObject("Origen " + (i + 1));
            go.transform.SetParent(piezasRoot, false);
            go.transform.SetPositionAndRotation(DefaultOrigenPosition(i), DefaultOrigenRotation(i));
            go.transform.localScale = DefaultOrigenScale(i);
            origenes[i] = go.transform;
        }
        return origenes;
    }

    private static Transform[] BuildTargets(Transform targetsRoot)
    {
        Transform[] targets = new Transform[PieceCount];
        for (int i = 0; i < PieceCount; i++)
        {
            string assetPath = PiecePath(i + 1);
            GameObject target = new GameObject("Target " + (i + 1));
            target.transform.SetParent(targetsRoot, false);
            target.transform.localPosition = GetArtOffset(assetPath);
            targets[i] = target.transform;
        }
        return targets;
    }

    private static Vector3 DefaultOrigenPosition(int i)
    {
        float step = 360f / PieceCount;
        float rad = (-90f + i * step) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * 1.75f, Mathf.Sin(rad) * 0.95f, 0f);
    }

    private static Quaternion DefaultOrigenRotation(int i) => Quaternion.Euler(0f, 0f, 90f * (i % 4));

    private static Vector3 DefaultOrigenScale(int i) => Vector3.one * (0.55f + 0.05f * (i % 3));

    private static ClockJamPiece[] BuildPieces(Transform piezasRoot, ClockJamPuzzleManager pm,
        Transform[] targets, Transform[] origenes)
    {
        ClockJamPiece[] pieces = new ClockJamPiece[PieceCount];
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PiecePrefabPath);
        if (prefab == null)
            throw new InvalidOperationException("No se encontró el prefab base: " + PiecePrefabPath);

        for (int i = 0; i < PieceCount; i++)
        {
            string assetPath = PiecePath(i + 1);
            Sprite sprite = LoadSprite(assetPath, true);

            GameObject go = PrefabUtility.InstantiatePrefab(prefab, piezasRoot) as GameObject;
            if (go == null) throw new InvalidOperationException("No se pudo instanciar " + PiecePrefabPath);
            go.name = "Pieza " + (i + 1);
            go.transform.SetPositionAndRotation(origenes[i].position, origenes[i].rotation);
            go.transform.localScale = origenes[i].localScale;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = OrderPieza;

            PolygonCollider2D poly = go.GetComponent<PolygonCollider2D>();
            Vector2[][] paths = AlphaOutline(sprite);
            if (paths != null)
            {
                poly.enabled = true;
                SetColliderPaths(poly, paths);
            }
            else
            {
                poly.enabled = false;
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = sprite != null ? sprite.bounds.size * 0.8f : new Vector2(1f, 1f);
                Debug.LogWarning("Pieza " + (i + 1) + ": sin contorno de alfa, se usó collider de caja.");
            }

            ClockJamPiece piece = go.GetComponent<ClockJamPiece>();
            piece.puzzleManager = pm;
            piece.snapTarget = targets[i];
            piece.origen = origenes[i];
            piece.snapRadius = SnapRadius;

            pieces[i] = piece;
        }
        return pieces;
    }

    private static void BuildSeams(Transform costurasRoot, ClockJamPuzzleManager pm, ClockJamPiece[] pieces)
    {
        List<TapestrySeam> seams = new List<TapestrySeam>();

        foreach (string file in SeamFiles)
        {
            string[] parts = file.Split('-');
            if (parts.Length != 2) continue;

            int a, b;
            if (!int.TryParse(parts[0], out a) || !int.TryParse(parts[1], out b)) continue;
            if (a < 1 || b < 1 || a > PieceCount || b > PieceCount) continue;

            string assetPath = SeamPath(a, b);
            Sprite sprite = LoadSprite(assetPath, false);
            if (sprite == null) continue;

            GameObject seam = new GameObject("Costura " + file);
            seam.transform.SetParent(costurasRoot, false);
            seam.transform.localPosition = GetArtOffset(assetPath);
            seam.SetActive(false);

            SpriteRenderer sr = seam.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = OrderCostura;

            seams.Add(new TapestrySeam
            {
                pieceA = pieces[a - 1],
                pieceB = pieces[b - 1],
                seamObject = seam
            });
        }

        pm.seams = seams.ToArray();
    }

    private static Transform[] BuildSunWaypoints(Transform host)
    {
        Transform[] waypoints = new Transform[SunWaypointCount];
        for (int i = 0; i < SunWaypointCount; i++)
        {
            Vector2 p = i < DefaultSunPath.Length ? DefaultSunPath[i] : Vector2.zero;
            GameObject go = new GameObject("Sol W" + (i + 1));
            go.transform.SetParent(host, false);
            go.transform.localPosition = p;
            waypoints[i] = go.transform;
        }
        return waypoints;
    }

    private static Transform[] BuildShadowRefs(Transform host)
    {
        Transform[] refs = new Transform[ShadowRefCount];
        for (int i = 0; i < ShadowRefCount; i++)
        {
            float u = i / (float)(ShadowRefCount - 1);
            float x = Mathf.Lerp(-1.85f, 1.85f, u);
            float y = -1.02f + 0.15f * Mathf.Sin(u * Mathf.PI);

            GameObject go = new GameObject("Sombra R" + (i + 1));
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            refs[i] = go.transform;
        }
        return refs;
    }

    private static void BuildCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CanvasHeight * 0.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        go.AddComponent<Physics2DRaycaster>();
    }

    private static void BuildGlobalLight()
    {
        GameObject go = new GameObject("Global Light 2D");
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static void BuildEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static SpriteRenderer MakeLayer(Transform parent, string name, string assetPath, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = GetArtOffset(assetPath);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(assetPath, false);
        sr.sortingOrder = order;
        return sr;
    }

    private static Transform MakePivotedLayer(Transform parent, string name, string assetPath, int order)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(pivot.transform, false);
        visual.transform.localPosition = GetArtOffset(assetPath);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(assetPath, false);
        renderer.sortingOrder = order;
        return pivot.transform;
    }

    private static Transform NewParent(string name, Vector3 pos, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    private static T NewComponent<T>(Transform parent, string name) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static Sprite LoadSprite(string assetPath, bool needsReadable)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, PPU))
            {
                importer.spritePixelsPerUnit = PPU;
                dirty = true;
            }
            if (needsReadable && !importer.isReadable)
            {
                importer.isReadable = true;
                _temporarilyReadable.Add(assetPath);
                dirty = true;
            }
            if (dirty) importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null) Debug.LogWarning("No se pudo cargar el sprite: " + assetPath);
        return sprite;
    }

    private static string PiecePath(int number) => PieceDir + "piece-" + number.ToString("00") + ".png";

    private static string SeamPath(int a, int b) =>
        SeamDir + "seam-" + a.ToString("00") + "-" + b.ToString("00") + ".png";

    private static Vector3 GetArtOffset(string assetPath)
    {
        EnsureLayoutLoaded();
        string fileName = Path.GetFileName(assetPath);
        return _artOffsets.TryGetValue(fileName, out Vector2 offset)
            ? new Vector3(offset.x, offset.y, 0f)
            : Vector3.zero;
    }

    private static void EnsureLayoutLoaded()
    {
        if (_artOffsets != null) return;
        _artOffsets = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);

        TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
        if (text == null)
            throw new InvalidOperationException("No se encontró el manifiesto de arte: " + LayoutPath);

        LayoutManifest manifest = JsonUtility.FromJson<LayoutManifest>(text.text);
        if (manifest == null || manifest.sprites == null)
            throw new InvalidOperationException("El manifiesto de arte no es válido: " + LayoutPath);

        foreach (LayoutEntry entry in manifest.sprites)
            _artOffsets[entry.assetName] = new Vector2(entry.offsetX, entry.offsetY);
    }

    private static void RestoreImporterState()
    {
        foreach (string path in _temporarilyReadable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !importer.isReadable) continue;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
        _temporarilyReadable.Clear();
    }

    private static void SetColliderPaths(PolygonCollider2D poly, Vector2[][] paths)
    {
        poly.pathCount = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            poly.SetPath(i, paths[i]);
    }

    private static Vector2[][] AlphaOutline(Sprite sprite)
    {
        if (sprite == null) return null;
        Texture2D tex = sprite.texture;
        if (tex == null) return null;

        Color32[] pixels;
        try
        {
            pixels = tex.GetPixels32();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo leer la textura de " + sprite.name + ": " + e.Message);
            return null;
        }

        byte threshold = (byte)Mathf.Clamp(Mathf.RoundToInt(AlphaThreshold * 255f), 1, 255);
        List<Vector2> contour = TraceContour(pixels, tex.width, tex.height, threshold, sprite.pixelsPerUnit, sprite.pivot);
        if (contour == null) return null;

        List<Vector2> simplified = SimplifyRdp(contour, MinColliderPointDistance);
        if (simplified == null) return null;
        simplified = CapPoints(simplified, MaxColliderPoints);
        if (simplified == null || simplified.Count < 3) return null;

        return new[] { simplified.ToArray() };
    }

    private static readonly Vector2Int[] MooreNeighborhood =
    {
        new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
        new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };

    private static List<Vector2> TraceContour(Color32[] pixels, int width, int height, byte threshold, float ppu, Vector2 pivot)
    {
        int startX = -1, startY = -1;
        for (int y = height - 1; y >= 0 && startX < 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a >= threshold)
                {
                    startX = x;
                    startY = y;
                    break;
                }
            }
        }
        if (startX < 0) return null;

        float scale = 1f / (Mathf.Approximately(ppu, 0f) ? 100f : ppu);
        List<Vector2> contour = new List<Vector2>();

        int cx = startX, cy = startY;
        int backX = startX - 1, backY = startY;
        int maxSteps = width * height;

        while (contour.Count <= maxSteps)
        {
            contour.Add(new Vector2((cx + 0.5f - pivot.x) * scale, (cy + 0.5f - pivot.y) * scale));

            int backIndex = NeighborIndex(backX - cx, backY - cy);
            bool moved = false;

            for (int k = 1; k <= 8; k++)
            {
                Vector2Int n = MooreNeighborhood[(backIndex + k) % 8];
                int nx = cx + n.x, ny = cy + n.y;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (pixels[ny * width + nx].a < threshold) continue;

                Vector2Int prev = MooreNeighborhood[((backIndex + k) + 7) % 8];
                backX = cx + prev.x;
                backY = cy + prev.y;
                cx = nx;
                cy = ny;
                moved = true;
                break;
            }

            if (!moved) break;
            if (cx == startX && cy == startY) break;
        }

        return contour.Count >= 3 ? contour : null;
    }

    private static int NeighborIndex(int dx, int dy)
    {
        for (int i = 0; i < MooreNeighborhood.Length; i++)
            if (MooreNeighborhood[i].x == dx && MooreNeighborhood[i].y == dy) return i;
        return 0;
    }

    private static List<Vector2> SimplifyRdp(List<Vector2> points, float epsilon)
    {
        if (points == null || points.Count < 3) return null;
        bool[] keep = new bool[points.Count];
        keep[0] = true;
        keep[points.Count - 1] = true;
        Rdp(points, 0, points.Count - 1, epsilon * epsilon, keep);

        List<Vector2> result = new List<Vector2>();
        for (int i = 0; i < points.Count; i++)
            if (keep[i]) result.Add(points[i]);
        return result;
    }

    private static void Rdp(List<Vector2> points, int first, int last, float epsSq, bool[] keep)
    {
        if (last <= first + 1) return;

        Vector2 a = points[first];
        Vector2 ab = points[last] - a;
        float lenSq = ab.sqrMagnitude;
        float maxDist = -1f;
        int index = -1;

        for (int i = first + 1; i < last; i++)
        {
            float d;
            if (lenSq < 1e-12f)
            {
                d = (points[i] - a).sqrMagnitude;
            }
            else
            {
                float t = Mathf.Clamp01(Vector2.Dot(points[i] - a, ab) / lenSq);
                d = (points[i] - (a + ab * t)).sqrMagnitude;
            }
            if (d > maxDist)
            {
                maxDist = d;
                index = i;
            }
        }

        if (maxDist > epsSq && index > 0)
        {
            keep[index] = true;
            Rdp(points, first, index, epsSq, keep);
            Rdp(points, index, last, epsSq, keep);
        }
    }

    private static List<Vector2> CapPoints(List<Vector2> points, int maxPoints)
    {
        if (points == null) return null;
        if (points.Count <= maxPoints) return points;

        List<Vector2> reduced = new List<Vector2>();
        float stride = (float)points.Count / maxPoints;
        for (int i = 0; i < maxPoints; i++)
            reduced.Add(points[Mathf.Clamp(Mathf.RoundToInt(i * stride), 0, points.Count - 1)]);
        return reduced;
    }

    private static void SetGameplayAsBuildScene(string path)
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
    }
}
