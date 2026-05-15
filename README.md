# Unity CCTV Stealth Game

Unity 6 sample project for a first-person stealth mini game with CCTV detection.

## Open

1. Clone or download this repository.
2. Open the folder in Unity Hub.
3. Use Unity `6000.3.13f1` or a compatible Unity 6 version.
4. Wait for scripts to compile.
5. In Unity, run `Tools > CCTV Starter > Create Stealth Mini Game`.
6. Select a wall, pillar, or large vertical surface.
7. Add wall-mounted cameras with `Tools > CCTV Starter > Create Placeable CCTV`.
8. Move and rotate each `Placeable_CCTV` root wherever you want it.
9. Press Play.

## Controls

- WASD: Move
- Mouse: Look
- R: Restart after clear/fail

## Integrating Into Another Unity Project

Copy only this folder into another Unity project:

```text
Assets/CCTVStarter
```

Then run:

```text
Tools > CCTV Starter > Create Stealth Mini Game
```

The generated map starts without CCTV cameras. Add them manually with:

```text
Tools > CCTV Starter > Create Placeable CCTV
```

Tip: select a wall before creating a CCTV. The tool will mount the camera on that wall face and aim it into the room.

To remove every CCTV from the open scene:

```text
Tools > CCTV Starter > Delete All CCTVs
```

For manual setup, see:

```text
Assets/CCTVStarter/INTEGRATION_GUIDE.md
```

Do not copy `Library`, `Logs`, `Temp`, or `UserSettings`.
