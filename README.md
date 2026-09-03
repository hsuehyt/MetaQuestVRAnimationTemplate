# Meta Quest VR Animation Template

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
4. In Unity, choose **Tools > VR Animation Template > Generate All Scenes**.
5. Open `Assets/VRAnimationTemplate/Scenes/StartMenu.unity`.
6. Press Play. Look at a button for about one second to activate it.
7. Open **Meta > Tools > Project Setup Tool**, select the Meta/Android target, then choose **Fix All** and **Apply All**.
8. In **File > Build Profiles**, enable/switch to **Meta Quest**. On Unity versions without that profile, use Android.
9. Connect the headset and select **Build And Run**.

If you only need portable OpenXR and not Meta-specific features, you may skip the Meta SDK and configure OpenXR under **Project Settings > XR Plug-in Management**.

## Adding your animation

Each generated chapter contains a `Chapter Director` and an `Animation Placeholder` object.

- Delete or disable `Animation Placeholder`.
- Add a Timeline/Playable Director, Animator, video player, or your own scene objects.
- Set `Chapter Director > Duration Seconds` to the desired chapter length.
- Disable `Auto Advance` if the chapter should wait for the viewer to press **Continue**.

Scene names are centralized in `VRSceneFlow.cs`. If you rename scenes, update its default scene list or the serialized list in the Inspector.

## Controls

- Head gaze: keep the center dot over a button until it activates.
- Keyboard testing: `N` next scene, `M` menu, `R` restart.
- The UI can also be replaced with Meta XR Interaction SDK ray, poke, or hand-tracking Building Blocks.

## Important Quest notes

- This template uses a lightweight OpenXR head-pose driver and gaze UI so it can run before controller prefabs are configured.
- For production controller or hand input, install Meta XR Interaction SDK and add the appropriate Meta Building Blocks.
- Keep shaders mobile-friendly, use baked lighting where possible, and test performance on the headset early.
