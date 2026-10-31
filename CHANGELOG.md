# Changelog

## Student starter (based on 1.0.2)

- Preserve the original animation content and five-scene flow.
- Backport immediate gray-to-blue gaze button feedback and repeating chapter countdowns.
- Disable automatic chapter advancement in Scene1–3 and the scene generator; use Menu or Skip / Next.
- Keep the original assets and scene object references.
- Validation: all three chapters passed desktop loop, animation, and gaze checks; all five scenes passed reference and navigation-binding checks. User confirmed Quest testing.


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
