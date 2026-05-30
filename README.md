# Project Gaze

Project Gaze is a Unity 6 research prototype for gaze-driven interaction on a glasses-free 3D display. It combines:

- Lenovo ThinkVision 27 3D / AutoStereo display integration.
- 7Invensun A8 gaze tracking and blink confirmation.
- Screen-space and depth calibration.
- Depth-aware target matching for layered pages.
- Two formal task scenes with background experiment logging.

The codebase is organized so that hardware SDK calls stay behind project-owned bridge/provider boundaries. Most interaction, matching, logging, and model code can be tested without the real devices.

## Current Flow

The default subject-test flow is:

1. `SubjectTestFlowScene`
2. `CalibrationScene`
3. temporary continuous depth validation
4. `LayeredPagesScene` for 120 seconds
5. `DepthGatedAgentScene` for 120 seconds
6. automatic player quit

The two task scenes are also standalone scenes. Opening them directly does not redirect to calibration; they use stereo gaze when accepted calibration artifacts and hardware are available, otherwise they use mouse fallback.

## Scenes

| Scene | Runtime entry | Purpose |
| --- | --- | --- |
| `SubjectTestFlowScene` | `SubjectTestFlowController` | Full subject-test flow controller. |
| `CalibrationScene` | `CalibrationSceneBootstrap` | 7Invensun xy calibration, posture check, depth calibration, temporary continuous depth validation. |
| `LayeredPagesScene` | `LayeredPagesBootstrap` | Seven-layer spatial page selection task. |
| `DepthGatedAgentScene` | `DepthGatedAgentDemo` | Far-depth AI logo trigger and panel round-trip task. |

## Data Outputs

Generated experiment data is written under `ExperimentData/` and is ignored by git by default:

- `ExperimentData/depth-measurements/continuous-depth-validation-*.json/.csv`
- `ExperimentData/depth-measurements/runtime-depth-layer-{sceneId}-*.json/.csv`
- `ExperimentData/task-interactions/task-interactions-*.json/.csv`

`*-latest.json/.csv` files are overwritten each run for quick plotting and inspection.
Experiment data capture is enabled by default. It controls continuous depth
validation logs, runtime depth-layer logs, and task-interaction logs together.
In `SubjectTestFlowScene`,
the `SubjectTestFlowController` component exposes `Data Capture Mode` in the
Inspector: follow `PROJECT_GAZE_EXPERIMENT_DATA`, force logging on, or force
logging off. Set `PROJECT_GAZE_EXPERIMENT_DATA=0` before starting Unity to
disable those generated logs when the component is left in environment/default
mode.

## Hardware Notes

Real stereo validation requires:

- Windows 10/11.
- Lenovo ThinkVision 27 3D software/runtime.
- `Assets/StreamingAssets/Config.xml`.
- `Assets/Plugins/ThinkVision/AutoStereo/Plugins/WindowsDisplayAPI.dll`.
- 7Invensun A8 runtime files under `Assets/StreamingAssets/7ia8`.

Without the stereo display or accepted calibration artifacts, the formal scenes fall back to mouse input for development.

## Development

After opening the project in Unity and letting packages reimport, generated `.csproj` files can be used for quick local compile checks. If `Library/` has just been deleted, open Unity once before running these commands so Unity can restore package-cache references such as NUnit:

```powershell
dotnet build "ProjectGaze.Runtime.csproj" /p:BaseIntermediateOutputPath="CodexBuild/obj/runtime/" /p:IntermediateOutputPath="CodexBuild/obj/runtime/" /p:OutputPath="CodexBuild/bin/runtime/"
dotnet build "ProjectGaze.EditModeTests.csproj" /p:BaseIntermediateOutputPath="CodexBuild/obj/editmode/" /p:IntermediateOutputPath="CodexBuild/obj/editmode/" /p:OutputPath="CodexBuild/bin/editmode/"
```

The generated `CodexBuild/`, Unity `Library/`, `Temp/`, `Logs/`, IDE files, and local experiment data should not be committed.

## Documentation

- [Function architecture](docs/01-function-architecture.md)
- [Hardware SDK layer](docs/02-hardware-sdk-layer.md)
- [Calibration and adaptation layer](docs/03-calibration-adaptation-layer.md)
- [Interaction state machine layer](docs/04-interaction-state-machine-layer.md)
- [Custom algorithms and logging layer](docs/05-custom-algorithms-layer.md)
- [Release checklist](docs/06-release-checklist.md)

## License

This project is released under the MIT License. See [LICENSE](LICENSE).
