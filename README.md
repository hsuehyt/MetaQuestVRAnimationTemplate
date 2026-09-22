# Meta Quest VR Animation Template

A Unity template for creating sequential VR animation experiences on Meta Quest. It provides gaze-based navigation, timed scene transitions, and editable example scenes so you can build a presentation around your own animations.

The experience follows a five-scene sequence:

`Start Menu -> Scene 1 -> Scene 2 -> Scene 3 -> End Credits`

Current version: **v1.0.7**. See the [changelog](CHANGELOG.md) for version history.

## Features

- A head-gaze start menu that works without controller setup
- Three animation chapters with optional timed transitions
- Skip, menu, replay, and quit controls
- A reusable scene-flow controller
- Example procedural animation that can be replaced with Timeline, Animator, or video
- Rain with surface-impact ripples and an animated forest robot encounter
- A scrolling end-credit scene
- An OpenXR head-pose driver and URP rendering setup, with optional Meta SDK integration

## Scene overview

| Scene | Purpose |
| --- | --- |
| StartMenu | Gaze-operated Start and Quit buttons. |
| Scene1 | Rain, surface-impact ripples, and an animated placeholder with a repeating 12-second countdown. |
| Scene2 | A looping nighttime forest encounter with a giant articulated robot. |
| Scene3 | A life-size dancing character with baked belly jiggle and adjustable procedural fur; loops until Skip / Next. |
| EndCredits | Scrolling credits with Replay and Menu controls. |

## Requirements

- Unity `6000.3.23f1` (the project's recorded editor version)
- Android Build Support, Android SDK & NDK Tools, and OpenJDK installed through Unity Hub
- A Meta Quest headset in Developer Mode, or Meta XR Simulator

## First-time setup

1. Add this folder to Unity Hub and open it.
2. Let Unity restore the Unity packages.
3. Add **Meta XR All-in-One SDK** from Package Manager > My Assets. This manual step avoids registry/certificate differences between machines.
4. Use the included scenes. **Tools > VR Animation Template > Generate All Scenes** resets them to template defaults; if you use it, then run **Build Forest Encounter in Scene 2** from the same menu to restore the forest.
5. Open `Assets/VRAnimationTemplate/Scenes/StartMenu.unity`.
6. Press Play. Buttons are gray until gazed at, then turn blue immediately; hold your gaze for about one second to activate one.
7. Open **Meta > Tools > Project Setup Tool**, select the Meta/Android target, then choose **Fix All** and **Apply All**.
8. In **File > Build Profiles**, enable/switch to **Meta Quest**. On Unity versions without that profile, use Android.
9. Connect the headset and select **Build And Run**.

If you only need portable OpenXR and not Meta-specific features, you may skip the Meta SDK and configure OpenXR under **Project Settings > XR Plug-in Management**.

## Render pipeline setup

The project includes `Assets/QuestURP.asset` and its renderer, assigned under **Edit > Project Settings > Graphics > Default Render Pipeline**. Quality levels inherit this setting when their Render Pipeline Asset is None.

If materials appear pink, confirm that **Default Render Pipeline** references **QuestURP**. To create a replacement, use **Assets > Create > Rendering > URP Asset (with Universal Renderer)** and assign the resulting pipeline asset in Graphics settings. Keep the accompanying renderer asset.

The scene generator selects the active pipeline's default shader (or Standard for the Built-in pipeline) and updates existing generated materials. Generating all scenes overwrites the template scenes, so preserve any custom scene edits before regenerating.

## Scene1 rain

Scene1 includes a **Rain VFX** object that runs during Play mode. Its **Rain Effect** component controls the emission area, height, drops per second, velocity, surface layers, and ripple size/lifetime. The generator also includes this effect when rebuilding Scene1.

Drops create expanding, fading rings where they hit non-trigger 3D colliders, including the floor and animated placeholder. Add a Collider to imported surfaces that should receive rain impacts; a Rigidbody is not required. Rings follow moving surfaces. The effect uses a URP shader, a bounded pool, and one combined mesh without per-drop GameObjects. Profile on the target headset and reduce Drops Per Second if needed.

## Scene2 forest robot encounter

Scene2 places the viewer at human scale in a dense North American mountain conifer forest at night. An approximately **29-metre robot** rises above the surrounding canopy. The environment includes moonlight, fog, drifting ground mist, rocks, ferns, fallen timber, and synthesized wind and footfall audio.

The robot faces the viewer and plays the supplied `Look Around.fbx` mocap at 30% speed, with visible searchlight-eye beams. It stays in place and loops the look-around motion, blinking twice near the end of each cycle. Its metal parts follow an articulated joint hierarchy driven by a retargeted animation clip through `ForestRobotEncounter`; this sequence does not use a Timeline asset. Adjust **Motion Speed** on the **Forest Encounter** component to change playback speed.

**Chapter Director** repeats the approximately **21.11-second** robot motion cycle at the default speed. Its countdown resets when the mocap loop restarts, and changes with Motion Speed. Auto Advance is disabled; use **Menu** or **Skip / Next** to leave Scene2. The forward-facing VR UI and visible gaze reticle remain available throughout.

To regenerate the encounter, choose **Tools > VR Animation Template > Build Forest Encounter in Scene 2**. This replaces the forest root and regenerates its assets; preserve manual edits first. See the [Scene2 setup notes](Assets/VRAnimationTemplate/ForestEncounter/README.md) for detailed timing and implementation notes. The scene is a stylized procedural interpretation of the concept, with simulated volumetric beams; Quest performance and headset comfort still require device testing.

## Scene3 furry dancer

Scene3 contains a **2-metre** character in front of the viewer, playing an approximately **18.67-second** looping belly dance. Belly jiggle amount **2** is baked into the exported joints. Auto Advance is disabled, and the countdown restarts with every dance loop. Use **Skip / Next** or Menu to leave the scene.

The current fur preset targets **Quest connected to a PC**. It uses 2,000 opaque tube strands plus an undercoat, blue roots and skin, and magenta tips. Strands are 15–35 cm long and 30–50 mm in diameter, with zero taper. Automatic segmentation uses a 7.5 cm target, 2–8 segments, and four tube sides. Palms and soles are excluded using a surface mask.

Select **Dancing Fat Man > Fur** in the Hierarchy. The **Dancer Fur** Inspector exposes colors (including bare skin), strand count, length up to 100 cm, diameter, taper, randomness, and segment settings. Outside Play mode, edit these settings and click **Apply Fur Settings**, then save. The groom asset is shared: changing it affects other instances using the same asset. Applying updates the selected coat's preview; it does not automatically overwrite the reusable prefab.

Motion controls can be adjusted during playback. Defaults are inertia **0.9**, stiffness **0.5**, gravity **0.4**, with simulation and body collision enabled. **Reset Settings to Defaults** appears below **Collision volumes** and resets appearance and motion settings; click Apply Fur Settings to rebuild the coat afterward. The fitted collision volumes are preserved.

Fur follows the skinned body with damped particle motion. Collision uses fitted body volumes and root surface planes, so some penetration is possible; strands can pass through each other. Profile the result on your PC and headset before increasing strand count, length, segments, or tube sides. Standalone Quest performance has not been validated.

The character FBX, controller, materials, groom, preview mesh, and prefabs are under `Assets/VRAnimationTemplate/DancingFatMan`. Open the saved Scene3 directly to retain its current setup. The Tools menu's scene and character builders replace generated content, so preserve custom edits before using them.

## Adding your animation and setting scene duration

Each animation chapter has a `Chapter Director`. Scene1 contains an `Animation Placeholder`; Scene2 contains the forest encounter and Scene3 contains the furry dancer.

1. Delete or disable the example animation you want to replace.
2. Add your Timeline/Playable Director, Animator, video player, or other scene content.
3. Set **Chapter Director > Duration Seconds** to the desired chapter length.
4. Enable **Auto Advance** to load the next scene when the timer ends, or disable it to restart the countdown every loop while waiting for **Skip / Next** or a transition triggered by your sequence.

The Scene1 and Scene3 countdowns repeat at their configured Duration Seconds. Scene2 instead follows the robot mocap loop, beginning after its lead-in. For replacement animations, align Duration Seconds with the animation loop yourself. The existing Chapter Director.ContinueNow() method can advance explicitly.

Scene names are centralized in `VRSceneFlow.cs`. If you rename scenes, update the serialized scene list in each scene and its defaults in the script.

## Controls

- Head gaze: keep the center dot over a button until it activates.
- Keyboard testing: `N` next scene, `M` menu, `R` restart.
- The UI can also be replaced with Meta XR Interaction SDK ray, poke, or hand-tracking Building Blocks.

## Important Quest notes

- This template uses a lightweight OpenXR head-pose driver and gaze UI so it can run before controller prefabs are configured.
- For production controller or hand input, install Meta XR Interaction SDK and add the appropriate Meta Building Blocks.
- Keep shaders mobile-friendly, use baked lighting where possible, and test performance on the headset early.

