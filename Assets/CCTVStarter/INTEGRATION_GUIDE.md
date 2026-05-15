# CCTV Starter Integration Guide

## Recommended Merge Path

Copy this folder into the target Unity project:

```text
Assets/CCTVStarter
```

This keeps the feature self-contained. The included assembly definitions isolate the runtime scripts and editor-only scene builder from the rest of the project.

## Dependencies

Required Unity packages:

- `com.unity.ugui`
- `com.unity.inputsystem`
- `com.unity.render-pipelines.universal`

The original project uses Unity `6000.3.13f1`.

## Quick Test In A Merged Project

After copying the folder:

1. Wait for Unity to compile.
2. Open any scene.
3. Run `Tools > CCTV Starter > Create Stealth Mini Game`.
4. Press Play.

The menu creates a generated test map. It does not require existing scene objects.

## Manual Setup

For your own scene:

1. Add `CctvDetectionTarget` to the player.
2. Add `CctvDetector` to each CCTV object.
3. Add `CctvPatrol` if the CCTV should scan left/right.
4. Add `CctvViewVisualizer` if the player should see the red detection area.
5. Assign the player target to `CctvDetector`.
6. Set `Obstacle Mask` to the layer used by walls and cover.

## Important Notes

- CCTV objects stay in place. Only their view direction rotates.
- Do not copy Unity cache folders such as `Library`, `Logs`, `Temp`, or `UserSettings`.
- If the target project already has classes with the same names, keep this feature in its `CCTVStarter` assembly or rename the classes before merging.
