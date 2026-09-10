# Changelog

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
