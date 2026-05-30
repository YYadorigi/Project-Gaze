# Release Checklist

Use this checklist before publishing Project Gaze to GitHub.

## Required Files

- `README.md`
- `LICENSE`
- `.gitignore`
- `Packages/`
- `ProjectSettings/`
- `Assets/`
- `docs/`
- `ExperimentData/README.md`

Do not publish generated experiment records or Unity caches.

## Clean Generated Files

The following paths are local generated artifacts and are ignored by git:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `CodexBuild/`
- `.vs/`
- `.vscode/`
- generated `.csproj` / `.slnx`
- `*.lscache`
- generated files under `ExperimentData/`

If Unity or an IDE is open, some files under `Library/` or `Temp/` may be locked. Close Unity, Unity Hub, Visual Studio, and VS Code before deleting those folders.

## Verify After Cleanup

After deleting `Library/`, open the project in Unity once and wait for package import to finish. This restores package-cache references used by Unity-generated `.csproj` files, including NUnit for EditMode tests.

Then run:

```powershell
dotnet build "ProjectGaze.Runtime.csproj" /p:BaseIntermediateOutputPath="CodexBuild/obj/runtime/" /p:IntermediateOutputPath="CodexBuild/obj/runtime/" /p:OutputPath="CodexBuild/bin/runtime/"
dotnet build "ProjectGaze.EditModeTests.csproj" /p:BaseIntermediateOutputPath="CodexBuild/obj/editmode/" /p:IntermediateOutputPath="CodexBuild/obj/editmode/" /p:OutputPath="CodexBuild/bin/editmode/"
```

If EditMode build fails with missing `nunit.framework`, Unity has not restored `Library/PackageCache/com.unity.ext.nunit...` correctly. Reopen Unity and let package import complete before retrying.

If Unity batchmode crashes while initializing the AssetDatabase, open the project once in the normal Unity Editor and wait for import to finish. This is usually a local cache or package import state issue, not a source-code issue.

## Static Consistency Checks

Search before release for removed scene names, deleted shortcut keys, and obsolete calibration redirects across `Assets/Scripts`, `Assets/Tests`, `docs`, and `README.md`.

Expected result: no matches for obsolete runtime behavior.
