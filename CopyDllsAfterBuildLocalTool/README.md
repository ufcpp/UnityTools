# CopyDllsAfterBuildLocalTools

A .NET Local Tools version of CopyDllsAfterBuild.
It works on multi-platform, Windows, macOS and Linux without installing dependencies.

https://www.nuget.org/packages/CopyDllsAfterBuildLocalTools

## Installation

3 step to use it on your project.

* Step1. Install as .NET Local Tool.

```shell
dotnet new tool-manifest
dotnet tool install CopyDllsAfterBuildLocalTool --version 0.1.0
```

> TIPS: See [Tutorial: Install and use \.NET local tools \- \.NET CLI \| Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use) for more details.

* Step2. Add PostBuild event to your csproj.

```csproj
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="dotnet tool run dotnet-copydllsafterbuild run &#45;&#45;project-dir $(ProjectDir) &#45;&#45;target-dir $(TargetDir)" />
  </Target>
```

* Step3. Generate CopySettings.json on same directory of csproj.

> TIPS: You can copy CopySettings.json from other project instead.

```shell
dotnet tool run dotnet-copydllsafterbuild init
```

* Step4. (Optional) Modify CopySettings.json if needed. You can check json is valid via `validate` command.

```shell
dotnet tool run dotnet-copydllsafterbuild validate
```

* Step5. Run `dotnet build` will work as like CopyDllsAfterBuild.

## TIPS

### Is there any way to show debug log?

Yes, set `COPYDLLS_LOGLEVEL` env before running `dotnet build`.

* `Warning`, `Error`, `Critical` only output if error happens.
* `Information` just output "Copy DLLs". (default)
* `Debug` output debug event log.
* `Trace` output detailed log.

```shell
COPYDLLS_LOGLEVEL=Debug && dotnet build
```

> Windows users, use `set COPYDLLS_LOGLEVEL=Debug`.

### Can I run tool as ConsoleApp?

Yes, you can install as .NET Global Tools.

```shell
dotnet tool install -g CopyDllsAfterBuildLocalTools --version 0.1.0
dotnet-copydllsafterbuild run --project-dir <ProjectDir> --target-dir <TargetDir>
```

### I'm developer, can I run tool as dotnet run?

Yes, you can run with `dotnet run`.

```shell
dotnet run --framework net5.0 -- init
dotnet run --framework net5.0 -- validate
dotnet run --framework net5.0 -- run --project-dir <ProjectDir> --target-dir <TargetDir>
```