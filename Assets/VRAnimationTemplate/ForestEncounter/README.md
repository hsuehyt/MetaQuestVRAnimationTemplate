# Scene 2 — Forest encounter

Open `Assets/VRAnimationTemplate/Scenes/Scene2.unity` and press Play. The sequence starts automatically. In a headset, look up toward the robot; the desktop camera retains its original level orientation. The original VR UI and visible center gaze reticle are restored in front of the viewer, matching Scenes 1 and 3. Look at Menu or Next to activate it. Scene 2 automatically advances to Scene 3 after the 30-second sequence.

The robot is approximately 29 metres tall. Mountain conifers reach approximately 20–25 metres above the clearing. The viewer starts at human scale, 29 metres from the robot. Geometry is a stylized procedural interpretation of the concept, with rigid meshes parented to named joints, rather than a skinned character imported from a modeling package.

Motion source: `Assets/VRAnimationTemplate/Mocap/Look Around.fbx`, a 6.333-second Mixamo skeleton clip supplied by the user. The editor bakes source rotation changes onto the robot's hips, torso, head, arms, hands, and fingers. Analytic leg posing compensates for the giant's different proportions and keeps both feet planted. The resulting `Giant Look Around.anim` runs through an Animator Playable; the imported skeleton is not instantiated during gameplay.

Finger animation transfers local mocap flexion onto the robot's hinge axes, limited to inward curls, rather than copying world-space finger rotations. Finger order is mirrored so each index finger sits beside its thumb. Rebaking preserves these corrections.

Default timing: 1.5-second lead-in, then continuous approximately 21.11-second mocap loops at 30% speed. Two blinks occur near each loop end. Head movement follows the capture throughout; beams soften whenever the robot looks toward the viewer. Scene3 starts at 30 seconds. The robot root faces the viewer and stays in place; the previous scripted stride and footfall are not played. Adjust `Motion Speed` on the Forest Encounter component before Play. At startup, the chapter duration is extended when necessary to fit the complete slowed first cycle plus five seconds. The camera is never shaken.

The environment includes moonlight, distance fog, animated translucent ground mist, layered conifers, rocky ground relief, ferns, fallen logs, distant mountain silhouettes, and synthesized wind audio. Eye beams are transparent mesh approximations with spotlights; they soften when aimed toward the viewer. They are not physically accurate volumetric shadows. The supplied FBX is included in the project.

The editor menu `Tools > VR Animation Template > Build Forest Encounter in Scene 2` regenerates the encounter and its materials/meshes with a fixed seed. It opens Scene 2 and replaces its `Forest Encounter` root. Preserve manual edits before rebuilding. The original `Generate All Scenes` command still resets all scenes to template defaults.

Use `Tools > VR Animation Template > Apply Look Around Mocap to Scene 2` to rebake and apply just the motion without rebuilding the forest. Keep the source FBX imported as Generic. The forest builder also applies the mocap when the source is present. The Animator intentionally has no controller asset: the encounter creates its Playable graph at runtime.

Validation output and rendered previews are generated in `Artifacts/ForestEncounter`. The mocap builder checks 301 poses, stationary root placement, both feet, viewer-facing orientation, blinking, and repeatable loop poses. Standalone Quest frame rate, stereo appearance, audio levels, and comfort need a headset test; no device performance claim is made. Forest meshes are combined by material; further spatial chunking/LOD work may be needed after profiling.
