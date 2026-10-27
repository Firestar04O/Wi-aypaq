using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SolarPathController))]
public class SolarPathControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SolarPathController c = (SolarPathController)target;
        if (c == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Arco del Sol", EditorStyles.boldLabel);

        int current = c.waypoints != null ? c.waypoints.Length : 0;
        int wanted = EditorGUILayout.IntSlider("Waypoints", Mathf.Clamp(current, 3, 4), 3, 4);
        if (wanted != current) ResizeWaypoints(c, wanted);

        if (GUILayout.Button("Arco por defecto")) ClockJamRemakeSetup.ResetSunPath();
        if (c.sun != null && GUILayout.Button("Seleccionar Sol")) Selection.activeGameObject = c.sun.gameObject;

        EditorGUILayout.HelpBox(
            "Arrastra las esferas en la Scene view para mover los waypoints (3 o 4 puntos). " +
            "La trayectoria se interpola con Catmull-Rom.", MessageType.Info);
    }

    private static void ResizeWaypoints(SolarPathController c, int count)
    {
        Undo.RecordObject(c, "Cambiar nº de waypoints");
        if (c.waypoints == null) c.waypoints = new Transform[0];
        System.Array.Resize(ref c.waypoints, count);
        EditorUtility.SetDirty(c);
    }

    private void OnSceneGUI()
    {
        SolarPathController c = (SolarPathController)target;
        if (c == null || c.waypoints == null) return;

        if (c.sun != null)
        {
            Handles.color = new Color(1f, 0.85f, 0.35f, 1f);
            Handles.DrawWireDisc(c.sun.position, Vector3.forward, 0.14f);
            Handles.Label(c.sun.position + Vector3.up * 0.18f, "Sol");
        }

        bool has = false;
        Vector3 prev = Vector3.zero;

        for (int i = 0; i < c.waypoints.Length; i++)
        {
            Transform wp = c.waypoints[i];
            if (wp == null) continue;

            EditorGUI.BeginChangeCheck();
            Vector3 pos = Handles.PositionHandle(wp.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(wp, "Mover waypoint");
                wp.position = pos;
            }

            Handles.color = new Color(1f, 0.7f, 0.2f, 1f);
            Handles.SphereHandleCap(0, wp.position, Quaternion.identity, 0.09f, EventType.Repaint);
            Handles.Label(wp.position, "W" + (i + 1));

            if (has) Handles.DrawDottedLine(prev, wp.position, 3f);
            prev = wp.position;
            has = true;
        }
    }
}

[CustomEditor(typeof(ShadowSweepController))]
public class ShadowSweepControllerEditor : Editor
{
    private float _preview = 0.5f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ShadowSweepController c = (ShadowSweepController)target;
        if (c == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Barrido de sombra", EditorStyles.boldLabel);
        _preview = EditorGUILayout.Slider("Vista previa t", _preview, 0f, 1f);

        if (GUILayout.Button("Capturar punta"))
        {
            Undo.RecordObject(c, "Capturar punta");
            c.CapturarPunta();
            c.Refresh();
            EditorUtility.SetDirty(c);
        }

        if (GUILayout.Button("Referencias por defecto")) ClockJamRemakeSetup.ResetShadowRefs();
        if (c.posicionOriginal != null && GUILayout.Button("Seleccionar Referencia 0"))
            Selection.activeGameObject = c.posicionOriginal.gameObject;
        if (c.sombra != null && GUILayout.Button("Seleccionar Sombra"))
            Selection.activeGameObject = c.sombra.gameObject;

        EditorGUILayout.HelpBox(
            "1) Arrastra \"Referencia 0\" hasta la punta de la sombra y pulsa \"Capturar punta\".\n" +
            "2) Coloca Sombra R1..R9 a la misma distancia del centro que la punta: la sombra gira, no se estira.\n" +
            "3) Las horas solo controlan el avance, no el lugar.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        ShadowSweepController c = (ShadowSweepController)target;
        if (c == null) return;

        Vector3 origin = c.Origin();

        if (c.sombra != null)
        {
            Handles.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            Handles.DrawWireDisc(c.sombra.position, Vector3.forward, 0.1f);
            Handles.Label(c.sombra.position + Vector3.up * 0.14f, "Centro rect");
        }

        if (c.posicionOriginal != null)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 pos = Handles.PositionHandle(c.posicionOriginal.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c.posicionOriginal, "Mover Referencia 0");
                c.posicionOriginal.position = pos;
            }

            Handles.color = new Color(1f, 0.85f, 0.2f, 1f);
            Handles.SphereHandleCap(0, c.posicionOriginal.position, Quaternion.identity, 0.11f, EventType.Repaint);
            Handles.Label(c.posicionOriginal.position + Vector3.up * 0.13f, "Referencia 0 (punta)");
            Handles.DrawDottedLine(origin, c.posicionOriginal.position, 3f);
        }

        Transform[] refs = c.referencias;
        if (refs == null) return;

        bool has = false;
        Vector3 prev = Vector3.zero;

        for (int i = 0; i < refs.Length; i++)
        {
            Transform r = refs[i];
            if (r == null) continue;

            EditorGUI.BeginChangeCheck();
            Vector3 pos = Handles.PositionHandle(r.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(r, "Mover referencia");
                r.position = pos;
            }

            Handles.color = new Color(0.3f, 1f, 0.6f, 1f);
            Handles.SphereHandleCap(0, r.position, Quaternion.identity, 0.07f, EventType.Repaint);
            Handles.Label(r.position, "R" + (i + 1));

            if (has) Handles.DrawDottedLine(prev, r.position, 3f);
            prev = r.position;
            has = true;
        }

        if (has && refs.Length > 0)
        {
            Vector3 target = c.TargetAt(_preview);
            Handles.color = new Color(1f, 1f, 1f, 0.8f);
            Handles.DrawDottedLine(origin, target, 4f);
            Handles.DrawWireDisc(target, Vector3.forward, 0.06f);
            Handles.Label(target, "destino t=" + _preview.ToString("F2"));
        }
    }
}

[CustomEditor(typeof(ClockJamPiece))]
public class ClockJamPieceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ClockJamPiece piece = (ClockJamPiece)target;
        if (piece == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Origen de la pieza", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Posición actual", piece.transform.position.ToString("F2"));
        EditorGUILayout.LabelField("Escala actual", piece.transform.localScale.ToString("F2"));
        EditorGUILayout.LabelField("Rotación actual", piece.transform.rotation.eulerAngles.ToString("F1"));

        Transform origen = piece.origen != null
            ? piece.origen
            : ClockJamRemakeSetup.FindOrigen(piece, ClockJamRemakeSetup.IndexOf(piece.name) - 1);

        if (origen == null)
        {
            EditorGUILayout.HelpBox("Sin origen asignado. Usa el reparto automático o asígnalo a mano.", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Seleccionar origen")) Selection.activeGameObject = origen.gameObject;

        if (GUILayout.Button("Fijar origen en la posición/escala/rotación actual"))
        {
            Undo.RecordObject(origen, "Fijar origen");
            origen.SetPositionAndRotation(piece.transform.position, piece.transform.rotation);
            origen.localScale = piece.transform.localScale;
            piece.origen = origen;
            piece.SnapToOrigen();
            EditorUtility.SetDirty(piece);
        }

        if (GUILayout.Button("Resetear pieza"))
        {
            Undo.RecordObject(piece.transform, "Resetear pieza");
            piece.ResetPiece();
        }

        EditorGUILayout.HelpBox(
            "Al agarrar la pieza vuelve a escala 1 y rotación 0 para alinearse con el mural. " +
            "Si la sueltas lejos del centro regresa sola a su origen.", MessageType.Info);
    }
}
