# Gameplay HUD corner layout

- Removed the upper-right currency display from the quick-slot HUD. Inventory and shop currency displays and the actual balance are unchanged.
- Moved the existing F9/F10 administrator indicator to the upper-left corner at (12,12).
- Added a themed Settings button in the upper-right corner. It opens the existing SettingsMenu prefab, with its normal pause/input handling, volume/fullscreen controls, save and close actions.
- Moved the minimap from top offset 130 to 66: 64 reference-resolution pixels higher, leaving a gap under the Settings button.

The shared QuickSlotBar and Minimap prefabs are updated, along with the independent MovementPrototype HUD. Runtime layout application handles scene-specific overrides. The legacy MovementPrototype settings entry is hidden to prevent duplicate buttons. The existing SettingsMenu prefab is reused rather than duplicated.

Verification: the settings button opens exactly one settings entry, opening pauses the game, closing resumes it, currency is hidden and minimap position is correct. Captured and visually inspected `gameplay.png` and `settings.png` at 1920x1080. See `verification.json` for the checks. Temporary runtime objects were discarded on leaving Play Mode and the original editor scene was restored.
