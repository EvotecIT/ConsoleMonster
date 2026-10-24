"""Pack/install the real template and compile/run every canonical gallery demo.

Run with Python 3 and a .NET 10 SDK: python3 Tests/Build/test_template_gallery.py
No test packages are required. All generated files and the template hive are
isolated beneath EVOTEC_SCRATCH_ROOT (or the system temporary directory).
"""

import json
import os
from pathlib import Path
import signal
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile


REPO = Path(__file__).resolve().parents[2]
TEMPLATE = REPO / "Templates/ConsoleMonster.Templates"
GALLERY = REPO / "Website/examples/gallery"
SLUGS = tuple(sorted(path.parent.name for path in GALLERY.glob("*/metadata.json")))


def normalized(data):
    """Allow CRLF/LF differences without normalizing any other source bytes."""
    return data.replace(b"\r\n", b"\n")


class TemplateGalleryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scratch = tempfile.TemporaryDirectory(
            prefix="consolemonster-template-", dir=os.environ.get("EVOTEC_SCRATCH_ROOT")
        )
        cls.addClassCleanup(cls.scratch.cleanup)
        cls.root = Path(cls.scratch.name)
        cls.env = dict(os.environ, MSBUILDDISABLENODEREUSE="1",
                       DOTNET_CLI_USE_MSBUILD_SERVER="0", DOTNET_NOLOGO="1")
        cls.package_hive = cls.root / "package-hive"
        for slug in SLUGS:
            if not (GALLERY / slug / "Program.cs").is_file():
                raise AssertionError(f"Missing canonical gallery program: {slug}")
        cls.sources = {slug: (GALLERY / slug / "Program.cs").read_bytes() for slug in SLUGS}
        cls.dotnet(
            "pack", str(TEMPLATE / "ConsoleMonster.Templates.csproj"),
            "-o", str(cls.root / "packages"),
            "--artifacts-path", str(cls.root / "template-artifacts"),
            "-p:UseSharedCompilation=false",
        )
        packages = list((cls.root / "packages").glob("*.nupkg"))
        if len(packages) != 1:
            raise AssertionError(f"Expected one template package: {packages}")
        cls.package = packages[0]
        cls.dotnet("new", "install", str(cls.package),
                   "--debug:custom-hive", str(cls.package_hive))

    @classmethod
    def dotnet(cls, *arguments):
        """Keep diagnostics and bound a failed CLI/child tree to five minutes."""
        command = ["dotnet", *arguments]
        process = subprocess.Popen(
            command, cwd=cls.root, env=cls.env, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT, text=True, start_new_session=os.name != "nt",
        )
        try:
            output, _ = process.communicate(timeout=300)
        except BaseException as error:
            if process.poll() is None:
                if os.name == "nt":
                    subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                                   check=True, capture_output=True)
                else:
                    os.killpg(process.pid, signal.SIGKILL)
            output, _ = process.communicate()
            if isinstance(error, subprocess.TimeoutExpired):
                raise AssertionError(f"Timed out: {command}\n{output}") from error
            raise
        if process.returncode:
            raise AssertionError(f"Exit {process.returncode}: {command}\n{output}")
        return output

    def generate(self, hive, slug=None, *, aot=False, suffix="", framework=None):
        name = "Gallery" + (slug or "Default").replace("-", "") + suffix
        output = self.root / name
        arguments = ["new", "consolemonster", "--debug:custom-hive", str(hive),
                     "--name", name, "--output", str(output)]
        if framework is not None:
            arguments.extend(["--Framework", framework])
        if not aot:
            arguments.append("--NoAot")
        if slug is not None:
            arguments.extend(["--demo", slug])
        self.dotnet(*arguments)
        self.assertEqual({p.name for p in output.iterdir()}, {"Program.cs", name + ".csproj"})
        return output, output / (name + ".csproj")

    def compile(self, project):
        """Use source references only in the disposable generated consumer."""
        tree = ET.parse(project)
        for reference in tree.findall(".//PackageReference"):
            name = reference.attrib["Include"]
            if name.startswith("ConsoleMonster"):
                reference.tag = "ProjectReference"
                reference.attrib.clear()
                reference.set("Include", str(REPO / name / (name + ".csproj")))
        tree.write(project, encoding="utf-8", xml_declaration=True)
        self.dotnet("build", str(project), "--artifacts-path", str(self.root / "app-artifacts"),
                    "-p:UseSharedCompilation=false")
        return self.root / "app-artifacts/bin" / project.stem / "debug" / (project.stem + ".dll")

    def test_package_programs_and_generated_demos(self):
        """The shipped package preserves source bytes and every demo is runnable."""
        with zipfile.ZipFile(self.package) as package:
            programs = [p for p in package.namelist() if p.endswith("/Program.cs")]
            self.assertEqual(len(programs), len(SLUGS) + 1)
            for slug in SLUGS:
                matches = [p for p in programs if p.endswith(f"/gallery/{slug}/Program.cs")]
                self.assertEqual(len(matches), 1, slug)
                self.assertEqual(package.read(matches[0]), (GALLERY / slug / "Program.cs").read_bytes())
        for slug in SLUGS:
            with self.subTest(slug=slug):
                output, project = self.generate(self.package_hive, slug)
                self.assertEqual(normalized((output / "Program.cs").read_bytes()),
                                 normalized((GALLERY / slug / "Program.cs").read_bytes()))
                tree = ET.parse(project)
                references = tree.findall(".//PackageReference")
                self.assertEqual([r.attrib["Include"] for r in references], ["ConsoleMonster"])
                self.assertEqual(references[0].attrib["Version"], "2.0.*")
                self.assertIsNone(tree.find(".//PublishAot"))
                assembly = self.compile(project)
                snapshot = self.dotnet(str(assembly), "--snapshot")
                self.assertTrue(snapshot.strip(), slug)
                self.assertEqual(snapshot, self.dotnet(str(assembly), "--snapshot"), slug)
                self.assertEqual(snapshot, self.dotnet(str(assembly)), slug)
                metadata = json.loads((GALLERY / slug / "metadata.json").read_text())
                last_step = str(metadata["steps"] - 1)
                self.assertTrue(self.dotnet(str(assembly), "--snapshot", "--step", last_step).strip(), slug)
        self.assertEqual(self.sources, {slug: (GALLERY / slug / "Program.cs").read_bytes() for slug in SLUGS},
                         "Canonical programs changed during validation; rerun against stable sources.")

    def test_default_template_and_options(self):
        """Omitting --demo retains the original menu, host dependencies and AOT."""
        output, project = self.generate(self.package_hive, aot=True)
        expected = (TEMPLATE / "content/ConsoleMonsterApp/Program.cs").read_bytes()
        self.assertEqual(normalized((output / "Program.cs").read_bytes()),
                         normalized(expected.replace(b"ConsoleMonsterApp", project.stem.encode())))
        tree = ET.parse(project)
        self.assertEqual(tree.findtext(".//PublishAot"), "true")
        self.assertEqual(tree.findtext(".//TargetFramework"), "net8.0")
        self.assertEqual([r.attrib["Include"] for r in tree.findall(".//PackageReference")],
                         ["ConsoleMonster", "ConsoleMonster.Extensions.Logging",
                          "ConsoleMonster.Hosting", "Microsoft.Extensions.Hosting"])
        _, explicit = self.generate(self.package_hive, "none", suffix="Explicit")
        self.assertEqual([r.attrib["Include"] for r in ET.parse(explicit).findall(".//PackageReference")],
                         [r.attrib["Include"] for r in tree.findall(".//PackageReference")])
        self.assertTrue(self.dotnet(str(self.compile(explicit))).strip())

    def test_staged_source_installation(self):
        """Folder installation uses the same MSBuild content links as packing."""
        staged = self.root / "source-template"
        self.dotnet("msbuild", str(TEMPLATE / "ConsoleMonster.Templates.csproj"),
                    "-t:PrepareTemplateSource", f"-p:TemplateSourcePath={staged}/")
        hive = self.root / "source-hive"
        self.dotnet("new", "install", str(staged), "--debug:custom-hive", str(hive))
        for slug in SLUGS:
            with self.subTest(slug=slug):
                output, project = self.generate(hive, slug, suffix="Source", aot=True, framework="net10.0")
                self.assertEqual(normalized((output / "Program.cs").read_bytes()),
                                 normalized((GALLERY / slug / "Program.cs").read_bytes()))
                tree = ET.parse(project)
                self.assertEqual(tree.findtext(".//TargetFramework"), "net10.0")
                self.assertEqual(tree.findtext(".//PublishAot"), "true")


if __name__ == "__main__":
    unittest.main(verbosity=2)
