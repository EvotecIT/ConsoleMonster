#!/usr/bin/env python3
"""Refresh the embedded catalogue and shipped notices from the six upstream families.

Run from any directory with Python 3: python3 Build/refresh-schemes.py
The current upstream commit is recorded before reading its files. Review the JSON
diff and rerun the catalogue tests before accepting an update.
"""

import datetime
import json
from pathlib import Path
import re
import time
import urllib.error
import urllib.request


ROOT = Path(__file__).resolve().parents[1]
PALETTES = ROOT / "ConsoleMonster/Styling/Schemes"
NOTICES = ROOT / "Module/Notices/Schemes"
KEYS = (
    "black red green yellow blue purple cyan white brightBlack brightRed "
    "brightGreen brightYellow brightBlue brightPurple brightCyan brightWhite"
).split()
FAMILIES = {
    "Dracula": ("dracula/windows-terminal", "master", "LICENSE", "MIT"),
    "Nord": ("nordtheme/nord", "develop", "license", "MIT"),
    "Solarized": ("altercation/solarized", "master", "LICENSE", "MIT"),
    "Gruvbox": ("morhetz/gruvbox", "master", "README.md", "MIT"),
    "Catppuccin": ("catppuccin/windows-terminal", "main", "LICENSE", "MIT"),
    "Tokyo Night": ("folke/tokyonight.nvim", "main", "LICENSE", "Apache-2.0"),
}


def fetch(url, accept="application/vnd.github+json"):
    request = urllib.request.Request(url, headers={"User-Agent": "ConsoleMonster-scheme-refresh", "Accept": accept})
    for attempt in range(3):
        try:
            with urllib.request.urlopen(request, timeout=20) as response:
                return response.read().decode("utf-8-sig")
        except urllib.error.HTTPError:
            raise
        except (urllib.error.URLError, TimeoutError, OSError):
            if attempt == 2:
                raise
            time.sleep(attempt + 1)


def palette(name, variant, values, foreground, background, selection):
    return dict(name=name, displayName=name, variant=variant, **dict(zip(KEYS, values)),
                foreground=foreground, background=background, selectionBackground=selection)


def read_family(family, read):
    def windows(path, name, variant):
        # Tokyo Night publishes a JSON object preceded by shell-style comments.
        data = json.loads(re.sub(r"(?m)^#.*$", "", read(path)))
        result = dict(name=name, displayName=data["name"], variant=variant)
        for key in KEYS + ["foreground", "background", "selectionBackground", "cursorColor"]:
            if key in data:
                result[key] = data[key]
        return result

    if family == "Dracula":
        data = windows("dracula.json", "Dracula", "Dark")
        data["signatureAccent"] = data["blue"]
        return [data]
    if family == "Catppuccin":
        return [windows("mocha.json", "Catppuccin Mocha", "Dark"),
                windows("latte.json", "Catppuccin Latte", "Light")]
    if family == "Tokyo Night":
        return [windows("extras/windows_terminal/tokyonight_night.json", "Tokyo Night Dark", "Dark"),
                windows("extras/windows_terminal/tokyonight_day.json", "Tokyo Night Day", "Light")]
    if family == "Nord":
        colors = dict(re.findall(r"\$(nord\d+):\s*(#[0-9a-fA-F]{6})", read("src/nord.scss")))
        # Nord's terminal mapping: polar night, aurora and frost; nord8 is its signature frost accent.
        indexes = [1, 11, 14, 13, 9, 15, 8, 5, 3, 11, 14, 13, 9, 15, 7, 6]
        data = palette("Nord", "Dark", [colors[f"nord{i}"] for i in indexes],
                       colors["nord4"], colors["nord0"], colors["nord2"])
        data["signatureAccent"] = colors["nord8"]
        return [data]
    if family == "Solarized":
        # The documented table supplies ANSI indexes, including the nonstandard bright slots.
        rows = re.findall(r"(?m)^\s+(\w+)\s+(#[0-9a-fA-F]{6})\s+(\d+)/\d+\s+", read("README.md"))
        colors = {name: color for name, color, _ in rows}
        ansi = {int(index): color for _, color, index in rows}
        values = [ansi[i] for i in range(16)]
        return [palette("Solarized Dark", "Dark", values, colors["base0"], colors["base03"], colors["base02"]),
                palette("Solarized Light", "Light", values, colors["base00"], colors["base3"], colors["base2"])]
    if family == "Gruvbox":
        colors = dict(re.findall(r"let s:gb\.(\w+)\s*=\s*\['(#[0-9a-fA-F]{6})'", read("colors/gruvbox.vim")))
        # The upstream g:terminal_color_* mapping, with its default medium contrast.
        hues = "red green yellow blue purple aqua".split()
        result = []
        for variant, bg, fg, gray, bright in [("Dark", "dark", "light", "gray_245", "bright"),
                                               ("Light", "light", "dark", "gray_244", "faded")]:
            values = ([colors[bg + "0"]] + [colors["neutral_" + h] for h in hues] + [colors[fg + "4"], colors[gray]]
                      + [colors[bright + "_" + h] for h in hues] + [colors[fg + "1"]])
            result.append(palette("Gruvbox " + variant, variant, values, colors[fg + "1"], colors[bg + "0"], colors[bg + "2"]))
        return result
    raise ValueError(f"Unknown family: {family}")


def main():
    outputs = {}
    dracula_licence = None
    for family, (repo, branch, licence_path, licence) in FAMILIES.items():
        revision = fetch(f"https://api.github.com/repos/{repo}/commits/{branch}", "application/vnd.github.sha").strip()
        if not re.fullmatch(r"[0-9a-f]{40}", revision):
            raise ValueError(f"Invalid upstream revision for {family}")
        source = f"https://github.com/{repo}"
        sources = []

        def read(path):
            url = f"https://raw.githubusercontent.com/{repo}/{revision}/{path}"
            sources.append(url)
            return fetch(url)

        schemes = read_family(family, read)
        notice = read(licence_path)
        if family == "Dracula":
            dracula_licence = notice[notice.index("Permission is hereby granted"):]
        if family == "Gruvbox":
            # Upstream declares MIT/X11 in README; it has no standalone licence file.
            declaration = notice[notice.index("License\n"):].strip()
            notice = "Author: morhetz <morhetz@gmail.com>\n\n" + declaration + "\n\n" + dracula_licence
        for scheme in schemes:
            for key in KEYS + ["foreground", "background", "selectionBackground", "cursorColor", "signatureAccent"]:
                if key in scheme:
                    if not re.fullmatch(r"#[0-9a-fA-F]{6}", scheme[key]):
                        raise ValueError(f"Invalid {family} {key}: {scheme[key]}")
                    scheme[key] = scheme[key].lower()
        data = dict(family=family, source=source, revision=revision,
                    readDate=datetime.datetime.now(datetime.timezone.utc).date().isoformat(),
                    licence=licence, sources=sources, schemes=schemes)
        slug = family.replace(" ", "")
        outputs[PALETTES / (slug + ".json")] = json.dumps(data, indent=2, ensure_ascii=False) + "\n"
        outputs[NOTICES / (slug + ".txt")] = f"{family}\nSource: {source}\nRevision: {revision}\nLicence: {licence}\n\n{notice.rstrip()}\n"
        print(f"{family}: {revision}, {len(schemes)} scheme(s)")
    # No partial rewrite when an upstream read or validation fails.
    for path, text in outputs.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(text.encode("utf-8"))


if __name__ == "__main__":
    main()
