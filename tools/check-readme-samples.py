#!/usr/bin/env python3
"""Compiles every ```csharp block in README.md against the library, so the documentation cannot drift.

Usage: python3 tools/check-readme-samples.py
"""
import pathlib
import re
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parent.parent
PROJECT = ROOT / "IpwBridge" / "IpwBridge" / "IpwBridge.csproj"

CSPROJ = f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <NoWarn>CS1998;CS0168;CS0219</NoWarn>
    <!-- The samples must also be valid in trimmed / Native AOT applications. -->
    <IsAotCompatible>true</IsAotCompatible>
    <EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="{PROJECT}" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.12" />
  </ItemGroup>
</Project>
"""


def main() -> int:
    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    blocks = [re.sub(r"^ {3}", "", b, flags=re.M) for b in re.findall(r"```csharp\n(.*?)```", readme, re.S)]

    usings = {
        "using Microsoft.Extensions.Configuration;",
        "using Microsoft.Extensions.DependencyInjection;",
        "using Microsoft.Extensions.Hosting;",
    }
    types, methods = [], []
    for index, block in enumerate(blocks):
        lines = [line for line in block.strip("\n").split("\n") if line.strip()]
        if all(line.startswith("using ") for line in lines):
            usings.update(lines)
        elif re.search(r"^\s*(\[JsonSerializable|public sealed (partial )?class)", block, re.M):
            types.append(block)
        elif "builder." in block:
            methods.append(f"static void Register{index}(HostApplicationBuilder builder)\n{{\n{block}\n}}")
        else:
            methods.append(f"static async Task Sample{index}(IMetazoApiClient client)\n{{\n{block}\n}}")

    source = "\n".join(sorted(usings)) + "\n\nnamespace ReadmeSamples;\n\n" + "\n".join(types)
    source += "\n\ninternal static class Samples\n{\n" + "\n\n".join(methods) + "\n}\n"

    with tempfile.TemporaryDirectory() as directory:
        path = pathlib.Path(directory)
        (path / "ReadmeSamples.csproj").write_text(CSPROJ, encoding="utf-8")
        (path / "Samples.cs").write_text(source, encoding="utf-8")
        result = subprocess.run(["dotnet", "build", str(path), "-nologo", "-v", "q"], capture_output=True, text=True)

    print(f"Compiled {len(blocks)} README samples.")
    if result.returncode != 0:
        print(result.stdout + result.stderr)
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
