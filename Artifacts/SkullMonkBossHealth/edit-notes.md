Tool: built-in image_gen, edit with the Giant farm boss frame as a style/layout reference.
Asset: Assets/_Project/Art/UI/SkullMonkBoss/SkullMonkBossHealthFrame.png
Prefab: Assets/_Project/Resources/SkullMonkBossHealthUI.prefab
Prompt: Create a new long transparent boss HP frame for an undead Korean Buddhist skull monk. Weathered bronze, dark iron and carved temple stone; lotus, prayer beads, four staff rings and subtle skull endcaps. Long straight empty burgundy gauge well and empty centered name plaque. No wheat, sickles, text or background. Crisp painterly UI consistent with a ruined Korean temple. Preserve transparent padding and approximately 3:1 canvas.

UI width: 1280 reference pixels at 1920x1080, responsive to smaller screens.
Before dialogue: health UI hidden, minimap/portal retain normal visibility.
Completed dialogue (including rise animation): health UI visible; minimap and configured scene portals hidden.
Boss defeat / encounter disabled or destroyed: hide health UI and release minimap/portal suppression.
Runtime checks: trigger conversation, post-dialogue visibility, 2250/3000=75% gauge, minimap rebuild remains hidden during encounter, defeat restores minimap and portal.
