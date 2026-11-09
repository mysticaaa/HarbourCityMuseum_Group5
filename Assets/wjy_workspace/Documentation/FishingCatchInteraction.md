# Fishing catch interaction — Museum_261122_1

The scene `Assets/261122/Museum_261122_1.unity` now uses the independent
`Assets/wjy_workspace/Prefabs/FishingRodCatchExhibit.prefab` station.
The original station prefab and original fishing controller remain unchanged.
The new station preserves the scene's shared XR Rig, camera, controllers and interaction manager references.

## Controls and behaviour

- The front of the rod pedestal displays **Start the Fishing Experience (7 Surprises!)** below the button instructions. The water basin is hidden until the rod is picked up, then fades in. Once the rod finishes returning to the stand, the basin stays visible for 5 seconds before fading out. Picking up the rod again cancels the countdown or reverses an in-progress fade.
- The enlarged white pedestal uses a fitted, non-interactive text area so long instructions wrap and stay on its front face.
- To teleport, aim either controller ray at a cyan floor ring and press that controller's **index Trigger** once. Rings are placed in front of the fishing rod, diving helmet, fishing boat, First Day exhibit, and the four video screens. The targeted ring turns green. The player lands in the center, facing the same direction. While holding the rod, Trigger still controls fishing.
- The **Museum Menu** is hidden by default. Open or close it with the left **Y**, right **B**, or keyboard **M**. The large **Resume** button closes the menu; the smaller **Quit** button is at the bottom. The old camera-following Quit canvas is disabled at runtime in this scene. While the menu is open, fishing and teleport inputs do not compete with its buttons.
- Aim at the rod and hold **Grip** to pick it up and stop its display rotation.
- While holding Grip, hold **Trigger**, swing, then release Trigger to cast into the pool.
- The float is absent on display, in the hand and during flight. On water entry, it appears upright and bobs. It smoothly drifts toward random points inside the pool, at 0.14 metres per second, with a margin from the edges. The fishing line and submerged hook follow it. Its drift uses a separate random stream from the bite roll.
- On each water entry, the cast samples one random wait of **1–3 seconds**. At the end of that wait, it makes one independent **80%** bite check. The wait is sampled once per cast and does not change while waiting. This probability does not guarantee that a fixed number of casts will succeed. The separate **3-second retrieval window after a bite** remains unchanged.
- On a bite, the float shakes horizontally and vertically, the rod model shakes, and the held controller receives short haptic pulses. The catch is randomly selected, with equal probability among the kinds remaining in the current round, from **fish, turtle, crab, Coca-Cola glass bottle (trash), lobster, seal and miniature whale**.
- Each successfully retrieved kind is removed from the round. After all seven have been collected, the next bite starts a fresh round with all seven kinds available. Failed bites, early empty retrievals and escaped catches do not consume a kind. Trigger reeling and the existing Grip auto-return both count as retrieval; the same reward cannot be counted twice. Round progress lasts for this running rod instance and is not saved between play sessions.
- Press **Trigger** while the hook is in the water to reel in. A successful bite brings the selected model back at the hook. All seven models are small exhibit-sized representations; the whale is intentionally miniature. Reeling before the bite check catches nothing.
- You must start reeling **within 3 seconds after the bite**. Otherwise the catch escapes, the shaking stops, and the panel displays **The fish is gone. Try again!** in large, bold red text. Reeling after escape brings back an empty hook. Starting a new cast clears the warning and resets the panel style.
- A failed check displays exactly: **Fish don’t seem to like you. Try again.**
- After reeling, hold Trigger and swing again to start another cast. This clears the previous fish and message.
- Releasing Grip returns the rod to its exhibit, cancels an unfinished attempt and preserves an already caught fish on the hook until the next cast.

## Implementation

- The fishing exhibit prefab owns the teleport marker and input routing. It uses the existing controller rays and Unity XR Interaction Toolkit's `TeleportationProvider` for the move. While the marker is active, the left and right controller rays keep their normal reach instead of retracting when aiming at the floor. The marker snaps to the floor at play time and adds no collider to the walkway. Nearer geometry and UI still block the ray; a blocked landing prevents teleportation.
- `Scripts/FishingRod/WjyFishingRodCatchInteraction.cs`: independent catch state machine, timed probability, float movement, haptics, reeling and line routing.
- `Scripts/FishingRod/WjyFishingCatchVisuals.cs`: locally generated float/fish geometry and a camera-facing result panel. All model creation code is inside this workspace. No permanent float is added to the rod model.
- `Scripts/FishingRod/WjyFishingCatchCatalog.cs`: loads the seven downloaded model prefabs from `Resources/WjyFishingCatches`, assigns catch labels and displays exactly one at the hook. The earlier procedural catalog remains only as a fallback if model resources are missing.
- `Models/RealisticCatches/Source~/`: seven original GLB downloads; this folder is ignored by Unity to avoid importing the originals a second time.
- `Models/RealisticCatches/Prepared/`: Unity meshes, adapted materials and textures used by the prefabs.
- `Models/RealisticCatches/ATTRIBUTION.md` and `SOURCES.json`: model authors, source links, CC BY 4.0 licenses and source SHA256 checksums. Preserve attribution when sharing or distributing.
- `Models/RealisticCatches/Preview.jpg`: Unity-rendered previews of all seven replacements.
- `Editor/Tools/ConvertRealisticCatches.py` and `Editor/WjyRealisticCatchBuilder.cs`: offline conversion/preparation utilities; the editor builder is guarded to run only in an isolated validation project and never runs automatically in the museum project.
- `Prefabs/FishingRodCatchExhibit.prefab`: copied station with the new controller; its pool, rod, hook, guides and exhibit layout are preserved.
- `Editor/WjyFishingCatchValidation.cs`: fixture for a disposable isolated Unity project, with a guard preventing it from running in the museum project.

## Validation

Compiled against the project's installed Unity 2022.3.62f3 / XR Interaction Toolkit assemblies.
The isolated Unity fixture passed **137 checks**, covering ballistic water entry,
float visibility and orientation, independently sampled 1–3 second waits, no premature result, both seeded outcomes of the actual
80% random check (including rolls between 50% and 80%), no rerolls after resolution, line connectivity, rod shaking,
the exact failure message, successful/unsuccessful reeling, early cancellation and disable cleanup.
Additional checks cover all seven models, the three-second escape deadline and its red warning,
inability to retrieve escaped catches, randomized movement within the pool, independence of
drift from the bite random stream and warning reset on the next cast. Seven additional checks confirm that the downloaded prefabs, rather than the procedural fallback, are loaded. Two complete rounds of actual casts/retrievals verify seven distinct rewards per round, automatic refill, no consumption before retrieval and no double consumption of a returned reward. GPU previews were inspected during model preparation; the probability/round update was verified headlessly. Preparation removes the crab's online display floor and adjusts scale, orientation and hook anchors; no original animations are used.
This is software validation without a connected headset; Quest controller feel and haptic comfort still require a headset play test.

The initial integration replaced only the target scene's fishing station prefab reference.
The drift, escape and six extra models update only files inside `Assets/wjy_workspace`;
they use that existing reference without modifying the scene again. No shared rig prefab,
project setting or dependency manifest was modified.
