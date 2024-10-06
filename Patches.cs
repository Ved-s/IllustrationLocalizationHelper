
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Menu;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace IllustrationLocalizationHelper;

class Patches
{
    public static void Apply()
    {
        IL.Menu.ExpeditionJukebox.ctor += ExpeditionJukebox_ctor;
        On.RainWorld.ctor += RainWorld_ctor;
        IL.Menu.Remix.InternalOI_Stats.LoadIllustrations += InternalOI_Stats_LoadIllustrations;

        IL.Menu.ProgressionPage.ctor += PatchExpeditionSubtitlesMenu;
        IL.Menu.CharacterSelectPage.ctor += PatchExpeditionSubtitlesMenu;
        IL.Menu.ChallengeSelectPage.ctor += PatchExpeditionSubtitlesMenu;

        IL.Menu.StatsDialog.ctor += PatchExpeditionSubtitlesDialog;
        IL.Menu.FilterDialog.ctor += PatchExpeditionSubtitlesDialog;
        IL.Menu.UnlockDialog.ctor += PatchExpeditionSubtitlesDialog;

        On.Menu.OptionsMenu.SetCurrentlySelectedOfSeries += OptionsMenu_SetCurrentlySelectedOfSeries;
        IL.Expedition.Expedition.OnInit += Expedition_OnInit;
        IL.Menu.MultiplayerMenu.ClearGameTypeSpecificButtons += MultiplayerMenu_ClearGameTypeSpecificButtons;
        On.Menu.MenuIllustration.ctor += MenuIllustration_ctor;
        IL.Menu.FastTravelScreen.FinalizeRegionSwitch += FastTravelScreen_FinalizeRegionSwitch;
    }

    private static void ExpeditionJukebox_ctor(ILContext il)
    {
        /*
        + call      ExpeditionJukeboxLogoOffset
        + stloc     logoOffset

          ldarg.0
	      ldfld     class FSprite Menu.ExpeditionJukebox::jukeboxLogo
	    - ldc.r4    640
	    + ldc.r4    810
        + ldloc     logoOffset
        + ldfld     Vector2::x
        + add
	      ldloc.0
	      sub
	      callvirt  instance void FNode::set_x(float32)
	      ldarg.0
	      ldfld     class FSprite Menu.ExpeditionJukebox::jukeboxLogo
	      ldc.r4    618
        + ldloc     logoOffset
        + ldfld     Vector2::y
        + add
	      callvirt  instance void FNode::set_y(float32)
	      ldarg.0
	      ldfld     class FSprite Menu.ExpeditionJukebox::jukeboxLogo
	    - ldc.r4    0.0
	    + ldc.r4    0.5
	      ldc.r4    0.0
	      callvirt  instance void FSprite::SetAnchor(float32, float32)
        */

        ILCursor c = new(il);

        if (!c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<ExpeditionJukebox>("jukeboxLogo"),
            x => x.MatchLdcR4(640),
            x => x.MatchLdloc(0),
            x => x.MatchSub(),
            x => x.MatchCallvirt<FNode>("set_x"),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<ExpeditionJukebox>("jukeboxLogo"),
            x => x.MatchLdcR4(618),
            x => x.MatchCallvirt<FNode>("set_y"),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<ExpeditionJukebox>("jukeboxLogo"),
            x => x.MatchLdcR4(0),
            x => x.MatchLdcR4(0),
            x => x.MatchCallvirt<FSprite>("SetAnchor")
        ))
        {
            throw new Exception("ExpeditionJukebox_ctor patch fail");
        }

        int logoOffset = il.Body.Variables.Count;
        il.Body.Variables.Add(new VariableDefinition(il.Import(typeof(Vector2))));

        c.Emit<Patches>(OpCodes.Call, nameof(ExpeditionJukeboxLogoOffset));
        c.Emit(OpCodes.Stloc, logoOffset);

        c.Index += 2;
        c.Next.Operand = 820f;
        c.Index += 1;

        c.Emit(OpCodes.Ldloc, logoOffset);
        c.Emit<Vector2>(OpCodes.Ldfld, "x");
        c.Emit(OpCodes.Add);

        c.Index += 6;

        c.Emit(OpCodes.Ldloc, logoOffset);
        c.Emit<Vector2>(OpCodes.Ldfld, "y");
        c.Emit(OpCodes.Add);

        c.Index += 3;

        c.Next.Operand = 0.5f;
    }

    static Vector2 ExpeditionJukeboxLogoOffset()
    {
        InGameTranslator.LanguageID lang = IllustrationLocalizationHelper.CurrentLanguage;
        string langShort = LocalizationTranslator.LangShort(lang);
        string path = AssetManager.ResolveFilePath($"illustrations/{langShort}/JukeboxLogoOffset.txt");
        if (File.Exists(path))
        {
            string[] lines = File.ReadLines(path).Take(2).ToArray();
            if (lines.Length == 2 && float.TryParse(lines[0], out float x) && float.TryParse(lines[1], out float y))
            {
                Debug.Log(x);
                Debug.Log(y);
                return new(x, y);
            }
        }
        return Vector2.zero;
    }

    private static void RainWorld_ctor(On.RainWorld.orig_ctor orig, RainWorld self)
    {
        orig(self);
        IllustrationLocalizationHelper.rainWorld = self;
    }

    private static void InternalOI_Stats_LoadIllustrations(ILContext il)
    {
        /*
          ldarg.0
	      ldloc.0
        + call      InternalOI_Stats_LoadLocalizedIllustrations
	    - ldlen
	    - conv.i4
        + ldc.i4.0
	      newarr    [UnityEngine.CoreModule]UnityEngine.Texture2D
	      stfld     class [UnityEngine.CoreModule]UnityEngine.Texture2D[] Menu.Remix.InternalOI_Stats::illustrationTextures
        */

        ILCursor c = new(il);

        if (!c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdloc(out _),
            x => x.MatchLdlen(),
            x => x.MatchConvI4(),
            x => x.MatchNewarr<Texture2D>(),
            x => x.MatchStfld(out FieldReference fld) && fld.Name == "illustrationTextures"
        ))
        {
            throw new Exception("InternalOI_Stats_LoadIllustrations patch fail");
        }

        c.Index += 2;
        c.Emit<Patches>(OpCodes.Call, nameof(InternalOI_Stats_LoadLocalizedIllustrations));
        c.RemoveRange(2);
        c.Emit(OpCodes.Ldc_I4_0);
        c.Index += 2;
        c.RemoveRange(c.Instrs.Count - c.Index - 1);
    }

    static void InternalOI_Stats_LoadLocalizedIllustrations(string[] names)
    {
        InGameTranslator.LanguageID lang = IllustrationLocalizationHelper.CurrentLanguage;
        foreach (string name in names)
        {
            IllustrationLocalizationHelper.LoadAtlasIllustration(name, lang, false);
        }
    }

    private static void PatchExpeditionSubtitlesDialog(ILContext il)
    {
        ILCursor c = new(il);

        /*
          ldstr     asset
		  ldc.i4.1
		  newobj    instance void FSprite::.ctor(string, bool)
		  stfld     class FSprite Menu.ProgressionPage::pageTitle
        */
        string asset = "";

        if (!c.TryGotoNext(
            x => x.MatchLdstr(out asset),
            x => x.MatchLdcI4(1),
            x => x.MatchNewobj(out _),
            x => x.MatchStfld(out FieldReference fld) && fld.Name == "pageTitle"

        ))
        {
            throw new Exception($"PatchExpeditionSubtitlesDialog match 1 fail on {il.Method.DeclaringType.Name}");
        }

        /*
          ldfld     class InGameTranslator/LanguageID Options::language
		  ldsfld    class InGameTranslator/LanguageID InGameTranslator/LanguageID::English
		  call      bool class ExtEnum`1<class InGameTranslator/LanguageID>::op_Inequality(class ExtEnum`1<!0>, class ExtEnum`1<!0>)
		  brfalse.s nosubtitle

        + ldarg.1
        + ldstr     asset
        + call      PatchExpeditionSubtitles_ShouldSubtitleBeVisible
        + brfalse   nosubtitle
          
		  ldarg.0
	      ldarg.0
	      ldarg.0
	      ldfld     class [mscorlib]System.Collections.Generic.List`1<class Menu.Page> Menu.Menu::pages
	      ldc.i4.0
	      callvirt  instance !0 class [mscorlib]System.Collections.Generic.List`1<class Menu.Page>::get_Item(int32)
	      ldarg.0
	      ldstr     "-FILTERS-"
          call      instance string Menu.Menu::Translate(string)
        */

        ILLabel nosubtitle = null!;

        if (!c.TryGotoNext(
            x => x.MatchLdfld<Options>("language"),
            x => x.MatchLdsfld<InGameTranslator.LanguageID>("English"),
            x => x.MatchCall(out MethodReference m) && m.Name == "op_Inequality",
            x => x.MatchBrfalse(out nosubtitle),
            x => x.MatchLdarg(0),
            x => x.MatchLdarg(0),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<Menu.Menu>("pages"),
            x => x.MatchLdcI4(0),
            x => x.MatchCallvirt(out _),
            x => x.MatchLdarg(0),
            x => x.MatchLdstr(out string str) && str.StartsWith("-") && str.EndsWith("-"),
            x => x.MatchCall<Menu.Menu>("Translate")
        ))
        {
            throw new Exception($"PatchExpeditionSubtitlesDialog match 2 fail on {il.Method.DeclaringType.Name}");
        }

        c.Index += 4;
        c.Emit(OpCodes.Ldarg_1);
        c.Emit(OpCodes.Ldstr, asset);
        c.Emit<Patches>(OpCodes.Call, nameof(PatchExpeditionSubtitlesDialog_ShouldSubtitleBeVisible));
        c.Emit(OpCodes.Brfalse, nosubtitle);
    }

    static bool PatchExpeditionSubtitlesDialog_ShouldSubtitleBeVisible(ProcessManager manager, string asset)
    {
        InGameTranslator.LanguageID lang = manager.rainWorld.inGameTranslator.currentLanguage;
        return !IllustrationLocalizationHelper.HasIllustrationLocalization(asset, lang);
    }

    private static void PatchExpeditionSubtitlesMenu(ILContext il)
    {
        ILCursor c = new(il);

        /*
          ldstr     asset
		  ldc.i4.1
		  newobj    instance void FSprite::.ctor(string, bool)
		  stfld     class FSprite Menu.ProgressionPage::pageTitle
        */
        string asset = "";

        if (!c.TryGotoNext(
            x => x.MatchLdstr(out asset),
            x => x.MatchLdcI4(1),
            x => x.MatchNewobj(out _),
            x => x.MatchStfld(out FieldReference fld) && fld.Name == "pageTitle"

        ))
        {
            throw new Exception($"PatchExpeditionSubtitlesMenu match 1 fail on {il.Method.DeclaringType.Name}");
        }

        /*
          ldfld     class InGameTranslator/LanguageID Options::language
		  ldsfld    class InGameTranslator/LanguageID InGameTranslator/LanguageID::English
		  call      bool class ExtEnum`1<class InGameTranslator/LanguageID>::op_Inequality(class ExtEnum`1<!0>, class ExtEnum`1<!0>)
		  brfalse.s nosubtitle

        + ldarg.1
        + ldstr     asset
        + call      PatchExpeditionSubtitles_ShouldSubtitleBeVisible
        + brfalse   nosubtitle
          
		  ldarg.0
		  ldarg.1
		  ldarg.0
		  ldarg.1
		  ldstr     "-EXPEDITION-"
          callvirt  instance string Menu.Menu::Translate(string)
        */

        ILLabel nosubtitle = null!;

        if (!c.TryGotoNext(
            x => x.MatchLdfld<Options>("language"),
            x => x.MatchLdsfld<InGameTranslator.LanguageID>("English"),
            x => x.MatchCall(out MethodReference m) && m.Name == "op_Inequality",
            x => x.MatchBrfalse(out nosubtitle),
            x => x.MatchLdarg(0),
            x => x.MatchLdarg(1),
            x => x.MatchLdarg(0),
            x => x.MatchLdarg(1),
            x => x.MatchLdstr(out string str) && str.StartsWith("-") && str.EndsWith("-"),
            x => x.MatchCallvirt<Menu.Menu>("Translate")
        ))
        {
            throw new Exception($"PatchExpeditionSubtitlesMenu match 2 fail on {il.Method.DeclaringType.Name}");
        }

        c.Index += 4;
        c.Emit(OpCodes.Ldarg_1);
        c.Emit(OpCodes.Ldstr, asset);
        c.Emit<Patches>(OpCodes.Call, nameof(PatchExpeditionSubtitlesMenu_ShouldSubtitleBeVisible));
        c.Emit(OpCodes.Brfalse, nosubtitle);
    }

    static bool PatchExpeditionSubtitlesMenu_ShouldSubtitleBeVisible(Menu.Menu menu, string asset)
    {
        InGameTranslator.LanguageID lang = menu.manager.rainWorld.inGameTranslator.currentLanguage;
        return !IllustrationLocalizationHelper.HasIllustrationLocalization(asset, lang);
    }

    private static void OptionsMenu_SetCurrentlySelectedOfSeries(On.Menu.OptionsMenu.orig_SetCurrentlySelectedOfSeries orig, OptionsMenu self, string series, int to)
    {
        InGameTranslator.LanguageID oldLang = self.manager.rainWorld.options.language;
        orig(self, series, to);

        if (series != "Language")
            return;

        InGameTranslator.LanguageID lang = self.manager.rainWorld.options.language;
        if (oldLang == lang)
        {
            return;
        }

        IllustrationLocalizationHelper.ReloadRegisteredIllustrationAssets(lang);
    }

    private static void Expedition_OnInit(ILContext il)
    {
        /*
        - ldsfld    class FAtlasManager Futile::atlasManager
        + ldarg.0
	      ldstr     "expeditiontitle"
        + call      Expedition_LoadAndRegisterAsset
	    - callvirt  instance bool FAtlasManager::DoesContainElementWithName(string)
	    - brtrue.s  iftrue

	    - ldc.i4.0
	    - ldc.i4.0
	    - newobj    instance void [UnityEngine.CoreModule]UnityEngine.Texture2D::.ctor(int32, int32)
	    - stloc.0
	    - ldloc.0
	    - ldstr     "illustrations/expeditiontitle.png"
	    - call      string AssetManager::ResolveFilePath(string)
	    - call      uint8[] [mscorlib]System.IO.File::ReadAllBytes(string)
	    - call      bool [UnityEngine.ImageConversionModule]UnityEngine.ImageConversion::LoadImage(class [UnityEngine.CoreModule]UnityEngine.Texture2D, uint8[])
	    - pop
	    - ldloc.0
	    - ldc.i4.0
	    - callvirt  instance void [UnityEngine.CoreModule]UnityEngine.Texture::set_filterMode(valuetype [UnityEngine.CoreModule]UnityEngine.FilterMode)
	    - ldsfld    class FAtlasManager Futile::atlasManager
	    - ldstr     "expeditiontitle"
	    - ldloc.0
	    - ldc.i4.0
	    - callvirt  instance class FAtlas FAtlasManager::LoadAtlasFromTexture(string, class [UnityEngine.CoreModule]UnityEngine.Texture, bool)
	    - pop
        */

        ILCursor c = new(il);
        bool once = false;

        ILLabel iftrue = null!;

        while (c.TryGotoNext(
            x => x.MatchLdsfld<Futile>("atlasManager"),
            x => x.MatchLdstr(out _),
            x => x.MatchCallvirt<FAtlasManager>("DoesContainElementWithName"),
            x => x.MatchBrtrue(out iftrue)
        ))
        {
            bool hasRegister = false;
            bool hasString = false;

            int targetIndex = il.IndexOf(iftrue.Target);

            for (int i = c.Index + 4; i < targetIndex; i++)
            {
                if (il.Instrs[i].MatchCallvirt<FAtlasManager>("LoadAtlasFromTexture"))
                {
                    hasRegister = true;
                }
                if (il.Instrs[i].MatchLdstr(out string str) && str.StartsWith("illustrations/"))
                {
                    hasString = true;
                }
                if (hasRegister && hasString)
                {
                    break;
                }
            }

            if (!hasRegister || !hasString)
            {
                continue;
            }

            c.Remove();
            c.MarkLabel(iftrue);
            c.Emit(OpCodes.Ldarg_0);
            c.Index += 1;
            c.Emit<Patches>(OpCodes.Call, nameof(Expedition_LoadAndRegisterAsset));
            c.RemoveRange(targetIndex - c.Index + 1);

            once = true;
        }

        if (!once)
        {
            throw new Exception("Expedition_OnInit patch fail");
        }
    }

    static void Expedition_LoadAndRegisterAsset(RainWorld rainWorld, string name)
    {
        InGameTranslator.LanguageID lang = rainWorld.inGameTranslator.currentLanguage;
        IllustrationLocalizationHelper.LoadAtlasIllustration(name, lang, true);
    }

    private static void MultiplayerMenu_ClearGameTypeSpecificButtons(ILContext il)
    {

        ILCursor c = new(il);

        for (int i = 1; i <= 2; i++)
        {

            /*
                ldstr     "shadow" | "title"
		      - callvirt  instance bool [mscorlib]System.String::EndsWith(string)
		      + callvirt  instance bool [mscorlib]System.String::Contains(string)
            */

            if (!c.TryGotoNext(
                x => x.MatchLdstr(out string str) && (str == "shadow" || str == "title"),
                x => x.MatchCallvirt<string>("EndsWith")
            ))
            {
                throw new Exception($"MultiplayerMenu_ClearGameTypeSpecificButtons patch {i} fail");
            }

            c.Index += 1;
            c.Next.Operand = typeof(string).GetMethod("Contains", BindingFlags.Instance | BindingFlags.Public);

            c.Index += 1;
        }
    }

    private static void MenuIllustration_ctor(On.Menu.MenuIllustration.orig_ctor orig, MenuIllustration self, Menu.Menu menu, MenuObject owner, string folderName, string fileName, Vector2 pos, bool crispPixels, bool anchorCenter)
    {
        if (folderName == "" || folderName.ToLower() == "illustrations")
        {
            SlugcatStats.Name? scug = (menu as FastTravelScreen)?.activeMenuSlugcat;
            InGameTranslator.LanguageID lang = menu.manager.rainWorld.inGameTranslator.currentLanguage;

            if (scug is not null)
            {
                fileName = IllustrationLocalizationHelper.ResolveSlugcatIllustration(fileName, scug, lang, false);
            }
            else
            {
                fileName = IllustrationLocalizationHelper.ResolveIllustration(fileName, lang, false);
            }

            if (fileName.EndsWith(".png"))
            {
                fileName = fileName.Substring(0, fileName.Length - 4);
            }
        }

        orig(self, menu, owner, folderName, fileName, pos, crispPixels, anchorCenter);
    }

    private static void FastTravelScreen_FinalizeRegionSwitch(ILContext il)
    {
        /* if (text != Region.GetRegionFullName(this.manager.rainWorld.progression.regionNames[this.accessibleRegions[newRegion]], SlugcatStats.Name.White))

          ldsfld    class SlugcatStats/Name SlugcatStats/Name::White
          call      string Region::GetRegionFullName(string, class SlugcatStats/Name)
          call      bool [mscorlib]System.String::op_Inequality(string, string)
          brfalse   iffalse

        + ldarg.0
        + ldarg.1
        + call      FastTravelScreen_ShouldDisplaySubtitle
        + brfalse   iffalse

          ldarg.0
        */

        ILCursor c = new(il);

        ILLabel iffalse = null!;

        if (!c.TryGotoNext(
            x => x.MatchLdsfld<SlugcatStats.Name>("White"),
            x => x.MatchCall<Region>("GetRegionFullName"),
            x => x.MatchCall<string>("op_Inequality"),
            x => x.MatchBrfalse(out iffalse)
        ))
        {
            throw new Exception("FastTravelScreen_FinalizeRegionSwitch patch 1 fail");
        }

        c.Index += 4;
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.Emit<Patches>(OpCodes.Call, nameof(FastTravelScreen_ShouldDisplaySubtitle));
        c.Emit(OpCodes.Brfalse, iffalse);

        c.Index = 0;

        /*
              + ldarg.0
              + ldarg.1
              + ldloca    offset
              + call      FastTravelScreen_GetSubtitleLabelOffset
              + brtrue    done
      
                ldarg.0
	            ldfld     class Region[] Menu.FastTravelScreen::allRegions
	            ldarg.0
	            ldfld     class [mscorlib]System.Collections.Generic.List`1<int32> Menu.FastTravelScreen::accessibleRegions
	            ldarg.1
	            callvirt  instance !0 class [mscorlib]System.Collections.Generic.List`1<int32>::get_Item(int32)
	            ldelem.ref
	            ldfld     string Region::name
	            call      class Menu.MenuScene/SceneID Region::GetRegionLandscapeScene(string)
	            call      valuetype [UnityEngine.CoreModule]UnityEngine.Vector2 Menu.FastTravelScreen::GetSubtitleLabelOffset(class Menu.MenuScene/SceneID)
                stloc.s   offset
        done:  
        */

        int offset = 0;

        if (!c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<FastTravelScreen>("allRegions"),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<FastTravelScreen>("accessibleRegions"),
            x => x.MatchLdarg(1),
            x => x.MatchCallvirt(out _),
            x => x.MatchLdelemRef(),
            x => x.MatchLdfld<Region>("name"),
            x => x.MatchCall<Region>("GetRegionLandscapeScene"),
            x => x.MatchCall<FastTravelScreen>("GetSubtitleLabelOffset"),
            x => x.MatchStloc(out offset)
        ))
        {
            throw new Exception("FastTravelScreen_FinalizeRegionSwitch patch 2 fail");
        }

        ILLabel done = c.DefineLabel();
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.Emit(OpCodes.Ldloca, offset);
        c.Emit<Patches>(OpCodes.Call, nameof(FastTravelScreen_GetSubtitleLabelOffset));
        c.Emit(OpCodes.Brtrue, done);

        c.Index += 11;
        c.MarkLabel(done);
    }

    static bool FastTravelScreen_GetSubtitleLabelOffset(FastTravelScreen self, int newRegion, out Vector2 vec)
    {
        string reg = self.allRegions[self.accessibleRegions[newRegion]].name;
        return IllustrationLocalizationHelper.GetSubtitleLabelOffset(reg, self.activeMenuSlugcat, self.manager.rainWorld.inGameTranslator.currentLanguage, out vec);
    }

    static bool FastTravelScreen_ShouldDisplaySubtitle(FastTravelScreen self, int newRegion)
    {
        string acro = self.manager.rainWorld.progression.regionNames[self.accessibleRegions[newRegion]];
        SlugcatStats.Name scug = self.activeMenuSlugcat;
        InGameTranslator.LanguageID lang = self.manager.rainWorld.inGameTranslator.currentLanguage;
        return IllustrationLocalizationHelper.ShouldDisplayRegionSubtitles(acro, scug, lang);
    }
}