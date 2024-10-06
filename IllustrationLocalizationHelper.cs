using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace IllustrationLocalizationHelper;

[BepInPlugin("ved_s.illustrationlochelper", "IllustrationLocalizationHelper", "1.0")]
public class IllustrationLocalizationHelper : BaseUnityPlugin
{
    internal static RainWorld? rainWorld;

    public static InGameTranslator.LanguageID CurrentLanguage 
        => rainWorld?.options.language ?? InGameTranslator.LanguageID.English;

    public static HashSet<string> LocalizedIllustrationAssets = new();
    internal IllustrationLocalizationHelper? Instance;

    public IllustrationLocalizationHelper()
    {
        Instance = this;
    }

    public void OnEnable()
    {
        try
        {
            Patches.Apply();
        }
        catch (Exception e)
        {
            Logger.LogError($"IllustrationLocalizationHelper patching exception: {e}");
        }
    }

    public static void ReloadRegisteredIllustrationAssets(InGameTranslator.LanguageID language)
    {
        foreach (string name in LocalizedIllustrationAssets)
        {
            LoadAtlasIllustration(name, language, false);
        }
    }

    public static void LoadAtlasIllustration(string name, InGameTranslator.LanguageID language, bool registerReload)
    {

        string langShort = LocalizationTranslator.LangShort(language);
        string asset = $"illustrations/{langShort}/{name}.png";
        string path = AssetManager.ResolveFilePath(asset);
        if (!File.Exists(path))
        {
            path = AssetManager.ResolveFilePath($"illustrations/{name}.png");
        }

        if (Futile.atlasManager.DoesContainElementWithName(name))
            Futile.atlasManager.UnloadAtlas(name);

        Texture2D tex = new(0, 0);
        tex.LoadImage(File.ReadAllBytes(path));
        tex.filterMode = FilterMode.Point;
        Futile.atlasManager.LoadAtlasFromTexture(name, tex, false);

        if (registerReload)
        {
            LocalizedIllustrationAssets.Add(name);
        }
    }

    public static bool HasIllustrationLocalization(string name, InGameTranslator.LanguageID language)
    {
        string langShort = LocalizationTranslator.LangShort(language);
        string asset = $"illustrations/{langShort}/{name}.png";
        string path = AssetManager.ResolveFilePath(asset);
        return File.Exists(path);
    }

    public static bool GetSubtitleLabelOffset(string regionAcro, SlugcatStats.Name scug, InGameTranslator.LanguageID language, out Vector2 vec)
    {
        string? path = null;
        foreach (string p in TryScugLanguagePaths(scug, language))
        {
            string trypath = $"illustrations/{p}/RegionSubLabelOffset_{regionAcro}.txt";
            string file = AssetManager.ResolveFilePath(trypath);
            if (File.Exists(file))
            {
                path = file;
            }
        }

        if (path is null)
        {
            path = AssetManager.ResolveFilePath($"illustrations/RegionSubLabelOffset_{regionAcro}.txt");
            if (!File.Exists(path))
            {
                vec = Vector2.zero;
                return false;
            }
        }

        string[] lines = File.ReadLines(path).Take(2).ToArray();
        if (lines.Length == 2 && float.TryParse(lines[0], out float x) && float.TryParse(lines[1], out float y))
        {
            vec = new(x, y);
            return true;
        }
        else
        {
            vec = Vector2.zero;
            return false;
        }

    }

    public static bool ShouldDisplayRegionSubtitles(string acro, SlugcatStats.Name scug, InGameTranslator.LanguageID language)
    {
        string langShort = LocalizationTranslator.LangShort(language);
        string scugSpecificIllustr = AssetManager.ResolveFilePath($"Illustrations/{langShort}/{scug.value}/Title_{acro}.png");
        if (File.Exists(scugSpecificIllustr))
        {
            return false;
        }

        string scugSpecificRegName = AssetManager.ResolveFilePath($"World/{acro}/DisplayName-{scug.value}.txt");
        if (!File.Exists(scugSpecificRegName))
        {
            string genericIllustr = AssetManager.ResolveFilePath($"Illustrations/{langShort}/Title_{acro}.png");

            return !File.Exists(genericIllustr);
        }
        return true;
    }

    public static string ResolveSlugcatIllustration(string name, SlugcatStats.Name scug, InGameTranslator.LanguageID language, bool fullpath)
    {
        foreach (string p in TryScugLanguagePaths(scug, language))
        {
            string path = $"illustrations/{p}/{name}.png";
            string file = AssetManager.ResolveFilePath(path);
            if (File.Exists(file))
            {
                return fullpath ? file : $"{p}/{name}.png";
            }
        }

        if (fullpath)
            return AssetManager.ResolveFilePath($"illustrations/{name}.png");
        else
            return $"{name}.png";
    }

    public static string ResolveIllustration(string name, InGameTranslator.LanguageID language, bool fullpath)
    {
        string langShort = LocalizationTranslator.LangShort(language);
        string path = $"illustrations/{langShort}/{name}.png";

        string file = AssetManager.ResolveFilePath(path);
        if (File.Exists(file))
            return fullpath ? file : $"{langShort}/{name}.png";

        if (fullpath)
            return AssetManager.ResolveFilePath($"illustrations/{name}.png");
        else
            return $"{name}.png";
    }

    public static IEnumerable<string> TryScugLanguagePaths(SlugcatStats.Name scug, InGameTranslator.LanguageID language)
    {
        string langShort = LocalizationTranslator.LangShort(language);
        yield return $"{langShort}/{scug.value}";
        yield return $"{langShort}";
        yield return $"{scug.value}";
    }
}

