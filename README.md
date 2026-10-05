# Harbour City Museum — Group 5 (COMP5424 Extended Reality)

An accessible VR museum experience for the Harbour City Museum, built with Unity, SteamVR, and Meta Quest 3.

## Project Overview

The prototype offers a 5-minute interactive VR visit with three stations:

1. **Ship Model** (primary) — 360° inspection + interior panorama or cut-away view
2. **Port-City Timeline** — Maritime work, migration history, and everyday life
3. **Diving Helmet** — Zoom and inspect construction, function, and history

Interaction is ray-based selection via controller, designed for older visitors and adults less comfortable with technology.

## Tech Stack

| Tool | Purpose |
|------|---------|
| Unity 2022 LTS | Game engine / VR scene |
| SteamVR Plugin | VR input and device management |
| Meta Quest 3 | Target headset (via SteamVR streaming) |
| Maya | 3D model creation |
| C# / VS Code | Scripting |
| Git | Version control |

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/mysticaaa/HarbourCityMuseum_Group5.git
cd HarbourCityMuseum_Group5
git checkout dev
```

### 2. Open in Unity

1. Open Unity Hub
2. Click "Open Project" and select the cloned folder
3. Wait for Unity to import assets and compile (first time may take a few minutes)

### 3. Build the grey-box scene (first time only)

1. In the Unity menu bar, click **Harbour City Museum > Build Grey-Box Scene**
2. Wait for the script to create all objects
3. Save the scene: **File > Save As...** → save to `Assets/Scenes/MainScene.unity`

### 4. Test without VR (desktop mode)

1. Open `MainScene`
2. Press **Play**
3. Controls:
   - **WASD** — Move
   - **Mouse** — Look around
   - **Left Click** — Select interactable objects
   - **Shift** — Sprint

### 5. Test with Quest 3

1. Ensure SteamVR is running
2. Connect Quest 3 via Link Cable or Air Link
3. Press **Play** in Unity
4. The ray selector will automatically switch from mouse to controller input

## Project Structure

```
Assets/
├── Scripts/
│   ├── Interaction/          # Ray selection, interactable base, station base, triggers
│   ├── Station_Ship/         # Ship model station logic
│   ├── Station_Helmet/        # Diving helmet station logic
│   ├── Station_Timeline/     # Timeline wall station logic
│   ├── Audio/                # Audio manager
│   ├── Utils/                # GameManager, UIManager, ExperienceFlow, desktop movement
│   └── Editor/               # Scene builder editor script
├── Models/                   # Maya-exported FBX models
├── Materials/                # Unity materials
├── Textures/                 # Texture maps
├── Prefabs/                  # Reusable prefabs
│   ├── Stations/
│   ├── UI/
│   └── Player/
├── Audio/
│   ├── Voiceover/           # Spoken narration clips
│   └── Ambient/             # Background ambient sounds
├── Scenes/                   # Unity scene files
├── SteamVR/                  # SteamVR plugin (do not modify)
├── SteamVR_Input/           # SteamVR input bindings
└── XR/                      # XR settings
```

## Team

| Member | Branch |
|--------|--------|
| Wang Jingyi | wjy_xr |
| Wan Yuhang | wyh_xr |
| Chen Siyu | csy_xr |
| Zhou Xinhao | zxh_xr |
| Huang Mina | hmn_xr |
| Zhang Zhixing | zzx_xr |

> **Note**: Task assignments are TBD — to be decided by the team. See `Docs/Phase2_GitWorkflow_and_TaskAssignment.md` for suggested task modules (P1–P6).

## Git Workflow

```
main          ← stable releases only (merge from dev at phase deadlines)
 └── dev       ← daily integration branch
      ├── wjy_xr
      ├── wyh_xr
      ├── csy_xr
      ├── zxh_xr
      ├── hmn_xr
      └── zzx_xr
```

### Rules

1. Never commit directly to `main`
2. Develop on your personal branch, merge to `dev` via Pull Request
3. At least one teammate must review your PR before merge
4. `dev` must always compile — fix merge conflicts immediately
5. Commit message format: `[Module] Description`, e.g. `[RaySelect] Implement ray-based selection logic`

## Phase 2 Timeline

| Week | Focus | Key deliverables |
|------|-------|------------------|
| Week 1 | Foundation & grey-box | Git setup, folder structure, ray selection, grey-box scene |
| Week 2 | Asset build & interaction | Ship model, helmet model, station interactions |
| Week 3 | Polish, test & deliver | Timeline wall, audio, classmate testing, technical note |

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Interaction method | Ray-based selection | Low-effort, familiar to primary audience (older visitors) |
| Ship interior | Panorama (default) / Cut-away (fallback) | To be decided Week 2 based on implementation complexity |
| Session length | 5 minutes max | Fits short supervised sessions in learning studio |
| Primary audience | Older / tech-uncomfortable adults | Per client clarification response |
| Auto-advance | Disabled (visitor-driven) | Visitor-experience manager wants freedom, not scripted feel |

## Known Issues

- Ship interior representation (panorama vs cut-away) is undecided
- No 3D models yet (using primitive shapes as placeholders)
- No audio assets yet
- Timeline data entries are empty
- SteamVR input bindings need to be configured per device

## License

This is a student project for COMP5424 Extended Reality at HKU. All content is fictional or uses placeholder material as permitted by the scenario description.
