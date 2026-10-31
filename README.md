# Meta Quest VR Animation Template

A Unity template for creating sequential VR animation experiences on Meta Quest. It provides gaze-based navigation, timed scene transitions, and editable example scenes so you can build a presentation around your own animations.

The experience follows a six-scene sequence:

`Start Menu -> Scene 1 -> Scene 2 -> Scene 3 -> Scene 4 -> End Credits`

Current version: **v1.0.8**. See the [changelog](CHANGELOG.md) for version history.

## Features

- A head-gaze start menu that works without controller setup
- Four animation chapters with optional timed transitions
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
| Scene4 | Storm ocean with a pale-sailed wooden pirate ship, wave-driven motion, and irregular hull-impact foam and spray; includes Menu / Next controls. |
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

A looping rain scene with surface-impact ripples and an animated placeholder. See the [Scene1 notes](docs/Scene1.md) for rain controls, collider setup, and performance guidance.

## Scene2 forest robot encounter

A looping nighttime forest encounter with a giant robot performing a look-around animation. See the [Scene2 notes](Assets/VRAnimationTemplate/ForestEncounter/README.md) for motion controls, timing, regeneration, and validation details.

## Scene3 furry dancer

A looping belly-dance scene with a life-size character and adjustable animated fur. See the [Scene3 notes](docs/Scene3.md) for fur settings, motion controls, asset locations, and performance guidance.

## Scene4 ocean storm

A wooden pirate ship with pale sails rides a stormy sea beneath a foggy sky, surrounded by rain, lightning, and irregular wave-impact spray. See the [Scene4 notes](Assets/VRAnimationTemplate/OceanStorm/README.md) for weather controls, ship setup, and validation details, including the successful Quest 3 PC Link test.

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

## Student starter and showcase

Start with the [student starter release](https://github.com/hsuehyt/MetaQuestVRAnimationTemplate/releases/tag/student-v1.0.2.1) for the three original animation placeholders and updated gaze UI. This **v1.0.8 showcase** demonstrates how the same chapter framework can grow into rain effects, a giant forest robot, an animated furry dancer, and a storm ocean with a pirate ship.
