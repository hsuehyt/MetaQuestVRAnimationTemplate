# Scene3 — Furry dancer

Scene3 contains a **2-metre** character in front of the viewer, playing an approximately **18.67-second** looping belly dance. Belly jiggle amount **2** is baked into the exported joints. Auto Advance is disabled, and the countdown restarts with every dance loop. Use **Skip / Next** or Menu to leave the scene.

The current fur preset targets **Quest connected to a PC**. It uses 2,000 opaque tube strands plus an undercoat, blue roots and skin, and magenta tips. Strands are 15–35 cm long and 30–50 mm in diameter, with zero taper. Automatic segmentation uses a 7.5 cm target, 2–8 segments, and four tube sides. Palms and soles are excluded using a surface mask.

Select **Dancing Fat Man > Fur** in the Hierarchy. The **Dancer Fur** Inspector exposes colors (including bare skin), strand count, length up to 100 cm, diameter, taper, randomness, and segment settings. Outside Play mode, edit these settings and click **Apply Fur Settings**, then save. The groom asset is shared: changing it affects other instances using the same asset. Applying updates the selected coat's preview; it does not automatically overwrite the reusable prefab.

Motion controls can be adjusted during playback. Defaults are inertia **0.9**, stiffness **0.5**, gravity **0.4**, with simulation and body collision enabled. **Reset Settings to Defaults** appears below **Collision volumes** and resets appearance and motion settings; click Apply Fur Settings to rebuild the coat afterward. The fitted collision volumes are preserved.

Fur follows the skinned body with damped particle motion. Collision uses fitted body volumes and root surface planes, so some penetration is possible; strands can pass through each other. Profile the result on your PC and headset before increasing strand count, length, segments, or tube sides. Standalone Quest performance has not been validated.

The character FBX, controller, materials, groom, preview mesh, and prefabs are under `Assets/VRAnimationTemplate/DancingFatMan`. Open the saved Scene3 directly to retain its current setup. The Tools menu's scene and character builders replace generated content, so preserve custom edits before using them.

[Back to the project overview](../README.md)
