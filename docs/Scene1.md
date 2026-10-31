# Scene1 — Rain

Scene1 includes a **Rain VFX** object that runs during Play mode. Its **Rain Effect** component controls the emission area, height, drops per second, velocity, surface layers, and ripple size/lifetime. The generator also includes this effect when rebuilding Scene1.

Drops create expanding, fading rings where they hit non-trigger 3D colliders, including the floor and animated placeholder. Add a Collider to imported surfaces that should receive rain impacts; a Rigidbody is not required. Rings follow moving surfaces. The effect uses a URP shader, a bounded pool, and one combined mesh without per-drop GameObjects. Profile on the target headset and reduce Drops Per Second if needed.

Scene1 uses a repeating 12-second countdown with Auto Advance disabled. Use **Menu** or **Skip / Next** to leave the chapter.

[Back to the project overview](../README.md)
