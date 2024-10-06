# Illustration localization helper mod

This mod allows modders to make custom localized illustrations.

## Usage
Put your illustration in the `LANG` directory inside the `illustrations directory`,
where `LANG` is first 3 letters of the name of the language (like `rus`, `eng`).

Example: `illustrations/rus/manual.png`

Supported illustrations: 
- All `MenuIllustration`s, game title, region names, safari, sandbox, etc
- Expedition's page titles
- Remix logo in mod options

All illustrations support `illustrations/LANG/NAME.png` path.
Region names in `Regions` menu also support slugcat-specific illustrations using
`illustrations/LANG/SLUGCAT/NAME.png`, for example `illustrations/rus/saint/title_sl.png`.

For region illustration, it checks the files in the following order:
`illustrations/LANG/SLUGCAT/NAME.png`
`illustrations/LANG/NAME.png`
`illustrations/SLUGCAT/NAME.png`

Subtitles for the region illustrations and expedition menus will be hidden if appropriate illustrations are provided.

Region name subtitles (`~ Frosted Cathedral ~`) can be offset if needed, by creating
`illustrations/LANG/regionsublabeloffset_REGION.txt`. `REGION` is region acronym, for example `SL`, `illustrations/rus/regionsublabeloffset_sl.txt`.
First line of the file is be horizontal offset, second line is vertical offset, usually negative for downwards offset.

Example:
```
0
-80
```

Subtitle offsets can also be slugcat-specific, using same naming and orsering (`illustrations/rus/saint/regionsublabeloffset_sl.txt`)

Offset for expedition's `Jukebox` title can be moved with `illustrations/LANG/jukeboxlogooffset.txt`,
using same syntax as with subtitles offset files.

## Building

Set your Rain World path in the .csproj RWPath parameter, overriding mine, then `dotnet build` should build it and copy it into the mods directory. Provided Rain World directory should contain `BepInEx` and `RainWorld_Data` subdirectories.