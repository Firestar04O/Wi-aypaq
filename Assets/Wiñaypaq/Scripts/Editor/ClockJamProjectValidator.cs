using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClockJamProjectValidator
{
    private const string ScenePath = "Assets/Wiñaypaq/Scenes/ClockGameplay.unity";
    private const string LayoutPath = "Assets/Wiñaypaq/Art/Clock/clock-layout.json";
    private const string PiecePrefabPath = "Assets/Wiñaypaq/Prefabs/ClockPiece.prefab";
    private const string AtlasPath = "Assets/Wiñaypaq/Art/Clock/clock-gameplay.spriteatlas";
    private const string MaterialPath = "Assets/Wiñaypaq/Art/Effects/clock-particles.mat";
    private const int CropPadding = 2;

    [Serializable]
    private sealed class LayoutManifest
    {
        public List<LayoutEntry> sprites = new List<LayoutEntry>();
    }

    [Serializable]
    private sealed class LayoutEntry
    {
        public string assetName;
        public int originalWidth;
        public int originalHeight;
        public int sourceX;
        public int sourceY;
        public int sourceWidth;
        public int sourceHeight;
        public float offsetX;
        public float offsetY;
    }

    [MenuItem("Tools/Clock Jam/Validate Migrated Project")]
    public static void Validate()
    {
        List<string> errors = new List<string>();
        List<string> checks = new List<string>();

        ValidateBuildSettings(errors, checks);
        ValidateRequiredAssets(errors, checks);
        ValidateCropFidelity(errors, checks);
        ValidateScene(errors, checks);

        string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectBackups", "validation-report.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        List<string> report = new List<string>
        {
            "Wiñaypaq project validation",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            string.Empty
        };
        report.AddRange(checks.Select(check => "OK  " + check));
        report.AddRange(errors.Select(error => "ERR " + error));
        File.WriteAllLines(reportPath, report);

        if (errors.Count > 0)
            throw new BuildFailedException("La validación falló:\n- " + string.Join("\n- ", errors));

        Debug.Log("Validación completada: " + checks.Count + " comprobaciones correctas. Reporte: " + reportPath);
    }

    public static void ValidateFromCommandLine()
    {
        Validate();
    }

    private static void ValidateBuildSettings(List<string> errors, List<string> checks)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length != 1 || !scenes[0].enabled || scenes[0].path != ScenePath)
            errors.Add("Build Settings debe contener únicamente ClockGameplay como escena habilitada.");
        else
            checks.Add("ClockGameplay es la única escena de build y ocupa el índice 0.");
    }

    private static void ValidateRequiredAssets(List<string> errors, List<string> checks)
    {
        string[] required = { ScenePath, LayoutPath, PiecePrefabPath, AtlasPath, MaterialPath };
        foreach (string path in required)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                errors.Add("Falta el asset requerido: " + path);
            else
                checks.Add("Asset presente: " + path);
        }
    }

    private static void ValidateCropFidelity(List<string> errors, List<string> checks)
    {
        TextAsset layoutText = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
        if (layoutText == null) return;

        LayoutManifest manifest = JsonUtility.FromJson<LayoutManifest>(layoutText.text);
        if (manifest == null || manifest.sprites == null || manifest.sprites.Count != 35)
        {
            errors.Add("El manifiesto debe contener 35 sprites recortados.");
            return;
        }

        foreach (LayoutEntry entry in manifest.sprites)
        {
            string runtimePath = RuntimePathFor(entry.assetName);
            string sourcePath = SourcePathFor(entry.assetName);
            if (!File.Exists(runtimePath) || !File.Exists(sourcePath))
            {
                errors.Add("No se pudo localizar el par original/runtime de " + entry.assetName);
                continue;
            }

            Texture2D original = LoadPng(sourcePath);
            Texture2D cropped = LoadPng(runtimePath);
            bool valid = ValidateCrop(entry, original, cropped);
            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(cropped);

            if (!valid)
                errors.Add("El recorte no conserva exactamente los píxeles de " + entry.assetName);
        }

        if (!errors.Any(error => error.Contains("recorte") || error.Contains("original/runtime")))
            checks.Add("Los 35 recortes conservan exactamente los píxeles RGBA originales.");
    }

    private static bool ValidateCrop(LayoutEntry entry, Texture2D original, Texture2D cropped)
    {
        if (original.width != entry.originalWidth || original.height != entry.originalHeight) return false;
        if (cropped.width != entry.sourceWidth + CropPadding * 2) return false;
        if (cropped.height != entry.sourceHeight + CropPadding * 2) return false;

        Color32[] sourcePixels = original.GetPixels32();
        Color32[] croppedPixels = cropped.GetPixels32();
        for (int y = 0; y < cropped.height; y++)
        {
            for (int x = 0; x < cropped.width; x++)
            {
                bool inside = x >= CropPadding && x < cropped.width - CropPadding
                              && y >= CropPadding && y < cropped.height - CropPadding;
                Color32 actual = croppedPixels[y * cropped.width + x];
                if (!inside)
                {
                    if (actual.a != 0) return false;
                    continue;
                }

                int sourceX = entry.sourceX + x - CropPadding;
                int sourceY = entry.sourceY + y - CropPadding;
                Color32 expected = sourcePixels[sourceY * original.width + sourceX];
                if (!SameColor(expected, actual)) return false;
            }
        }
        return true;
    }

    private static bool SameColor(Color32 a, Color32 b) =>
        a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

    private static Texture2D LoadPng(string path)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path), false))
            throw new InvalidOperationException("No se pudo leer " + path);
        return texture;
    }

    private static string RuntimePathFor(string fileName)
    {
        string folder = fileName.StartsWith("piece-", StringComparison.OrdinalIgnoreCase)
            ? "Pieces"
            : fileName.StartsWith("seam-", StringComparison.OrdinalIgnoreCase)
                ? "Seams"
                : "Elements";
        return Absolute("Assets/Wiñaypaq/Art/Clock/" + folder + "/" + fileName);
    }

    private static string SourcePathFor(string fileName)
    {
        string originalName;
        if (fileName.StartsWith("piece-", StringComparison.OrdinalIgnoreCase))
        {
            int number = int.Parse(Path.GetFileNameWithoutExtension(fileName).Substring("piece-".Length));
            originalName = "Pieza" + number + "Remake.png";
        }
        else if (fileName.StartsWith("seam-", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = Path.GetFileNameWithoutExtension(fileName).Substring("seam-".Length).Split('-');
            originalName = int.Parse(parts[0]) + "-" + int.Parse(parts[1]) + "Remake.png";
        }
        else
        {
            originalName = fileName == "sun.png"
                ? "SolRemake.png"
                : fileName == "shadow.png"
                    ? "SombraRemake.png"
                    : "FiguraCompletaRemake.png";
        }

        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ArtSource", "Clock", "OriginalCanvasExports", originalName));
    }

    private static void ValidateScene(List<string> errors, List<string> checks)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ClockJamPiece[] pieces = ComponentsInScene<ClockJamPiece>(scene);
        ClockJamPuzzleManager[] managers = ComponentsInScene<ClockJamPuzzleManager>(scene);
        LayoutManifest manifest = JsonUtility.FromJson<LayoutManifest>(
            AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath).text);
        Dictionary<string, LayoutEntry> layout = manifest.sprites.ToDictionary(entry => entry.assetName);

        if (pieces.Length != 11)
            errors.Add("La escena debe contener 11 piezas; contiene " + pieces.Length + ".");
        else
            checks.Add("La escena contiene 11 piezas.");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PiecePrefabPath);
        Material sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        foreach (ClockJamPiece piece in pieces)
        {
            if (piece.snapTarget == null || piece.origen == null)
                errors.Add(piece.name + " no tiene target u origen asignado.");
            if (piece.fireEffect == null || piece.respawnEffect == null)
                errors.Add(piece.name + " no tiene sus dos efectos asignados.");
            if (PrefabUtility.GetCorrespondingObjectFromSource(piece.gameObject) != prefab)
                errors.Add(piece.name + " no es instancia de ClockPiece.prefab.");

            int pieceNumber = int.Parse(piece.name.Substring("Pieza ".Length));
            LayoutEntry placement = layout["piece-" + pieceNumber.ToString("00") + ".png"];
            Vector2 actualPosition = piece.snapTarget != null ? piece.snapTarget.localPosition : Vector2.zero;
            if (Vector2.Distance(actualPosition, new Vector2(placement.offsetX, placement.offsetY)) > 0.0001f)
                errors.Add(piece.name + " no conserva su offset de composición.");

            ParticleSystem[] effects = piece.GetComponentsInChildren<ParticleSystem>(true);
            if (effects.Length != 2)
                errors.Add(piece.name + " debe contener exactamente dos efectos.");
            foreach (ParticleSystem effect in effects)
            {
                ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
                if (renderer == null || renderer.sharedMaterial != sharedMaterial)
                    errors.Add(piece.name + " contiene un efecto sin el material compartido.");
            }
        }

        if (managers.Length != 1 || managers[0].seams == null || managers[0].seams.Length != 21)
            errors.Add("PuzzleManager debe contener 21 costuras configuradas.");
        else
        {
            checks.Add("PuzzleManager contiene 21 costuras configuradas.");
            foreach (TapestrySeam seam in managers[0].seams)
            {
                string[] numbers = seam.seamObject.name.Substring("Costura ".Length).Split('-');
                string assetName = "seam-" + int.Parse(numbers[0]).ToString("00") + "-"
                                   + int.Parse(numbers[1]).ToString("00") + ".png";
                LayoutEntry placement = layout[assetName];
                Vector2 actualPosition = seam.seamObject.transform.localPosition;
                if (Vector2.Distance(actualPosition, new Vector2(placement.offsetX, placement.offsetY)) > 0.0001f)
                    errors.Add(seam.seamObject.name + " no conserva su offset de composición.");
            }
        }

        SpriteRenderer figureRenderer = ComponentsInScene<SpriteRenderer>(scene)
            .FirstOrDefault(renderer => renderer.name == "Figura Completa");
        LayoutEntry figurePlacement = layout["figure-complete.png"];
        if (figureRenderer == null || Vector2.Distance(
                figureRenderer.transform.localPosition,
                new Vector2(figurePlacement.offsetX, figurePlacement.offsetY)) > 0.0001f)
            errors.Add("Figura Completa no conserva su offset de composición.");

        int missingScripts = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);

        if (missingScripts != 0)
            errors.Add("La escena contiene " + missingScripts + " scripts perdidos.");
        else
            checks.Add("La escena no contiene scripts perdidos.");

        if (!errors.Any(error => error.Contains("Pieza") || error.Contains("pieza") || error.Contains("efecto")))
            checks.Add("Las 11 piezas usan el prefab base y el material de partículas compartido.");

        if (!errors.Any(error => error.Contains("offset de composición")))
            checks.Add("Piezas, costuras y figura completa conservan sus offsets de composición.");

        ValidatePivotedLayer(scene, layout, "Sol", "sun.png", errors);
        ValidatePivotedLayer(scene, layout, "Sombra", "shadow.png", errors);
        if (!errors.Any(error => error.Contains("pivote visual")))
            checks.Add("Sol y sombra conservan sus pivotes visuales tras el recorte.");

        FileInfo sceneFile = new FileInfo(Absolute(ScenePath));
        if (sceneFile.Length >= 1024 * 1024)
            errors.Add("La escena volvió a superar 1 MB: " + sceneFile.Length + " bytes.");
        else
            checks.Add("La escena pesa menos de 1 MB (" + sceneFile.Length + " bytes).");
    }

    private static T[] ComponentsInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }

    private static void ValidatePivotedLayer(
        Scene scene,
        Dictionary<string, LayoutEntry> layout,
        string objectName,
        string assetName,
        List<string> errors)
    {
        Transform pivot = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == objectName);
        Transform visual = pivot != null ? pivot.Find("Visual") : null;
        LayoutEntry placement = layout[assetName];
        if (visual == null || Vector2.Distance(
                visual.localPosition,
                new Vector2(placement.offsetX, placement.offsetY)) > 0.0001f)
            errors.Add(objectName + " no conserva su pivote visual.");
    }

    private static string Absolute(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
