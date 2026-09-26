# BiomedPPTX Installer

Standalone installer for [BiomedPPTX](https://github.com/TriasJ/BiomedPPTX) — Biomedical PowerPoint Extensions.

## What It Does

1. Checks that VSTO Runtime is installed (prompts to download if missing)
2. Extracts and runs the ClickOnce deployment (`data.zip`)
3. Deploys SMART-Library assets to `%APPDATA%\BiomedPPTX\Assets\` (`smart-assets.zip`)
4. Deploys BioArt index to `%APPDATA%\BiomedPPTX\Assets\` (`bioart_index.json`)
5. Deploys Tutorial presentation to `%APPDATA%\BiomedPPTX\` (`Tutorial.pptx`)

## Building the Installer

Open `PowerPointLabsInstaller\PowerPointLabsInstallerUi.sln` in Visual Studio and build.

The output is `BiomedPPTXInstaller.exe`.

## Packaging for Distribution

Place these files alongside `BiomedPPTXInstaller.exe`:

| File | Source | Description |
|------|--------|-------------|
| `data.zip` | ClickOnce Publish output from BiomedPPTX | The VSTO add-in deployment |
| `smart-assets.zip` | SMART-Library assets | Contains `SMART-Library/` (illustrations.db + png/) and `SMART-Lib/` (49 source PPTX files) |
| `bioart_index.json` | BioArt crawler output | NCBI BioArt catalog (661 items) |
| `Tutorial.pptx` | `BiomedPPTX/doc/Tutorial.pptx` | Tutorial slideshow |

### Creating smart-assets.zip

```powershell
# From the __Scratch directory
cd "C:\Users\...\Documents\__Scratch"

# Create zip with SMART-Library (db + thumbnails) and SMART-Lib (source PPTXs)
Compress-Archive -Path "SMART-Library\illustrations.db","SMART-Library\png","SMART-Lib" -DestinationPath smart-assets.zip
```

### Creating data.zip

1. In Visual Studio, right-click the BiomedPPTX project > Publish
2. Publish to a local folder
3. Zip the published output as `data.zip`

## Asset Deployment Paths

After installation:

```
%APPDATA%\BiomedPPTX\
  Assets\
    SMART-Library\
      illustrations.db
      png\
    SMART-Lib\
      SMART-Cell-membrane.pptx
      SMART-Receptors-channels.pptx
      ... (49 files)
    bioart_index.json
  Tutorial.pptx
```

## License

Based on [PowerPointLabs-Installer](https://github.com/PowerPointLabs/PowerPointLabs-Installer) (GPLv2).
