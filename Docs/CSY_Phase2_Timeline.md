# CSY Phase 2 timeline delivery — 2026-10-15

## Scope and handoff
This delivery covers the opt-in port-city story timeline. The follow-view UI and Ship-to-Museum button are explicitly deferred following CSY's latest instruction: the group leader's implementation has not yet been uploaded. Do not replace that implementation with another menu or claim it as original CSY code. When delivered, record the leader's original work and CSY's integration/maintenance separately.

## Repository check before implementation
- Working branch: csy-ui-story; baseline main: 3b836896fccccce3a12e716632952c108b7e21e7.
- GitHub main and the queried personal branches matched the locally recorded remote tips on 2026-10-15. No pull/reset was needed.
- No unmerged entries were found. Seven previously staged files and the earlier untracked UI work were retained. The seven staged paths do not overlap paths changed by teammates between c99ec59 and 3b83689.
- This timeline work adds CSY files, extends CSY's existing desktop-preview scene whitelist, and repairs an earlier CSY validation tool that used an unavailable OpenPreviewScene API (CS0117). That old tool is now manual-only and uses the supported additive scene API. The editor builder appends a CSY scene to Build Settings; it does not remove teammate scene entries or edit the source scene.
- Source integration is a copy of zxh_workspace/Scenes/Museum_Vensail_Cabin.unity. Future source updates require rebuilding and rerunning validation; copies do not automatically inherit later scene edits.

## P1 requirements -> implementation -> evidence
| Requirement | Decision / implementation | Verification |
|---|---|---|
| Short entry then free exploration | Station overview offers three chapters in any order; continue exploring remains available | Direct selection, close/reopen tests |
| Maritime work, migration, everyday life | Stories.json contains three individually sourced chapters with object-observation questions | Catalogue/source validation; visible content screenshots |
| Credible interpretation | Each source page separates real museum examples from the fictional museum and illustrative models | Source disclosure required for each chapter |
| Low dexterity and understandable controls | Reuse team's physical ray-hover + trigger buttons; large text; no dragging, quiz, forced chronology or countdown | Button callbacks and text-fit checks; device usability pending |
| A complete journey | Exhibition exploration -> optional timeline -> chapter -> source -> recap -> continue; markers persist across ship visits | Three ship-return regression cycles |
| Clear recovery | Disabled endpoint navigation; reopen retains markers; reset requires confirmation | Bounds/idempotence/reset-cancel tests |

The P1 proposal's primary audience is a team prioritisation/working assumption, rather than a universal-accessibility guarantee. It mentions a five-minute visit; the timeline itself is approximately one minute of optional reading. It does not enforce an overall five-minute limit. The latest isolated boat-only session also does not currently provide a hard five-minute end. The group must state and evaluate actual total duration rather than claim the timer is enforced.

## Story content and sources
The connecting idea is: **ships as working spaces -> people maintaining the port above/below water -> movement through ports -> everyday memories behind objects**. This connects both models without inventing a shared historical owner or vessel biography.

1. Work: London Museum, diving helmet DK86.21/2. https://www.londonmuseum.org.uk/collections/v/object-260946/helmet-diving-helmet/
2. Migration: Royal Museums Greenwich, History of the Windrush. https://www.rmg.co.uk/stories/ocean/history-empire-windrush
3. Everyday life: Royal Museums Greenwich, Oral history. https://www.rmg.co.uk/collections/our-collections-oral-history

The source links were already present in ZXH's project research. CSY reused them, rechecked the official pages on 2026-10-15, and wrote the timeline wording, observation questions and disclosures for this station. These are international examples; do not describe them as evidence about the fishing-boat model, a Hong Kong voyage, or a named Harbour City resident. Text is paraphrased. No museum images, quotations or recordings are copied. This iteration provides written interpretation; it adds no new narration or video.

## Files and responsibilities
- Resources/CsyTour/Stories.json: CSY content selection, questions and evidence/reconstruction boundaries.
- Scripts/StoryProgress.cs: independent progress state; pages viewed are not automatically marked read.
- Scripts/CSYTimelineStation.cs: overview, chapter navigation, source disclosure, recap, continue and confirmed reset.
- Scripts/Editor/CSYTimelineBuilder.cs: reproducible hardware-free preview and additive scene-copy integration.
- Scripts/Editor/CSYTimelineValidation.cs: behavioural and regression tests plus screenshots.
- Scenes/Timeline_CSY_Preview.unity: desktop preview.
- Scenes/Museum_CSY_Timeline.unity: latest teammate museum/boat scene plus CSY station.

Reused team work: ZXH WorldPanel and BoatTriggerAction implement the underlying physical button/trigger system; MuseumSession and the ship scene implement loading, movement, scene hiding and saved-pose return. hmn supplies the major exhibit/environment models. None of those implementations is newly attributed to CSY. Existing follow-menu work and seven staged changes predate this timeline delivery.

## How to run / demonstrate
1. Unity 2022.3.62f3: open CSY Tools > Timeline > Open desktop preview. Wait for compilation and press Play. Desktop preview skips automatic XR startup only for the CSY preview scene.
2. Click a chapter, read the question, open its source page, then return. Click Mark this chapter read to record a marker. Try another chapter in a different order.
3. Visit recap shows count only, not a learning score. Continue exploring leaves a small reopen station. Reset markers asks for confirmation and does not reset ship/helmet/session state.
4. For the integrated experience, open Museum_CSY_Timeline. It uses the existing XR rig and controller bindings; requires a working OpenXR runtime for ordinary headset Play. The CSY station is hidden aboard and reappears on return.
5. Initial station placement is fixed after startup, offset from the viewer. It does not follow the head. Final wall placement and target-device readability must be checked in the gallery layout before submission.

## Phase 2 individual evidence
The rubric allocates 3 marks to repository-evidenced significant, correct, attributable and integrated contribution and 1 to technical/design reflection (essay maximum 750 words). Commit count and file volume do not guarantee marks.

Use the named artefacts above to explain the actual task: translating the proposal's opt-in narrative into implemented behaviour, designing source-aware content, distinguishing read markers from learning, and verifying scene integration. Keep evidence of failed checks and repairs as well as passing outcomes. Describe AI assistance under course policy and be able to explain the data/state/UI separation and integration callbacks.

Before submission: review the focused diff, commit the CSY timeline files with their .meta and validated scenes, and merge through the team's process. Preserve earlier staged changes separately; do not use git add . or count teammate implementations as original work. The timeline is published separately on csy-ui-story; it is not a merge to main, a release build or a submitted assignment. Consult Git history for the actual commit. The personal reflection draft remains local.

## Validation evidence
Authoritative results: Assets/csy_workspace/Evidence/Timeline_validation.txt and Logs/CSY-Timeline-Headless-20261015.log and Logs/CSY-Timeline-Visual-20261015.log. Screenshots, if graphical execution succeeds, are in the same Evidence folder. Read the actual generated report; the existence of a validator is not proof of passing tests.

Still requires hardware/people: actual controller input, perceived comfort and text size, FPS, accessibility with intended users, narrative comprehension, and the final team submission build. Do not claim these from synthetic button callbacks.

### Executed results, 2026-10-15
- Unity 2022.3.62f3 compilation passed after repairing the older CSY validator's unsupported scene API.
- Headless and OpenGL graphical runs both passed the timeline callback/state/layout checks and three additive boat-return cycles.
- Four actual Unity-rendered page screenshots were inspected: overview, chapter, source and recap; no text clipping was observed.
- A batch persistence issue was found: the added Build Settings entry existed in memory but did not survive the test editor exit. The builder now persists only the appended CSY entry, preserving the prior four entries, and the saved file is checked separately.
- No real headset/controller events or participant study were performed. The callback tests explicitly invoke the physical button adapter; they do not prove actual hardware input.
