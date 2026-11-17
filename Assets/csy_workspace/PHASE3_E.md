# Phase 3 exhibit guidance

This first implementation is scoped to the helmet. It uses the existing XR grab and return behaviour. Visitors can use the fixed card without grabbing; grabbing changes its instruction to inspect the viewing window and release to return. Finding the window is an observation task, not an automatically scored event.

## Integration

- Use the agreed Phase 3 scene in a separate scene copy for the first review. In its Hierarchy select the helmet with `HelmetHandInspection`, then choose **CSY Tools > Phase 3 > Add guide to selected helmet**. The command binds the clip and grab object and selects the new guide. It does not save the scene. For an older local baseline, `CSYExhibitGuideSetup.BuildDemo` is the batch entry point that can save a separate demo scene.
- Position the card at the agreed visitor standing point, without obstructing scenery or the route. Use the scene's active XR camera as `Visitor` if the tagged `MainCamera` is ambiguous. Move the card as needed, then put its child `Fixed helmet range` back at the exhibit centre so the trigger area does not follow the held model.
- Defaults: enter within 1.6 m horizontally, dwell 0.6 s, exit beyond 1.85 m. Leaving stops playback and resets the visit, so re-entry can replay. The guide plays once per visit. Guide-managed clips do not overlap; existing cabin audio, video and background music are independent. Coordinate their volume with the layout/audio owner before release.
- The new card does not remove the legacy `ExhibitInspectionHint`. Check whether that text is duplicated in the release scene and coordinate a scoped suppression before replacing the old display. No shared scripts, existing scene files or Build Settings are changed by this implementation.
- For another exhibit, configure a new guide with its own clip, name, instruction and interpretation. Leave `Inspection Object` empty for exhibits without XR grabbing. Do not use the helmet recording for the ship. Check neighbouring guide radii against the walking route. No additional prop is required to test the helmet task.

## Narration

`Phase3Helmet.wav` is synthetic speech rendered using Windows speech synthesis for the prototype. Script: “Look for the viewing window on the diving helmet. The window lets a diver see while keeping water out of the helmet. If you prefer, read the exhibit card without picking up the model.” This explains a general function without assigning a historical provenance to the model. The card repeats the core interpretation in text.

## Phase 3 checks

Record the source commit, launch scene and test mode. In desktop or XR, check a quick pass, a sustained approach, standing in place after playback, leaving during playback, re-entry and a second nearby guide. On the headset, check card legibility at the approach point, whether the user can locate the window, and grab/release/return. Also verify that the guide is silent while away in the cabin and that existing controls still work. Desktop motion and headset input are separate checks.

For the report, link observed difficulty or interview evidence to the change and repeat the same task after integration. The scenarios above are a verification plan, not participant findings or results.
