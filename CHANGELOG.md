# Changelog

## 1.0.7

- Make every gaze-controlled button use the existing gray background when idle and blue background while gazed at, with an immediate color swap.
- Keep Scene1 and Scene2 running until Menu or Skip / Next is selected; their countdowns restart on each loop.
- Restart Scene3's countdown with its existing dance loop.
- Synchronize Scene2's countdown to the robot mocap cycle (about 21.11 seconds at the default speed), including changes to Motion Speed.
- Preserve the looping behavior when the template scenes or forest encounter are regenerated.
- Validation: scene settings, button links, and code changes checked; headset playback has not been verified.

## 1.0.6

- Replace Scene2's scripted robot stride with the supplied Look Around mocap, retargeted to the giant's rigid joints.
- Face the robot toward the viewer and loop the capture at 30% speed, approximately 21.11 seconds per cycle, with both feet planted.
- Correct inward finger curls and mirror finger order so the index fingers sit beside the thumbs; rebaking preserves the fixes.
- Repeat eye blinks each cycle and soften searchlight beams when aimed toward the viewer.
- Preserve the chapter UI and 30-second automatic transition to Scene3; extend the timer at startup if a slower playback speed needs more time.
- Include the source FBX, baked animation, mocap builder, and updated setup notes. Scene3's v1.0.5 content is retained.
- Validation: Unity Play mode verified mocap playback; 301 sampled poses and hand closeups verified corrected finger flexion, foot stability, facing direction, and looping. Headset performance remains untested.

## 1.0.5

- Replace Scene3's placeholder with a 2-metre looping belly-dance character in front of the viewer.
- Include the skinned FBX and animation controller, with belly jiggle amount 2 baked to joints.
- Add procedural tube fur, an undercoat shader, inertial strand motion, and approximate body collision.
- Expose fur colors, density, length, thickness, taper, automatic segments, and motion controls in the Inspector.
- Save the current blue/magenta 2,000-strand preset as defaults and move Reset Settings to Defaults below Collision volumes.
- Keep Scene3 looping with manual Menu and Skip / Next navigation.
- Document PC-connected Quest usage, shared groom editing, and collision/performance limitations. Headset performance remains unverified.

## 1.0.4

- Replace Scene2's placeholder with a stylized mountain forest and approximately 29-metre articulated robot.
- Add a 30-second scan, single stride, viewer discovery, and blinking searchlight-eye sequence.
- Include moonlight, fog, drifting ground mist, procedural materials, and synthesized wind and footfall audio.
- Preserve the original forward-facing chapter UI and visible gaze reticle; automatically advance to Scene3 after 30 seconds.
- Add a repeatable Scene2 forest builder and setup notes.
- Validation: Unity compilation, rendered previews, 121 animation samples, planted-foot stability, blinking, and final gaze checks passed. Final navigation settings were checked against Scene1 and the scene-flow order. Quest performance and headset comfort remain untested.

## 1.0.3

- Add falling rain streaks and expanding, fading impact ripples to Scene1.
- Detect impacts on non-trigger 3D colliders and attach ripples to moving surfaces.
- Include the rain runtime component, URP shader, and material, with adjustable emission and ripple settings.
- Include rain when regenerating Scene1 and document setup and performance controls.
- Validation: C# compilation and scene-reference checks passed; visual rendering and Quest performance remain unverified.

## 1.0.2

- Fix Scene1, Scene2, and Scene3 menus in world space so head movement can aim at buttons.
- Move the chapter gaze reticles onto separate camera-space canvases.
- Preserve the updated lower menu anchored Y position of -400 in all three chapter scenes and in the scene generator.
- Leave StartMenu and EndCredits unchanged.

## 1.0.1

- Include and assign the QuestURP pipeline and Universal Renderer assets, with Unity's associated URP settings and volume profile update.
- Choose generated material shaders using the active render pipeline instead of the presence of the URP package.
- Update existing generated materials to the selected shader during scene generation.
- Document render pipeline setup and pink-material troubleshooting.

