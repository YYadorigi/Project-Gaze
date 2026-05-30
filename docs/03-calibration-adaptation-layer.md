# Calibration and Adaptation Layer

Calibration code lives mainly in:

- `Assets/Scripts/Runtime/Calibration`
- `Assets/Scripts/Runtime/InvensunA8`
- `Assets/Scripts/Runtime/Gaze/Depth`

## Calibration Scene

`CalibrationScene` is an independent scene. `CalibrationSceneBootstrap` creates `InvensunA8CalibrationSceneController` when that scene is loaded.

The controller handles:

1. Mono display setup for xy calibration.
2. 7Invensun runtime start and callback registration.
3. Posture alignment gate.
4. 9-point screen-space xy calibration.
5. Score review and accepted artifact persistence.
6. Stereo setup for depth calibration when available.
7. Depth calibration dataset/model training.
8. Temporary continuous depth validation for thesis plotting.

## Explicit Scene Requests

`CalibrationSceneFlow` is explicit. A caller may request a scene after calibration with:

```csharp
CalibrationSceneFlow.RequestSceneAfterCalibration(sceneName);
```

After successful or skipped calibration, the requested scene is consumed and loaded. If there is no pending request, the calibration scene stays on its review/completion UI.

Task scenes do not automatically redirect to calibration.

## Depth Calibration

`InvensunA8DepthCalibrationRunner` collects fixed viewport/depth targets and trains a `GazeDepthModelBundle`.

Important artifacts:

- `depth-calibration-dataset.json`
- `depth-calibration-model.json`
- `coefficient.dat`
- `calibration-state.json`

These are written under `Application.persistentDataPath/7ia8-calibration`.

## Temporary Continuous Validation

`InvensunA8DepthValidationRunner` runs after model training. It injects continuous target ray distances and records:

- injected ray distance
- predicted ray distance
- signed/absolute error
- nearest injected/predicted layer
- layer match flag

Outputs are saved to:

- `ExperimentData/depth-measurements/continuous-depth-validation-*.json/.csv`
- `ExperimentData/depth-measurements/continuous-depth-validation-latest.json/.csv`

The validation pass can still run when experiment data capture is disabled, but
these JSON/CSV artifacts are only written when `GazeExperimentDataCapture` is
enabled.

This stage is intentionally marked temporary and can be removed after the thesis figure is finalized.

## Runtime Depth Layer Logs

`GazeDepthRuntimeLayerRecorder` records task-scene depth/layer diagnostics. These logs use page-configured depth layers as reference truth for discrete matching analysis, not continuous-depth ground truth.

Outputs are saved to:

- `ExperimentData/depth-measurements/runtime-depth-layer-{sceneId}-*.json/.csv`
- `ExperimentData/depth-measurements/runtime-depth-layer-{sceneId}-latest.json/.csv`

`LayeredPagesScene` and `DepthGatedAgentScene` maintain separate latest files, so one scene no longer overwrites the other's discrete depth diagnostics. For full subject-test runs, the `SubjectTestFlowController` component in `SubjectTestFlowScene` can force data capture on/off from the Inspector or follow `PROJECT_GAZE_EXPERIMENT_DATA`. Set `PROJECT_GAZE_EXPERIMENT_DATA=0` before launching Unity to disable continuous validation, background runtime depth-layer, and task-interaction logs when the component is left in environment/default mode.
