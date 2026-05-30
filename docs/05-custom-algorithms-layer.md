# Custom Algorithms and Logging Layer

This layer contains the project-owned algorithms that sit between hardware samples and task-scene behavior.

## Depth Estimation

Depth estimation lives in `Assets/Scripts/Runtime/Gaze/Depth`.

Key types:

- `BinocularGazeSample`
- `GazeDepthFeatureExtractor`
- `DisparityDepthEstimator`
- `LinearSvrDepthEstimator`
- `CalibratedGazeDepthEstimator`
- `GazeDepthLayerProfile`
- `GazeDepthLayerResolver`

The calibrated estimator predicts continuous ray distance, then maps that distance to the nearest depth layer for discrete page matching.

## Seven-Layer Profile

The symmetric seven-layer profile is defined in `GazeDepthLayerProfile`:

- Near3
- Near2
- Near1
- Zero
- Far1
- Far2
- Far3

The layered-page scene uses all seven layers. The AI logo scene keeps normal pages in the Near2 through Far1 range and places the logo at Far3.

## Continuous vs Discrete Truth

The code distinguishes two truth sources:

- Continuous validation truth: injected ray distance from `InvensunA8DepthValidationRunner`.
- Discrete task truth: page-configured `DepthLayerRayDistance` from the hit page.

This distinction matters for thesis figures:

- continuous predicted depth vs injected depth
- continuous error vs injected depth
- injected nearest layer vs predicted nearest layer
- task success rate and layer consistency by scene

## Persistence Helpers

Shared helpers:

- `CsvPersistenceUtility`
- `SessionArtifactPersistenceUtility`
- `UnityObjectLifecycleUtility`

Depth persistence:

- `GazeDepthPersistence`
- `GazeDepthRuntimeLayerRecorder`
- `GazeExperimentDataCapture`

Task interaction persistence:

- `GazeTaskInteractionSceneLogger`
- `GazeTaskInteractionRecorder`
- `GazeTaskInteractionPersistence`
- `GazeTaskInteractionRecordFactory`

Runtime depth-layer logs are written as `runtime-depth-layer-{sceneId}-...` so each formal task scene has its own session and latest files. `GazeExperimentDataCapture` controls generated experiment logging, including continuous depth validation, runtime depth-layer, and task-interaction artifacts; it is enabled by default. In `SubjectTestFlowScene`, `SubjectTestFlowController.Data Capture Mode` can follow `PROJECT_GAZE_EXPERIMENT_DATA`, force logging on, or force logging off from the Unity Inspector. With the default environment mode, `PROJECT_GAZE_EXPERIMENT_DATA=0` disables capture before Unity starts. All generated experiment outputs are ignored by git. Keep only documentation and code in the repository.

Scene scripts use `GazeTaskInteractionSceneLogger` as a small no-op facade over
`GazeTaskInteractionRecorder`, so disabled data capture does not leak persistence
checks into task-scene business logic.

## Verification

The repository includes EditMode tests for:

- gaze viewport conversion
- ray projection and target matching
- depth target libraries
- depth measurement persistence
- task interaction persistence
- state machine transitions
- layout providers
- subject-test flow sequencing

When changing runtime behavior, build both runtime and EditMode projects before publishing.
