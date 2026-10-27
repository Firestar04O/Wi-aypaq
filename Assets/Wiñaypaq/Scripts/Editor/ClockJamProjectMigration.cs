using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class ClockJamProjectMigration
{
    private const string LegacyArtRoot = "Assets/Assets/FinalReloj_export";
    private const string ProjectRoot = "Assets/Wiñaypaq";
    private const string ClockArtRoot = ProjectRoot + "/Art/Clock";
    private const string LayoutPath = ClockArtRoot + "/clock-layout.json";
    private const string ParticleTexturePath = ProjectRoot + "/Art/Effects/particle-soft.png";
    private const string ParticleMaterialPath = ProjectRoot + "/Art/Effects/clock-particles.mat";
    private const string PiecePrefabPath = ProjectRoot + "/Prefabs/ClockPiece.prefab";
    private const string AtlasPath = ClockArtRoot + "/clock-gameplay.spriteatlas";
    private const float PixelsPerUnit = 100f;
    private const int CropPadding = 2;

    [Serializable]
    private sealed class LayoutManifest
    {
        public int version = 1;
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

    [MenuItem("Tools/Clock Jam/Migrate Project Structure")]
    public static void Run()
    {
        if (File.Exists(ToAbsolute(LayoutPath)))
        {
            Debug.Log("La migración de arte ya fue aplicada: " + LayoutPath);
            return;
        }

        PreserveSourceExports();
        EnsureProjectFolders();

        MoveStaticArt();
        MovePiecesAndSeams();
        MoveProjectAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        LayoutManifest manifest = CropRuntimeSprites();
        File.WriteAllText(ToAbsolute(LayoutPath), JsonUtility.ToJson(manifest, true));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureClockTextures();
        CreateSharedParticleAssets();
        CreatePiecePrefab();
        CreateClockAtlas();
        RemoveArchivedImportedAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        MoveScriptsLast();
        AssetDatabase.SaveAssets();
        Debug.Log("Migración de estructura y arte completada.");
    }

    public static void RunFromCommandLine()
    {
        try
        {
            Run();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    public static void CropAnimatedElementsFromCommandLine()
    {
        TextAsset layoutText = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
        if (layoutText == null)
            throw new InvalidOperationException("No se encontró " + LayoutPath);

        LayoutManifest manifest = JsonUtility.FromJson<LayoutManifest>(layoutText.text);
        AddCropIfMissing(manifest, ClockArtRoot + "/Elements/sun.png");
        AddCropIfMissing(manifest, ClockArtRoot + "/Elements/shadow.png");
        File.WriteAllText(ToAbsolute(LayoutPath), JsonUtility.ToJson(manifest, true));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureClockTextures();
        AssetDatabase.SaveAssets();
        Debug.Log("Sol y sombra fueron recortados conservando sus offsets de pivote.");
    }

    private static void AddCropIfMissing(LayoutManifest manifest, string assetPath)
    {
        string fileName = Path.GetFileName(assetPath);
        if (manifest.sprites.Exists(entry => entry.assetName == fileName)) return;
        manifest.sprites.Add(CropPng(ToAbsolute(assetPath)));
    }

    private static void PreserveSourceExports()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string sourceRoot = Path.Combine(root, "ArtSource", "Clock");
        CopyDirectory(
            Path.Combine(Application.dataPath, "Assets", "FinalReloj_export"),
            Path.Combine(sourceRoot, "OriginalCanvasExports"));
        CopyDirectory(
            Path.Combine(Application.dataPath, "Assets", "Old"),
            Path.Combine(sourceRoot, "LegacyExports"));
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) return;
        Directory.CreateDirectory(destination);

        foreach (string file in Directory.GetFiles(source, "*.png", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
    }

    private static void EnsureProjectFolders()
    {
        string[] folders =
        {
            ProjectRoot,
            ProjectRoot + "/Art",
            ClockArtRoot,
            ClockArtRoot + "/Backgrounds",
            ClockArtRoot + "/Elements",
            ClockArtRoot + "/Pieces",
            ClockArtRoot + "/Seams",
            ProjectRoot + "/Art/Effects",
            ProjectRoot + "/Input",
            ProjectRoot + "/Prefabs",
            ProjectRoot + "/Scenes"
        };

        foreach (string folder in folders)
            Directory.CreateDirectory(ToAbsolute(folder));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void MoveStaticArt()
    {
        MoveAsset(LegacyArtRoot + "/CieloRemake.png", ClockArtRoot + "/Backgrounds/sky.png");
        MoveAsset(LegacyArtRoot + "/FondoRemake.png", ClockArtRoot + "/Backgrounds/background.png");
        MoveAsset(LegacyArtRoot + "/MarcoRemake.png", ClockArtRoot + "/Backgrounds/frame.png");
        MoveAsset(LegacyArtRoot + "/HoraYRangoRemake.png", ClockArtRoot + "/Backgrounds/hour-range.png");
        MoveAsset(LegacyArtRoot + "/FiguraCompletaRemake.png", ClockArtRoot + "/Elements/figure-complete.png");
        MoveAsset(LegacyArtRoot + "/SolRemake.png", ClockArtRoot + "/Elements/sun.png");
        MoveAsset(LegacyArtRoot + "/SombraRemake.png", ClockArtRoot + "/Elements/shadow.png");
    }

    private static void MovePiecesAndSeams()
    {
        for (int i = 1; i <= 11; i++)
            MoveAsset(
                LegacyArtRoot + "/Pieza" + i + "Remake.png",
                ClockArtRoot + "/Pieces/piece-" + i.ToString("00") + ".png");

        string[] seamNames =
        {
            "1-2", "1-3", "1-7", "1-8", "1-10", "1-11", "2-3", "3-4", "3-10", "4-5",
            "4-8", "4-9", "4-10", "5-6", "5-8", "6-7", "7-8", "7-11", "8-9", "8-10", "8-11"
        };

        foreach (string seam in seamNames)
        {
            string[] pieces = seam.Split('-');
            string normalized = int.Parse(pieces[0]).ToString("00") + "-" + int.Parse(pieces[1]).ToString("00");
            MoveAsset(
                LegacyArtRoot + "/" + seam + "Remake.png",
                ClockArtRoot + "/Seams/seam-" + normalized + ".png");
        }
    }

    private static void MoveProjectAssets()
    {
        MoveAsset("Assets/Scenes/Scene-Gameplay.unity", ProjectRoot + "/Scenes/ClockGameplay.unity");
        MoveAsset("Assets/InputSystem_Actions.inputactions", ProjectRoot + "/Input/InputSystem_Actions.inputactions");
        MoveAsset("Assets/Settings", ProjectRoot + "/Settings");
    }

    private static LayoutManifest CropRuntimeSprites()
    {
        LayoutManifest manifest = new LayoutManifest();

        foreach (string path in Directory.GetFiles(ToAbsolute(ClockArtRoot + "/Pieces"), "*.png"))
            manifest.sprites.Add(CropPng(path));
        foreach (string path in Directory.GetFiles(ToAbsolute(ClockArtRoot + "/Seams"), "*.png"))
            manifest.sprites.Add(CropPng(path));

        manifest.sprites.Add(CropPng(ToAbsolute(ClockArtRoot + "/Elements/figure-complete.png")));
        return manifest;
    }

    private static LayoutEntry CropPng(string absolutePath)
    {
        byte[] sourceBytes = File.ReadAllBytes(absolutePath);
        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(sourceBytes, false))
            throw new InvalidOperationException("No se pudo leer " + absolutePath);

        Color32[] pixels = source.GetPixels32();
        int minX = source.width;
        int minY = source.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                if (pixels[y * source.width + x].a == 0) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            UnityEngine.Object.DestroyImmediate(source);
            throw new InvalidOperationException("El sprite no contiene píxeles visibles: " + absolutePath);
        }

        int contentWidth = maxX - minX + 1;
        int contentHeight = maxY - minY + 1;
        int outputWidth = contentWidth + CropPadding * 2;
        int outputHeight = contentHeight + CropPadding * 2;
        Texture2D output = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
        Color32[] cropped = new Color32[outputWidth * outputHeight];

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int destinationX = x - minX + CropPadding;
                int destinationY = y - minY + CropPadding;
                cropped[destinationY * outputWidth + destinationX] = pixels[y * source.width + x];
            }
        }

        output.SetPixels32(cropped);
        output.Apply(false, false);
        File.WriteAllBytes(absolutePath, output.EncodeToPNG());

        LayoutEntry entry = new LayoutEntry
        {
            assetName = Path.GetFileName(absolutePath),
            originalWidth = source.width,
            originalHeight = source.height,
            sourceX = minX,
            sourceY = minY,
            sourceWidth = contentWidth,
            sourceHeight = contentHeight,
            offsetX = (((minX + maxX + 1) * 0.5f) - source.width * 0.5f) / PixelsPerUnit,
            offsetY = (((minY + maxY + 1) * 0.5f) - source.height * 0.5f) / PixelsPerUnit
        };

        UnityEngine.Object.DestroyImmediate(source);
        UnityEngine.Object.DestroyImmediate(output);
        return entry;
    }

    private static void ConfigureClockTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ClockArtRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

    private static void CreateSharedParticleAssets()
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x + 0.5f, y + 0.5f),
                    new Vector2(texture.width * 0.5f, texture.height * 0.5f)) / (texture.width * 0.5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply(false, false);
        File.WriteAllBytes(ToAbsolute(ParticleTexturePath), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(ParticleTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        AssetDatabase.DeleteAsset(ParticleMaterialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) throw new InvalidOperationException("No se encontró un shader para partículas.");

        Material material = new Material(shader)
        {
            name = "Clock Particles",
            mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ParticleTexturePath)
        };
        AssetDatabase.CreateAsset(material, ParticleMaterialPath);
    }

    private static void CreatePiecePrefab()
    {
        AssetDatabase.DeleteAsset(PiecePrefabPath);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);

        GameObject root = new GameObject("ClockPiece");
        root.AddComponent<SpriteRenderer>();
        root.AddComponent<PolygonCollider2D>();
        ClockJamPiece piece = root.AddComponent<ClockJamPiece>();
        piece.fireEffect = CreateParticleEffect(
            root.transform, "Smoke FX", material,
            new Color(0.45f, 0.42f, 0.4f, 0.55f), 0.45f, 0.9f, 0.9f, false);
        piece.respawnEffect = CreateParticleEffect(
            root.transform, "Thread Respawn FX", material,
            new Color(1f, 0.85f, 0.3f, 0.9f), 0.12f, 2.2f, 1.2f, true);

        PrefabUtility.SaveAsPrefabAsset(root, PiecePrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static ParticleSystem CreateParticleEffect(
        Transform parent,
        string name,
        Material material,
        Color color,
        float size,
        float speed,
        float lifetime,
        bool small)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
        main.startSize = size;
        main.startSpeed = speed;
        main.startLifetime = lifetime;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop = true;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = small ? 10f : 16f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = small ? 0.2f : 0.4f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 20;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private static void CreateClockAtlas()
    {
        AssetDatabase.DeleteAsset(AtlasPath);
        SpriteAtlas atlas = new SpriteAtlas();
        SpriteAtlasPackingSettings packing = atlas.GetPackingSettings();
        packing.enableRotation = false;
        packing.enableTightPacking = true;
        packing.padding = 4;
        atlas.SetPackingSettings(packing);

        SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings();
        textureSettings.generateMipMaps = false;
        textureSettings.filterMode = FilterMode.Point;
        textureSettings.sRGB = true;
        atlas.SetTextureSettings(textureSettings);

        AssetDatabase.CreateAsset(atlas, AtlasPath);
        UnityEngine.Object[] packables =
        {
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ClockArtRoot + "/Pieces"),
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ClockArtRoot + "/Seams"),
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ClockArtRoot + "/Elements")
        };
        SpriteAtlasExtensions.Add(atlas, packables);
        SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
        EditorUtility.SetDirty(atlas);
    }

    private static void RemoveArchivedImportedAssets()
    {
        AssetDatabase.DeleteAsset("Assets/Assets/Old");
        AssetDatabase.DeleteAsset("Assets/Scenes/_Backup");
        AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
        AssetDatabase.DeleteAsset("Assets/TutorialInfo");
        AssetDatabase.DeleteAsset("Assets/Readme.asset");
        AssetDatabase.DeleteAsset(LegacyArtRoot);
        AssetDatabase.DeleteAsset("Assets/Assets");
        AssetDatabase.DeleteAsset("Assets/Scenes");
    }

    private static void MoveScriptsLast()
    {
        MoveAsset("Assets/Scripts/Scripts", ProjectRoot + "/Scripts");
        AssetDatabase.DeleteAsset("Assets/Scripts");
    }

    private static void MoveAsset(string source, string destination)
    {
        if (AssetDatabase.LoadMainAssetAtPath(source) == null) return;
        if (AssetDatabase.LoadMainAssetAtPath(destination) != null)
            throw new InvalidOperationException("El destino ya existe: " + destination);

        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException("No se pudo mover " + source + " a " + destination + ": " + error);
    }

    private static string ToAbsolute(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
