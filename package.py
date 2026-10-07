#!/usr/bin/env python3
"""Build Thunderstore-compatible zips (for Hexium/Gale) from each mod's tracked package/ folder.

    ./package.py                  # every mod with a package/ folder
    ./package.py OtBulkStation    # just the named mod(s)

For each mod: builds the Release DLL, stages <Mod>/package/* plus the DLL into <Mod>/dist/pkg/,
and zips that to <Mod>/dist/<Mod>-<version>.zip. The version comes from PluginVersion in
<Mod>/Plugin.cs and is injected into manifest.json here - package/manifest.json deliberately has
no version_number of its own, so the two can't drift apart.
"""

import json
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PACKAGE_FILES = ["manifest.json", "icon.png", "README.md", "CHANGELOG.md"]
MAX_DESCRIPTION = 256


def fail(mod, message):
    sys.exit(f"{mod}: {message}")


def plugin_version(mod_dir):
    match = re.search(r'PluginVersion\s*=\s*"([^"]+)"', (mod_dir / "Plugin.cs").read_text())
    if not match:
        fail(mod_dir.name, "couldn't find PluginVersion in Plugin.cs")
    version = match.group(1)
    if not re.fullmatch(r"\d+\.\d+\.\d+", version):
        fail(mod_dir.name, f'PluginVersion "{version}" isn\'t Major.Minor.Patch')
    return version


def build_manifest(mod_dir, version):
    manifest = json.loads((mod_dir / "package" / "manifest.json").read_text())
    if "version_number" in manifest:
        fail(mod_dir.name, "package/manifest.json shouldn't set version_number - it comes from PluginVersion")
    if not re.fullmatch(r"\w+", manifest.get("name", "")):
        fail(mod_dir.name, "manifest name must be letters/digits/underscores only")
    if len(manifest.get("description", "")) > MAX_DESCRIPTION:
        fail(mod_dir.name, f"manifest description is over {MAX_DESCRIPTION} characters")

    # Keep Thunderstore's conventional key order: version_number right after description.
    ordered = {}
    for key, value in manifest.items():
        ordered[key] = value
        if key == "description":
            ordered["version_number"] = version
    return json.dumps(ordered, indent=4, ensure_ascii=False) + "\n"


def package(mod_dir):
    mod = mod_dir.name
    for name in PACKAGE_FILES:
        if not (mod_dir / "package" / name).is_file():
            fail(mod, f"missing package/{name}")

    version = plugin_version(mod_dir)
    changelog = (mod_dir / "package" / "CHANGELOG.md").read_text()
    if not re.search(rf"^## {re.escape(version)}\s*$", changelog, re.MULTILINE):
        fail(mod, f'package/CHANGELOG.md has no "## {version}" section')
    manifest = build_manifest(mod_dir, version)

    subprocess.run(["dotnet", "build", "-c", "Release", "--nologo", "-v", "quiet"], cwd=mod_dir, check=True)
    dll = mod_dir / "bin" / "Release" / "netstandard2.1" / f"{mod}.dll"

    dist = mod_dir / "dist"
    staging = dist / "pkg"
    shutil.rmtree(staging, ignore_errors=True)
    staging.mkdir(parents=True)
    for name in PACKAGE_FILES[1:]:
        shutil.copy2(mod_dir / "package" / name, staging / name)
    (staging / "manifest.json").write_text(manifest)
    shutil.copy2(dll, staging / dll.name)

    archive = dist / f"{mod}-{version}.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as zf:
        for name in PACKAGE_FILES + [dll.name]:
            zf.write(staging / name, name)
    print(f"{mod}: {archive.relative_to(ROOT)}")


def main(names):
    if names:
        mod_dirs = [ROOT / name for name in names]
        for mod_dir in mod_dirs:
            if not (mod_dir / "package").is_dir():
                fail(mod_dir.name, "no package/ folder")
    else:
        mod_dirs = sorted(p.parent.parent for p in ROOT.glob("*/package/manifest.json"))
    for mod_dir in mod_dirs:
        package(mod_dir)


if __name__ == "__main__":
    main(sys.argv[1:])
