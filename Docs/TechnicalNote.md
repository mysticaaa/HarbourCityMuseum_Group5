# Phase 2 Technical Note — Harbour City Museum (Group 5)

## 1. Architecture Overview

The prototype follows a modular singleton-based architecture:

```
GameManager (session state, station progression)
    ├── ExperienceFlow (5-minute journey sequencing)
    ├── AudioManager (voiceover, ambient, SFX)
    ├── UIManager (HUD, tooltips, welcome/complete panels)
    └── StationBase[] (ship, timeline, helmet)

Interaction:
    RaycastSelector (controller ray + hover/select)
        └── InteractableObject (base for all interactable items)
                └── StationBase (base for station logic)
                        ├── ShipStation
                        ├── TimelineStation
                        └── HelmetStation
```

## 2. Core Scripts

### GameManager.cs
- Singleton managing overall session state
- Tracks station progression and timer
- Provides events for station enter/exit, session start/complete
- Supports reset for staff between visitors

### RaycastSelector.cs
- Casts ray from VR controller (SteamVR) or mouse (desktop fallback)
- Detects InteractableObject hits via Physics.Raycast
- Manages hover/select states with visual feedback (LineRenderer color change)
- Supports SteamVR_Action_Boolean for trigger input

### InteractableObject.cs
- Base class for all interactable museum objects
- Manages visual feedback (emissive highlight, material swap)
- Events: OnHoverEnter, OnHoverExit, OnSelect, OnDeselect

### StationBase.cs
- Abstract base for the three stations
- State machine: NotVisited → Visiting → Completed
- Auto-enter via trigger zone collider
- Integrates with GameManager for progression tracking

### ShipStation.cs
- Two interaction phases: orbit (360°) → interior (panorama/cut-away)
- Auto-rotates ship model; camera orbits at configurable distance
- Interior hotspots for cabin, seating, instruments, machinery
- InteriorMode enum for runtime switching between panorama and cut-away

### HelmetStation.cs
- Zoom-based inspection with component cycling
- States: Idle → ZoomingIn → Inspecting → ZoomingOut
- Highlights individual helmet components with emissive effect
- Shows info panel with component labels

### TimelineStation.cs
- Data-driven timeline with TimelineEntry struct array
- Creates UI entries dynamically from inspector-configured data
- Opt-in exploration: visitor selects entries to expand details
- Detail panel shows year, title, description, and image

### ExperienceFlow.cs
- Manages the 5-minute journey: Intro → Ship → Timeline → Helmet → Outro
- Configurable phase durations
- Auto-advance mode (disabled by default per client preference)
- Manual advance via AdvanceToNextPhase()

### AudioManager.cs
- Three-channel audio: voiceover, ambient, SFX
- Preset methods for common sounds (hover, select, station enter/complete)
- Volume control per channel

### UIManager.cs
- Welcome panel, HUD, tooltip, session complete screen
- Crosshair color states (normal/hover/select)
- Timer display subscribed to GameManager events
- Hint prompts for each station

### MuseumSceneBuilder.cs (Editor)
- Menu item: Harbour City Museum > Build Grey-Box Scene
- Creates room environment, three stations, lighting, player, and managers
- Uses primitive shapes as placeholders
- Auto-configures station references in GameManager

## 3. Design Decisions

### Ray-based selection (vs gesture/direct manipulation)
**Rationale**: The primary audience is older visitors and adults less comfortable with technology. Ray-based selection requires minimal physical dexterity and is the most familiar interaction pattern (analogous to a TV remote or laser pointer). This directly addresses the client's Q1 response about addressing barriers for people with low flexibility.

### Singleton pattern for managers
**Rationale**: The museum experience is a single-user, single-scene application. Singletons provide simple global access without the overhead of dependency injection. This is appropriate for a student prototype scope.

### Desktop fallback for testing
**Rationale**: Not all team members have consistent access to a Quest 3 headset. The RaycastSelector automatically falls back to mouse-based ray casting when SteamVR is not active, allowing testing on any machine.

### Modular station architecture
**Rationale**: Each station can be developed independently by different team members. The StationBase abstraction allows the GameManager and ExperienceFlow to manage stations without knowing their specific implementation details.

## 4. Known Limitations

1. **Ship interior undecided**: The panorama vs cut-away decision is pending. The code supports both modes via the InteriorMode enum, but only placeholder geometry exists.
2. **No 3D models**: All stations use Unity primitives as placeholders. Maya models need to be built and imported.
3. **No audio assets**: Audio references are empty. Need to record or source voiceover, ambient, and SFX clips.
4. **Timeline data empty**: The TimelineStation has no configured entries. Historical content needs to be researched and added.
5. **No accessibility alternatives**: Beyond ray-based selection, no additional accessibility features (subtitles, audio descriptions, high-contrast mode) are implemented yet.
6. **Single scene only**: No scene transitions or loading screens. The entire experience takes place in one scene.

## 5. Testing Plan

### Desktop testing (Week 1-2)
- All team members can test using mouse + keyboard
- Verify ray selection, station triggers, and flow logic
- Test timer and session reset

### VR testing (Week 2-3)
- Test on Quest 3 via SteamVR streaming
- Verify controller ray accuracy and comfort
- Test with classmates as anonymous visitors
- Record feedback for Phase 3 evaluation

### Acceptance criteria for Phase 2 submission
- [ ] Complete 5-minute journey runs without crashes
- [ ] All three stations are reachable and interactive
- [ ] Ray selection works on both desktop and VR
- [ ] Git repository has meaningful, attributable history from all members
- [ ] Technical note is complete
- [ ] Weekly activity record is maintained
