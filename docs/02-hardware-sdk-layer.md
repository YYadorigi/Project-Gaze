# Hardware SDK Layer

The hardware layer keeps vendor SDK details behind project-owned boundaries.

## ThinkVision 27 3D

Project code lives under `Assets/Scripts/Runtime/ThinkVision`.

| Type | Responsibility |
| --- | --- |
| `IThinkVisionDisplayBridge` | Small interface consumed by scenes and calibration code. |
| `ThinkVisionDisplayBridgeFactory` | Selects vendor, preview, or fallback bridge. |
| `ThinkVisionBridgeEnvironment` | Detects staged SDK files, Windows platform, and display names. |
| `ThinkVisionDisplayProbe` | Immutable probe result object. |
| `ThinkVisionDisplayUtilities` | Camera defaults, fullscreen setup, stereo scene scale helpers. |
| `ThinkVisionStereoRigHealth` | Validates `StereoCam` sub-cameras for depth calibration. |
| `ThinkVisionStereoActivator` | Starts SideBySide stereo mode after scene setup. |
| `ThinkVisionRuntimeDiagnostics` | Builds status text for the active bridge. |
| `ThinkVisionUrpPostProcessingGuard` | Disables URP features that can distort stereo output. |

The vendor SDK types are in the `AS3DPlugin` namespace, especially `StereoCam`, `StereoUI`, and `EyeTracking`. Project code should adapt them in the bridge layer instead of scattering vendor calls across task scenes.

## ThinkVision Modes

`ThinkVisionDisplayBridgeFactory.Create()` returns:

- `VendorThinkVisionDisplayBridge` when Windows, SDK files, and a ThinkVision 27 3D display are detected.
- `PreviewThinkVisionDisplayBridge` when SDK files are present but the real display path is unavailable.
- `FallbackThinkVisionDisplayBridge` when the SDK is absent or incomplete.

The previous diagnostics hotkeys have been removed for the final demo path.

## 7Invensun A8

Project code lives under `Assets/Scripts/Runtime/InvensunA8`.

| Type | Responsibility |
| --- | --- |
| `InvensunA8SdkInterop` | P/Invoke declarations for `aSeeX.dll`. |
| `InvensunA8DeviceRuntime` | Starts/stops the SDK, registers callbacks, and exposes latest samples. |
| `InvensunA8CalibrationSupport` | Loads accepted calibration artifacts for runtime use. |
| `InvensunA8RuntimeLayoutUtility` | Stages runtime files from `StreamingAssets/7ia8`. |
| `InvensunA8GazeTrackingProvider` | Converts SDK frames into `GazeTrackingSample`. |
| `InvensunA8BlinkDetectionProvider` | Converts blink/openness signals into confirm events. |

Runtime staging and accepted calibration artifacts are kept separate:

- Runtime staging: `Application.persistentDataPath/7ia8-runtime`
- Calibration artifacts: `Application.persistentDataPath/7ia8-calibration`

## Fallback Boundaries

For tests and development without hardware:

- Fake `IThinkVisionDisplayBridge` rather than fake vendor `StereoCam`.
- Fake `IGazeTrackingProvider` and `IBlinkDetectionProvider` rather than mock native callbacks.
- Use mouse fallback in task scenes when no accepted calibration model is available.
