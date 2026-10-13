# Museum Quest 3 interactions

Uses the existing XR Interaction Toolkit 2.6.5 action-based controllers, ray interactors and OpenXR. No Meta SDK is required for these controller interactions.

## Controls

- Aim either controller ray and hold the index trigger on the helmet. The model moves to the controller with a short easing transition. Move and turn the controller to examine it. Release the trigger to return the model to its saved display pose in 0.45 seconds. Rotation pauses throughout grabbing and return; throwing and gravity are disabled.
- Aim at an actual mesh surface on the ship and press the index trigger. The selected point is marked yellow and brought to the display center while the ship enlarges to 2x in 0.35 seconds. The camera is never moved. Release and press again on the ship to restore its pose, or wait 12 seconds. Automatic rotation resumes after reset.
- Both exhibit rotation pivots start at zero rotation. The imported models retain their -90 degree X correction. Exhibits rotate at 15 degrees/second while idle.

Runtime setup maps existing controller Select actions to their existing trigger Activate actions. Consequently other ray-selectable objects in Museum also use the trigger during this scene. Grip bindings in the shared input asset are not edited. Force Grab is enabled on existing rays. This implementation targets Quest Touch controllers, not bare-hand pinch input.

## Configuration and files

- Scene: Assets/Scenes/Museum.unity, ZXH_HelmetRotationPivot / QuestExhibitSetup, with explicit helmet and ship references.
- New scripts: Assets/zxh_workspace/Scripts/Interaction.
- ZXH > Quest 3 > Configure Museum OpenXR configures Android and PC OpenXR settings, Touch / Touch Plus profiles, Android Meta Quest support, ARM64, IL2CPP, and adds Museum to Build Settings. It also runs once per editor session after the scripts load. Newly created XR manager assets live in zxh_workspace/QuestSettings; shared XR settings necessarily remain in the existing project settings store.
- Install Android Build Support with SDK, NDK and OpenJDK for Unity 2022.3.62f3 before making a Quest standalone build. For PC testing, connect Quest Link and use the Meta runtime as the PC OpenXR runtime.

## Verification before release

Verified in Unity 2022.3.62f3 batch Play Mode using QuestInteractionValidation.Run:

- Scene references, upright ship bounds, runtime interaction components and ship mesh colliders.
- Simulated ray grab, movement, release, re-grab during return, restored helmet parent/position/rotation/scale and resumed rotation.
- Actual Physics collider ray hit on the ship, 2x zoom, clicked point at display center, complete pose reset and resumed rotation.
- Repeated detail inspection and the 12 second timeout reset.
- Runtime and Editor C# compilation. The two pre-existing missing empty components on Left Controller and ShipModel_Placeholder were removed.

Museum's explicit controller XR Origin is activated by setup; cameras outside it and the desktop movement behavior are disabled in Play Mode. Interactors and interactables are assigned to one XR Interaction Manager. This replaces the previous inactive controller rig and prevents mismatched managers from blocking selection.

This computer lacks Android Build Support. The configuration tool now reports that limitation without throwing an exception, configures PC OpenXR, and skips unavailable Android OpenXR features. Install the module and run the configuration menu on the build computer before producing an APK. No Quest hardware input, stereo output or Android/IL2CPP player build was validated.

Hardware testing is still required:

1. Open Museum in Unity 2022.3.62f3 and check Console for errors. Confirm OpenXR initialization and tracked left/right rays on Quest.
2. Grab helmet from each hand at a distance. It should move near the hand, retain a useful orientation, pause rotation, and return on release without falling or throwing.
3. Release and re-grab during the return animation. Release again and confirm it returns to its original display parent and pose.
4. Select bow, stern and deck surfaces. Confirm the yellow point matches the ray hit and that each point moves to the enlarged display center.
5. Release then click again to reset; also test timeout. Repeat from both hands and verify no accumulated scale, position or rotation drift.
6. Test Quest Android build for stereo rendering, ray hit accuracy, readable labels, collider cost and frame rate. PC compilation does not establish on-device performance or Android build success.

The enlarged ship stays in the exhibition area rather than moving the visitor's head/camera. Inspect its enlarged footprint in the actual cabinet and adjust magnification if it overlaps nearby exhibits. No APK has been built or installed by this change.

## Sharing with teammates

Commit the full zxh_workspace directory with its .meta files together with Museum.unity, the changed shared XR/OpenXR settings, XRI interaction layer settings, EditorBuildSettings.asset and ProjectSettings.asset. Uploading the workspace scripts alone does not transfer the scene references or platform configuration. Review pre-existing Packages changes separately; they were present before this interaction work.

Run the repeatable batch validation after closing the interactive Editor:

Unity.exe -batchmode -nographics -projectPath <project-folder> -executeMethod QuestInteractionValidation.Run -logFile <log-file>

Do not add -quit: validation exits only after the asynchronous Play Mode checks finish. Successful logs contain ZXH TEST PASS lines for helmet return, ship zoom/reset and automatic timeout. This simulated test deliberately disables XR initialization only for the test session and restores PC XR initialization afterwards.
