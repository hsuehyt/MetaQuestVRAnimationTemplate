# Meta Quest VR Animation Template

Version **1.0.4** adds a stylized nighttime mountain forest and giant articulated robot to Scene2. Its 30-second sequence includes scanning searchlight eyes, one step, viewer discovery, and blinking, then advances automatically to Scene3. The original chapter UI and gaze reticle remain in front of the viewer. Scene1 rain and earlier URP/menu fixes are included.

A small, editable Unity project template for a linear VR presentation:

`Start Menu -> Scene 1 -> Scene 2 -> Scene 3 -> End Credits`

The generated experience includes:

- A head-gaze start menu that works without controller setup
- Three timed animation chapters
- Skip, menu, replay, and quit controls
- A reusable scene-flow controller
- Example procedural animation that can be replaced with Timeline, Animator, or video
- A scrolling end-credit scene
- OpenXR and Meta XR Core SDK package references

## Requirements

- Unity `6000.3.10f1` or a compatible Unity 6 release
- Android Build Support, Android SDK & NDK Tools, and OpenJDK installed through Unity Hub
- A Meta Quest headset in Developer Mode, or Meta XR Simulator

## First-time setup

1. Add this folder to Unity Hub and open it.
2. Let Unity restore the Unity packages.
3. Add **Meta XR All-in-One SDK** from Package Manager > My Assets. This manual step avoids registry/certificate differences between machines.
4. Use the included scenes. **Tools > VR Animation Template > Generate All Scenes** resets them to template defaults; if you use it, then run **Build Forest Encounter in Scene 2** from the same menu to restore the forest.
5. Open `Assets/VRAnimationTemplate/Scenes/StartMenu.unity`.
6. Press Play. Look at a button for about one second to activate it.
7. Open **Meta > Tools > Project Setup Tool**, select the Meta/Android target, then choose **Fix All** and **Apply All**.
8. In **File > Build Profiles**, enable/switch to **Meta Quest**. On Unity versions without that profile, use Android.
9. Connect the headset and select **Build And Run**.

If you only need portable OpenXR and not Meta-specific features, you may skip the Meta SDK and configure OpenXR under **Project Settings > XR Plug-in Management**.

## Render pipeline setup

The project includes `Assets/QuestURP.asset` and its renderer, assigned under **Edit > Project Settings > Graphics > Default Render Pipeline**. Quality levels inherit this setting when their Render Pipeline Asset is None.

If materials appear pink, confirm that **Default Render Pipeline** references **QuestURP**. To create a replacement, use **Assets > Create > Rendering > URP Asset (with Universal Renderer)** and assign the resulting pipeline asset in Graphics settings. Keep the accompanying renderer asset.

The scene generator selects the active pipeline's default shader (or Standard for the Built-in pipeline) and updates existing generated materials. Generating all scenes overwrites the template scenes, so preserve any custom scene edits before regenerating.

## Adding your animation

Scene2 already contains the forest encounter. See [its setup notes](Assets/VRAnimationTemplate/ForestEncounter/README.md) for timing, regeneration, and rendering limitations. The character uses rigid meshes attached to joints. It is a procedural interpretation of the concept; standalone Quest performance still needs profiling.

Each template-generated chapter contains a `Chapter Director` and an `Animation Placeholder` object. In the included Scene2, the forest encounter replaces that placeholder.

- Delete or disable `Animation Placeholder`.
- Add a Timeline/Playable Director, Animator, video player, or your own scene objects.
- Set `Chapter Director > Duration Seconds` to the desired chapter length.
- Disable `Auto Advance` if the chapter should wait for the viewer to press **Continue**.

Scene names are centralized in `VRSceneFlow.cs`. If you rename scenes, update its default scene list or the serialized list in the Inspector.

## Scene1 rain

Scene1 includes a **Rain VFX** object that runs during Play mode. Its **Rain Effect** component controls the emission area, height, drops per second, velocity, surface layers, and ripple size/lifetime. The generator also includes this effect when rebuilding Scene1.

Drops create expanding, fading rings where they hit non-trigger 3D colliders, including the floor and animated placeholder. Add a Collider to imported surfaces that should receive rain impacts; a Rigidbody is not required. Rings follow moving surfaces. The effect uses a URP shader, a bounded pool, and one combined mesh without per-drop GameObjects. Profile on the target headset and reduce Drops Per Second if needed.

## Scene2 forest robot encounter

Scene2 places the viewer at human scale in a dense North American mountain conifer forest at night. An approximately **29-metre robot** rises above the surrounding canopy. The environment includes moonlight, fog, drifting ground mist, rocks, ferns, fallen timber, and synthesized wind and footfall audio.

The sequence starts automatically: the robot looks around with visible searchlight-eye beams, takes one step, then turns toward the viewer. It blinks twice, softens its beams, and tilts its head while looking at the headset position. Its metal parts are attached to an articulated joint hierarchy and animated procedurally by `ForestRobotEncounter`; this sequence does not use a Timeline asset.

**Chapter Director** is set to **30 seconds** with **Auto Advance** enabled, so Scene2 proceeds to Scene3 when the timer finishes. The original forward-facing VR UI and visible gaze reticle provide **Menu** and **Skip / Next** controls throughout the sequence. The chapter timer runs independently of the robot animation.

To regenerate the encounter, choose **Tools > VR Animation Template > Build Forest Encounter in Scene 2**. This replaces the forest root and regenerates its assets; preserve manual edits first. See the [Scene2 setup notes](Assets/VRAnimationTemplate/ForestEncounter/README.md) for detailed timing and implementation notes. The scene is a stylized procedural interpretation of the concept, with simulated volumetric beams; Quest performance and headset comfort still require device testing.

## Controls

- Head gaze: keep the center dot over a button until it activates.
- Keyboard testing: `N` next scene, `M` menu, `R` restart.
- The UI can also be replaced with Meta XR Interaction SDK ray, poke, or hand-tracking Building Blocks.

## Important Quest notes

- This template uses a lightweight OpenXR head-pose driver and gaze UI so it can run before controller prefabs are configured.
- For production controller or hand input, install Meta XR Interaction SDK and add the appropriate Meta Building Blocks.
- Keep shaders mobile-friendly, use baked lighting where possible, and test performance on the headset early.
