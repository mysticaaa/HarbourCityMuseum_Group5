# Change Log — csy_xr branch

## 2026-10-06: Initial prototype submission

**Submitted by**: Chen Siyu (csy_xr)
**Commits**: 4 (ae74d7f → 221b3cb → e0afa0d + this update)

---

## What was done

Built and submitted a complete grey-box prototype codebase for the Harbour City Museum VR experience. This is a **foundation / starting point** — all scripts compile and the architecture is in place, but 3D models, audio assets, and final UI are not yet included.

## Files added

### Core scripts (12 C# files)

| File | Purpose |
|------|---------|
| `Scripts/Utils/GameManager.cs` | Singleton session manager — tracks station progress, timer, session state. Provides events for station enter/exit, session start/complete |
| `Scripts/Utils/UIManager.cs` | Manages welcome panel, HUD, tooltips, timer display, crosshair, completion screen. Subscribes to GameManager events |
| `Scripts/Utils/ExperienceFlow.cs` | 5-minute journey flow controller — intro → ship → timeline → helmet → outro. Auto-advance disabled (visitor-driven) |
| `Scripts/Utils/DesktopMovementController.cs` | WASD + mouse look movement for testing without VR headset |
| `Scripts/Interaction/RaycastSelector.cs` | Ray-based selection using SteamVR controller input. Includes mouse fallback for desktop testing. LineRenderer visual feedback |
| `Scripts/Interaction/InteractableObject.cs` | Base class for all interactive objects — hover/select/deselect events, emission highlight |
| `Scripts/Interaction/StationBase.cs` | Abstract base class for all three stations — state machine (NotVisited → Visiting → Completed) |
| `Scripts/Interaction/StationTrigger.cs` | Trigger zone component — auto-enters station when player walks in |
| `Scripts/Station_Ship/ShipStation.cs` | Ship model station — 360° inspection + interior panorama/cut-away mode (undecided, both supported) |
| `Scripts/Station_Helmet/HelmetStation.cs` | Diving helmet station — zoom in/out state machine, component highlight rotation |
| `Scripts/Station_Timeline/TimelineStation.cs` | Timeline wall — data-driven UI generation from TimelineEntry array |
| `Scripts/Audio/AudioManager.cs` | Three-channel audio (voiceover / ambient / SFX) with preset methods |

### Editor tool

| File | Purpose |
|------|---------|
| `Scripts/Editor/MuseumSceneBuilder.cs` | Unity editor menu — one-click grey-box scene generation (room + 3 stations + lights + player + managers) |

### Documentation

| File | Purpose |
|------|---------|
| `README.md` | Project overview, setup guide, tech stack, project structure, team, Git workflow, Phase 2 timeline, design decisions, known issues |
| `Docs/TechnicalNote.md` | Architecture description, script details, design rationale, known limitations, test plan |
| `Docs/WeeklyActivityRecord.md` | 3-week activity log template (per member, per task, with commit references) |

### Folder structure

Created empty folder structure under `Assets/` with `.meta` files:
- `Scripts/` (Interaction, Station_Ship, Station_Helmet, Station_Timeline, Audio, Utils, Editor)
- `Models/`, `Materials/`, `Textures/`
- `Prefabs/` (Stations, UI, Player)
- `Audio/` (Voiceover, Ambient)
- `Scenes/`, `Timeline/`, `Resources/`

## How to use

1. Clone the repo, checkout `csy_xr` branch
2. Open in Unity Hub (Unity 2022 LTS)
3. Wait for compilation
4. Menu bar → **Harbour City Museum → Build Grey-Box Scene**
5. Press **Play** — use WASD + mouse + left click to test

## Architecture summary

```
GameManager (singleton)
  ├── ExperienceFlow (5-min journey controller)
  ├── UIManager (HUD, panels, tooltips)
  ├── AudioManager (3-channel audio)
  └── Station tracking
       ├── ShipStation ← StationBase ← InteractableObject
       ├── HelmetStation ← StationBase ← InteractableObject
       └── TimelineStation ← StationBase ← InteractableObject

RaycastSelector (player input → Physics.Raycast → InteractableObject.OnSelect)

StationTrigger (trigger zone → StationBase.OnPlayerEnter)
```

## Known limitations

- No 3D models — using Unity primitive shapes (cubes, spheres, cylinders) as placeholders
- No audio assets — AudioManager methods exist but clips are empty
- Timeline entries are empty — data structure exists, content needs to be filled
- Ship interior mode (panorama vs cut-away) is undecided — both modes are coded but not finalized
- SteamVR input bindings use default InteractUI action — may need custom bindings per device
- Scene must be built via editor menu (MuseumSceneBuilder) — not saved as a .unity file yet

## What needs to happen next

- Team meeting to assign P1–P6 task modules to specific members
- 3D models (ship, helmet, environment) from Maya → export as FBX → import to Unity
- Audio recording and sourcing
- Timeline content research and data entry
- Ship interior mode decision (panorama vs cut-away)
- Dev branch creation and PR-based integration
