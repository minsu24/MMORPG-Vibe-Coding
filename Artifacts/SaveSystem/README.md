# Save system

- Runtime files: Application.persistentDataPath/Saves/class-{id}.json. Dosa=0, Monk=1, Swordswoman=2.
- Each write uses a temporary file and atomic replacement; the previous file is retained as .bak. Invalid primary files fall back to the backup. If neither can be read, saving is blocked to protect the existing records.
- Automatic saves run every 10 seconds, after inventory/quest/skill changes settle, after scene arrival, before portal/admin scene transfers, on focus loss/pause, and on application quit.
- Saving waits during character restoration, scene transitions and death. The last living checkpoint remains available until revival.
- Settings: Save button in the previous volume row, volume below fullscreen, status underneath. Global sound/fullscreen preferences remain separate from character progress.
- F10: the reset button starts the selected class from the tutorial for this Play session. Both automatic and manual saves are blocked throughout that demo session. The next Play session resumes the original save.

Saved state includes scene/position, HP/MP/mental/Monk energy, level/EXP, currency, inventory quantities and layout, equipped items, consumable and skill quick slots, allocated and unused skill points, advancement, quests and their progress, boss defeats and the unclaimed toad relic, companion progress/equipment, maps/codex discoveries, completed tutorials and one-time dialogue/reward history. Ordinary enemies and temporary drops retain their existing in-session map cache behavior.

Asset references use GUIDs through Resources/SaveAssetCatalog.asset. The catalog rebuilds before Play/build and from Eastern Fantasy > Save > Rebuild Asset Catalog.

## Validation

Native Unity Play mode was used with an isolated storage override under this artifact folder. See Validation.txt and Settings.png. The actual user save directory was not used.

The editor harness is Assets/_Project/Editor/SaveSystemPlayValidation.cs. Set SessionState key EasternFantasy.SaveTestDirectory to a fresh directory before entering Play mode; run Seed, restart Play, VerifyRestored, VerifyClassesAndAutosave, then VerifyDemo. A subsequent Play session must load the original fixture again. Clear EasternFantasy.SaveTestDirectory and EasternFantasy.SaveValidationFreeze after stopping Play.
