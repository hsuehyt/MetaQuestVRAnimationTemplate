# Scene 2 — Forest encounter

Open `Assets/VRAnimationTemplate/Scenes/Scene2.unity` and press Play. The sequence starts automatically. In a headset, look up toward the robot; the desktop camera retains its original level orientation. The original VR UI and visible center gaze reticle are restored in front of the viewer, matching Scenes 1 and 3. Look at Menu or Next to activate it. Scene 2 automatically advances to Scene 3 after the 30-second sequence.

The robot is approximately 29 metres tall. Mountain conifers reach approximately 20–25 metres above the clearing. The viewer starts at human scale, 29 metres from the robot. Geometry is a stylized procedural interpretation of the concept, with rigid meshes parented to named joints, rather than a skinned character imported from a modeling package.

Timing: forest introduction 0–6 seconds; scanning 6–14; weight shift and one stride 15–19; head and torso discovery 20–27; two blinks at 26.2 and 26.65; gentle head tilt through 30; automatic transition to Scene 3 at 30 seconds. Head tracking uses the main camera position. The supporting foot uses analytic two-bone posing, and the camera is never shaken.

The environment includes moonlight, distance fog, animated translucent ground mist, layered conifers, rocky ground relief, ferns, fallen logs, distant mountain silhouettes, and synthesized wind/footfall audio. Eye beams are transparent mesh approximations with spotlights; they fade when looking at the viewer. They are not physically accurate volumetric shadows. No external audio or model assets are required.

The editor menu `Tools > VR Animation Template > Build Forest Encounter in Scene 2` regenerates the encounter and its materials/meshes with a fixed seed. It opens Scene 2 and replaces its `Forest Encounter` root. Preserve manual edits before rebuilding. The original `Generate All Scenes` command still resets all scenes to template defaults.

Validation output and three rendered previews are in `Artifacts/ForestEncounter`. Unity compilation, missing-script checks, timeline sampling, support-foot stability, blink state, and final gaze are checked by the builder. Standalone Quest frame rate, stereo appearance, audio levels, and comfort need a headset test; no device performance claim is made. Forest meshes are combined by material; further spatial chunking/LOD work may be needed after profiling.
