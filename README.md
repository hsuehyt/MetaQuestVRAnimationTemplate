# MetaQuest-VR-Animation-Template

This student starter is based on **v1.0.2**, with the latest gray/blue gaze feedback and repeating chapter timer backported. The original three scenes and their animation content are preserved; Scene4 and later demo content are not included.

Version **1.0.2** fixes the chapter menus in world space for head-gaze selection and lowers their menu panels. The configured Universal Render Pipeline (URP) setup from 1.0.1 is included.

A small, editable Unity project template for a linear VR presentation:

`Start Menu -> Scene 1 -> Scene 2 -> Scene 3 -> End Credits`

The generated experience includes:

- A head-gaze start menu that works without controller setup
- Three continuously animated chapters with repeating 12-second countdowns
- Skip, menu, replay, and quit controls
- A reusable scene-flow controller
- Example procedural animation that can be replaced with Timeline, Animator, or video
- A scrolling end-credit scene
- OpenXR and Meta XR Core SDK package references

## Requirements

- Unity `6000.3.23f1` (the recorded project version)
- Android Build Support, Android SDK & NDK Tools, and OpenJDK installed through Unity Hub
- A Meta Quest headset in Developer Mode, or Meta XR Simulator

## First-time setup

1. Add this folder to Unity Hub and open it.
2. Let Unity restore the Unity packages.
3. Add **Meta XR All-in-One SDK** from Package Manager > My Assets. This manual step avoids registry/certificate differences between machines.
4. Use the included scenes. **Generate All Scenes** rebuilds the template and overwrites scene edits; it is not required to get started.
5. Open `Assets/VRAnimationTemplate/Scenes/StartMenu.unity`.
6. Press Play. Buttons are gray when idle and immediately turn blue when gazed at. Hold your gaze for about one second to activate one.
7. Open **Meta > Tools > Project Setup Tool**, select the Meta/Android target, then choose **Fix All** and **Apply All**.
8. In **File > Build Profiles**, enable/switch to **Meta Quest**. On Unity versions without that profile, use Android.
9. Connect the headset and select **Build And Run**.

If you only need portable OpenXR and not Meta-specific features, you may skip the Meta SDK and configure OpenXR under **Project Settings > XR Plug-in Management**.

## Render pipeline setup

The project includes `Assets/QuestURP.asset` and its renderer, assigned under **Edit > Project Settings > Graphics > Default Render Pipeline**. Quality levels inherit this setting when their Render Pipeline Asset is None.

If materials appear pink, confirm that **Default Render Pipeline** references **QuestURP**. To create a replacement, use **Assets > Create > Rendering > URP Asset (with Universal Renderer)** and assign the resulting pipeline asset in Graphics settings. Keep the accompanying renderer asset.

The scene generator selects the active pipeline's default shader (or Standard for the Built-in pipeline) and updates existing generated materials. Generating all scenes overwrites the template scenes, so preserve any custom scene edits before regenerating.

## Adding your animation

Each generated chapter contains a `Chapter Director` and an `Animation Placeholder` object.

- Delete or disable `Animation Placeholder`.
- Add a Timeline/Playable Director, Animator, video player, or your own scene objects.
- Set `Chapter Director > Duration Seconds` to the desired chapter length.
- `Auto Advance` is disabled by default: the countdown repeats and the animation continues until **MENU** or **SKIP / NEXT** is selected. Enable it for a timed transition.

Scene names are centralized in `VRSceneFlow.cs`. If you rename scenes, update its default scene list or the serialized list in the Inspector.

## Controls

- Head gaze: keep the center dot over a button until it activates.
- Keyboard testing: `N` next scene, `M` menu, `R` restart.
- The UI can also be replaced with Meta XR Interaction SDK ray, poke, or hand-tracking Building Blocks.

## Important Quest notes

- This template uses a lightweight OpenXR head-pose driver and gaze UI so it can run before controller prefabs are configured.
- For production controller or hand input, install Meta XR Interaction SDK and add the appropriate Meta Building Blocks.
- Keep shaders mobile-friendly, use baked lighting where possible, and test performance on the headset early.

## Student starter validation

A Unity 6000.3.23f1 batch Play Mode check passed for Scene1, Scene2, and Scene3: the original animation keeps moving, the 12-second countdown repeats without changing scenes, and gaze highlights and dwell activation work. All five scenes passed missing-script/reference and navigation-binding checks. The original scene files differ only in Auto Advance being disabled.

The user subsequently tested this student starter on Quest successfully. Headset frame rate was not measured. The desktop batch run logged startup licensing messages and an unavailable OpenXR headset, but the chapter checks completed successfully. Run Unity in batch mode with `-executeMethod StudentStarterCheck.Run` (without `-quit`) to repeat the checks; this opens the included scenes, enters Play Mode, and exits the Editor when finished. Results are written to `Artifacts/StudentStarterValidation.txt`. Use a separate Editor instance without unsaved work.

This branch is **student-starter-v1.0.2** in the existing repository. The Unity product name remains **MetaQuest-VR-Animation-Template**. After extracting a GitHub ZIP, you can rename the extracted folder to that name before adding it to Unity Hub.
