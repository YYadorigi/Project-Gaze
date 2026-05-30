# ExperimentData

This directory is the local output root for generated experiment logs.

The repository intentionally ignores generated files under this directory:

- `depth-measurements/*.json`
- `depth-measurements/*.csv`
- `task-interactions/*.json`
- `task-interactions/*.csv`

Runtime depth-layer files include the scene id, for example
`runtime-depth-layer-LayeredPagesScene-latest.csv` and
`runtime-depth-layer-DepthGatedAgentScene-latest.csv`, so the two formal task
scenes do not overwrite each other's latest diagnostics.

In `SubjectTestFlowScene`, the `SubjectTestFlowController` component exposes
`Data Capture Mode` in the Unity Inspector. It can follow
`PROJECT_GAZE_EXPERIMENT_DATA`, force logging on, or force logging off for a
subject-test run. Set `PROJECT_GAZE_EXPERIMENT_DATA=0` before launching Unity to
disable continuous depth validation logs, background runtime depth-layer logs,
and task-interaction logs when the component is left in environment/default
mode. Logging is enabled by default when the variable is unset.

Keep this README so the output location exists in a fresh clone, but do not commit subject data or local run logs.
