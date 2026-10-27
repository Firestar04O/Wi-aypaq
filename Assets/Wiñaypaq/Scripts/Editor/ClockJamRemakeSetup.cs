using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ClockJamRemakeSetup
{
    private const float RingRadiusX = 1.75f;
    private const float RingRadiusY = 0.95f;
    private const float BaseScale = 0.55f;
    private const float ScaleStep = 0.05f;

    public static void ApplyLayerOrders()
    {
        Transform lienzo = GameObject.Find("Lienzo") != null ? GameObject.Find("Lienzo").transform : null;
        if (lienzo == null)
        {
            Debug.LogWarning("No se encontró el objeto \"Lienzo\" en la escena abierta.");
            return;
        }

        int count = 0;
        foreach (Transform child in lienzo)
        {
            int order = OrderFor(child.name);
            if (child.name == "Piezas" || child.name == "Costuras")
            {
                foreach (Transform sub in child)
                {
                    SpriteRenderer sr = sub.GetComponent<SpriteRenderer>();
                    if (sr == null) continue;
                    Undo.RecordObject(sr, "Orden de capa");
                    sr.sortingOrder = order;
                    count++;
                }
                continue;
            }

            SpriteRenderer self = child.GetComponent<SpriteRenderer>();
            if (self == null) continue;
            Undo.RecordObject(self, "Orden de capa");
            self.sortingOrder = order;
            count++;
        }

        EditorUtility.SetDirty(lienzo.gameObject);
        Debug.Log("Órdenes de capa aplicados a " + count + " sprite(s).");
    }

    private static int OrderFor(string name)
    {
        if (name.StartsWith("Cielo")) return ClockJamScenarioBuilder.OrderCielo;
        if (name.StartsWith("Sol")) return ClockJamScenarioBuilder.OrderSol;
        if (name.StartsWith("Fondo")) return ClockJamScenarioBuilder.OrderFondo;
        if (name.StartsWith("Marco")) return ClockJamScenarioBuilder.OrderMarco;
        if (name.StartsWith("Figura")) return ClockJamScenarioBuilder.OrderFigura;
        if (name.StartsWith("Piezas")) return ClockJamScenarioBuilder.OrderPieza;
        if (name.StartsWith("Costuras")) return ClockJamScenarioBuilder.OrderCostura;
        if (name.StartsWith("Hora")) return ClockJamScenarioBuilder.OrderHoraYRango;
        if (name.StartsWith("Sombra")) return ClockJamScenarioBuilder.OrderSombra;
        return 0;
    }

    public static void ScatterRing() => Scatter(0);

    public static void ScatterGrid() => Scatter(1);

    public static void ScatterRandom() => Scatter(2);

    public static void ResetSunPath()
    {
        SolarPathController[] controllers = UnityEngine.Object.FindObjectsByType<SolarPathController>(FindObjectsSortMode.None);
        foreach (SolarPathController c in controllers)
        {
            if (c == null || c.waypoints == null || c.waypoints.Length < 3) continue;
            Undo.RecordObject(c, "Arco de Sol por defecto");
            int count = Mathf.Clamp(c.waypoints.Length, 3, 4);
            System.Array.Resize(ref c.waypoints, count);
            for (int i = 0; i < count; i++)
            {
                float u = count > 1 ? i / (float)(count - 1) : 0f;
                if (c.waypoints[i] == null) continue;
                Undo.RecordObject(c.waypoints[i], "Arco de Sol por defecto");
                c.waypoints[i].position = DefaultSunPoint(u);
                c.waypoints[i].rotation = Quaternion.identity;
            }
            EditorUtility.SetDirty(c);
        }
        SceneView.RepaintAll();
    }

    public static void ResetShadowRefs()
    {
        ShadowSweepController[] controllers = UnityEngine.Object.FindObjectsByType<ShadowSweepController>(FindObjectsSortMode.None);
        foreach (ShadowSweepController c in controllers)
        {
            if (c == null || c.referencias == null || c.referencias.Length == 0) continue;
            Undo.RecordObject(c, "Referencias de sombra por defecto");
            for (int i = 0; i < c.referencias.Length; i++)
            {
                if (c.referencias[i] == null) continue;
                Undo.RecordObject(c.referencias[i], "Referencias de sombra por defecto");
                float u = c.referencias.Length > 1 ? i / (float)(c.referencias.Length - 1) : 0f;
                c.referencias[i].position = new Vector3(Mathf.Lerp(-1.85f, 1.85f, u),
                    -1.02f + 0.15f * Mathf.Sin(u * Mathf.PI), 0f);
            }
            c.Refresh();
            EditorUtility.SetDirty(c);
        }
        SceneView.RepaintAll();
    }

    public static Vector3 DefaultSunPoint(float u)
    {
        float x = Mathf.Lerp(-1.9f, 1.8f, u);
        float y = Mathf.Lerp(-0.6f, -0.55f, u) + 1.05f * Mathf.Sin(u * Mathf.PI);
        return new Vector3(x, y, 0f);
    }

    private static void Scatter(int mode)
    {
        List<ClockJamPiece> pieces = FindSortedPieces();
        if (pieces.Count == 0)
        {
            Debug.LogWarning("No se encontraron piezas (ClockJamPiece) en la escena abierta.");
            return;
        }

        int n = pieces.Count;
        List<Vector3> placed = new List<Vector3>();

        for (int i = 0; i < n; i++)
        {
            Transform origen = FindOrigen(pieces[i], i);
            if (origen == null) continue;

            Undo.RecordObject(origen, "Repartir orígenes");
            float step = 360f / n;
            float rad = (-90f + i * step) * Mathf.Deg2Rad;

            Vector3 pos;
            switch (mode)
            {
                case 1:
                    int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
                    int rows = Mathf.CeilToInt(n / (float)cols);
                    int row = i / cols;
                    int col = i % cols;
                    pos = new Vector3(
                        Mathf.Lerp(-1.75f, 1.75f, cols > 1 ? col / (float)(cols - 1) : 0.5f),
                        Mathf.Lerp(0.85f, -0.85f, rows > 1 ? row / (float)(rows - 1) : 0.5f),
                        0f);
                    break;
                case 2:
                    pos = RandomScatterPoint(placed);
                    break;
                default:
                    pos = new Vector3(Mathf.Cos(rad) * RingRadiusX, Mathf.Sin(rad) * RingRadiusY, 0f);
                    break;
            }

            placed.Add(pos);
            origen.position = pos;
            origen.rotation = Quaternion.Euler(0f, 0f, 90f * (i % 4));
            origen.localScale = Vector3.one * (BaseScale + ScaleStep * (i % 3));
            pieces[i].SnapToOrigen();
        }

        SceneView.RepaintAll();
        Debug.Log("Orígenes repartidos en " + pieces.Count + " pieza(s). Guarda la escena para conservarlo.");
    }

    private static Vector3 RandomScatterPoint(List<Vector3> placed)
    {
        const float minSeparation = 0.55f;
        float minSq = minSeparation * minSeparation;
        Vector3 candidate = Vector3.zero;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            candidate = new Vector3(Random.Range(-1.9f, 1.9f), Random.Range(-1.05f, 1.05f), 0f);
            bool tooClose = false;
            foreach (Vector3 other in placed)
            {
                if ((candidate - other).sqrMagnitude >= minSq) continue;
                tooClose = true;
                break;
            }
            if (!tooClose) break;
        }

        return candidate;
    }

    public static List<ClockJamPiece> FindSortedPieces()
    {
        ClockJamPiece[] found = UnityEngine.Object.FindObjectsByType<ClockJamPiece>(FindObjectsSortMode.None);
        List<ClockJamPiece> list = new List<ClockJamPiece>();
        foreach (ClockJamPiece p in found)
            if (p != null) list.Add(p);
        list.Sort((a, b) => IndexOf(a.name).CompareTo(IndexOf(b.name)));
        return list;
    }

    public static Transform FindOrigen(ClockJamPiece piece, int index)
    {
        if (piece == null) return null;
        if (piece.origen != null) return piece.origen;

        Transform parent = piece.transform.parent;
        if (parent == null) return null;

        string wanted = "Origen " + IndexOf(piece.name);
        Transform[] siblings = parent.GetComponentsInChildren<Transform>(true);
        Transform exact = null;
        List<Transform> fallback = new List<Transform>();

        foreach (Transform t in siblings)
        {
            if (t == null || t == piece.transform) continue;
            if (!t.name.StartsWith("Origen")) continue;
            if (t.name == wanted) exact = t;
            fallback.Add(t);
        }

        if (exact != null) return exact;
        if (fallback.Count == 0) return null;
        return fallback[Mathf.Clamp(index, 0, fallback.Count - 1)];
    }

    public static int IndexOf(string name)
    {
        int value = 0;
        bool found = false;
        for (int i = name.Length - 1; i >= 0; i--)
        {
            if (char.IsDigit(name[i]))
            {
                value = value * 10 + (name[i] - '0');
                found = true;
            }
            else if (found)
            {
                break;
            }
        }
        return value;
    }
}
