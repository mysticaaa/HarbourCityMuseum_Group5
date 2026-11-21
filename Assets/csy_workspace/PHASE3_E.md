# Phase 3 E delivery

Initial E base: main at a53edad. Main integration base: f22485b, including the updated six-step tutorial, quick menu, cabin and exhibit changes. The original dirty working directory was preserved; development uses a separate Git worktree. No fishing gameplay, existing exhibit placement, input bindings, video commentary or entrance animation was edited.

## Open and review

- `Assets/csy_workspace/Scenes/Phase3E_XR/Museum_261122_1.unity` is the XR review copy of the latest `Assets/261122/Museum_261122_1.unity`.
- `Assets/csy_workspace/Scenes/Phase3E_Desktop/Museum_261122_1.unity` is the same copy with the existing **Keyboard and Mouse Interaction** root enabled. Use the existing W/S movement, A/D turning and mouse click. It is a testing mode, not a replacement for headset testing.
- Menu: **CSY Tools > Phase 3 > Open integrated E scene**. To integrate into a later layout, open a safe scene copy and choose **Install E guides in current scene**. The menu is idempotent and refuses Play mode. Installation marks the scene dirty but does not save it. Inspect the three card positions before saving.
- Build Settings in upstream still points to the Phase 2 scene. The review copies do not change it. The release owner must explicitly choose the agreed release scene and document build/source correspondence.

## Implemented E scope

Three configurable stations cover the active helmet, active boarding boat and First Day display. Video screens retain HMN's existing playback and commentary. The rod retains its owner's tutorial and controls. All new guide content is an introduction; the First Day audio does not disclose the map answer.

The coordinator binds the active Hands Rig camera, not the inactive legacy rig. It chooses the nearest eligible station in the visitor's forward 60-degree cone with an unobstructed sightline, requires a 1.2-second dwell, uses separate entry/exit thresholds, and plays once per visit. Leaving the region resets the visit. A fixed region stays at the display when the helmet is held.

Cards expose **Replay**, **Stop**, and **Auto audio on / off** with the existing physical XRI ray/Activate and direct mouse-ray fallback. Automatic speech waits during video playback or other non-looping audio. While E speaks, looping sources are temporarily reduced to one quarter and then restored. E stops when the gallery is inactive, during boat entry/cabin sessions, loss of application focus, or disable. The active-scene gate handles transitions without changing teammates' transition code. Text remains an alternative when audio is off or occupied.

Cards share a fixed high-contrast style, 28-40 point UI fonts at a fixed world scale and top height of 1.75 m. Their positions are configurable on the adapter. They do not follow or cover the held helmet. The card does not use shrinking auto-size to fit long text; runtime checks flag content overflow. The tiny legacy hints are suppressed only for E's bound models, using a small `ExhibitInspectionHint.Source` association; disabling E restores their prior activation state.

The helmet retains the existing grab, rotation and return behaviour. Visitors observe the window and choose its function on the card. An incorrect choice explains what to observe and permits retry; a correct choice produces completion feedback; reset clears only this local observation task. The task can be completed without sustained Grip. It is a self-check, not geometric window detection or a validated learning measure. No new prop is added.

## Audio provenance

The three `Audio/Phase3*.wav` clips are prototype synthetic speech rendered with the local Windows speech synthesizer. They are not historical recordings or quotations. Scripts are in `Audio/Phase3Narration.json`. Cards provide written summaries, not synchronized captions. The helmet and boat are illustrative models; no specific historical provenance is asserted.

## Checks and limits

See `PHASE3_E_VALIDATION.txt` for the dated software-check record and the remaining walkthroughs. Unity runtime import, rendered layout, mouse button operation, executable and physical-headset checks are still pending because the local Editor could not obtain a valid license. Do not report static compilation as a successful Play session.

Menus **Check guide policies** and **Check running guide references and text** provide repeatable checks once Unity runs. The latter needs Play mode and the integrated scene. These are technical checks, not participant observations.

## Phase 3 evidence and evaluation

The group leader requested proximity introductions, more legible hints and a richer helmet interaction. This is a requirements source, not participant evaluation evidence. The user subsequently relayed three findings: small hints are hard to read, helmet operation is unclear, and automatic speech can misfire. See PHASE3_E_FEEDBACK.txt for their scope, changes and repeat tasks. No original participant identifiers, raw observation sheets or timings were supplied; no post-change results have been invented.

Suggested tasks for a consenting novice participant:

1. Enter, approach the helmet and explain what its controls allow, without a moderator demonstrating them.
2. Listen, stop and replay the introduction; also find the equivalent information without audio.
3. Inspect the window, answer the observation question, try a different answer and reset.
4. Walk to the boat, cancel boarding, enter and return; check whether gallery narration intrudes.

Record anonymous participant IDs, device/mode, source commit, prompts/help given, actions and short interview notes. Ask about visibility, discoverability, control over audio and physical effort. Obtain consent, allow withdrawal or stopping for discomfort, and collect only necessary data. Never ask a participant to move a physical chair based on a virtual seat. Actual safe reach and readability must be checked on hardware before participant use.

Use a finding register: `finding ID -> anonymous evidence -> code/theme -> requirement -> severity/benefit/effort -> change commit -> repeated task -> remaining limitation`. Keep contradictory observations. Individual attribution is provided by the E branch's commits and named instruments/analysis, not by commit counts. The final individual reflection is at most 750 words and must accurately separate implementation, evaluation contribution and limitations.

The review copies deliberately retain the upstream scene name in separate directories because existing teammate scripts use that name. Open and build only the chosen copy; do not put all same-named review scenes in the release scene list.

## Main integration

The E adapter is now installed in Assets/261122/Museum_261122_1.unity. Both review copies were refreshed from f22485b before adding E, retaining the new tutorial and XR bindings. E hides its controls and stops narration while the six-step tutorial or quick menu is displayed, without resetting the current visit. MuseumQuickMenu exposes one read-only IsOpen property for this purpose. Existing fishing gameplay and layout remain unchanged. Build Settings were not changed; select the agreed release entry explicitly.
