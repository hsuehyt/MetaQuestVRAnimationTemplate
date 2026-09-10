# Changelog

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
