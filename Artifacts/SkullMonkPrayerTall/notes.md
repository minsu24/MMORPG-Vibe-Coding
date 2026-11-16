Prayer pattern revision: sky-high eruptions and vertical leap/plunge.

Explosion target height: 18 world units; matching damage column grows with the visual. Width: approximately 2.898 units in the current arena, about 18% wider than before. Eight eruptions remain; warning-to-warning safe gaps are approximately 1.560 units. Warning footprints remain low on the floor; eruption and dissolve are enlarged vertically.

Jump: brace until 0.12s, rise vertically at the original X to a 16-unit apex by 0.34s, reposition offscreen above the center, begin vertical plunge at 0.40s, land at 0.56s. Scripted position avoids physics speed limits delaying the plunge. Existing actor scale and design are preserved.
Landing: source landing pose is held 0.56..0.86s (0.30s); the next crouch/rise pose plays 0.86..1.04s; standing pose holds until prayer preparation at 1.12s. The three existing landing/recovery poses provide the extra settling motion. Landing dust is enlarged to 2 world units.

Verified: vertical rise X, vertical center drop X, measured leap height 16.015, zero horizontal landing error, height and width parameters, warning harmlessness, safe gap, damage and invulnerability, aura through eruption/dissolve, idle recovery, gravity and collider cleanup. Runtime save data protected using demo flag. See validation.json and explosions.png.
