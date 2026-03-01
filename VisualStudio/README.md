# btx_decoder_csharp

C#-Port des Projekts `btx_decoder_C` inklusive SDL3-Frontend.

## Ziel: Windows

Damit es unter Windows läuft, lege `SDL3.dll` hier ab:

- `native/win-x64/SDL3.dll`

Beim Build wird die Datei ins Output kopiert, und der Loader lädt sie von dort.

## Build (Windows)

```powershell
dotnet restore --configfile NuGet.Config
dotnet build --no-restore -c Release
```

## Start (Windows)

```powershell
dotnet run --no-build -- 195.201.94.166:20000
```

Oder veröffentlichen:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

Dann liegt die EXE unter `bin/Release/net8.0/win-x64/publish/`.

## Bedienung

- `F1` Initiator `*`
- `F2` Terminator `#`
- `F3` Vordergrundfarbe
- `Shift+F3` Hintergrundfarbe
- `F4` Größe
- `F5` Zeichensatzumschaltung
- `F12` DCT
- Pfeiltasten Cursor
- `Enter` sendet `CR+LF`
- `Alt+Enter` sendet `CR`
- `Shift+Enter` sendet `CAN`
- `Home` APH
- `Shift+Home` Clear Screen
- `Backspace` sendet `\b \b`

## Hinweis

Die Decoder-Engine (`layer2`, `layer6`, `xfont`, Zeichensatzdaten) wurde aus C nach C# portiert.
Das Frontend rendert wie im Original per SDL3-Textur.
