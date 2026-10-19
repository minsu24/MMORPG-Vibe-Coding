# Spider hunt quest validation

- Ghost quest still supports offer, acceptance, five kills and chief turn-in.
- Spider quest cannot be accepted before the ghost quest is completed.
- Chief switches to the spider quest after ghost turn-in; decline permits a later offer.
- Chief cannot complete the spider quest; injured resident accepts turn-in only.
- Spider prefab used by the cave spawner references the new ten-kill quest.
- Actual TakeDamage death path increments once, including repeated damage on a defeated spider.
- 9/10 kills: Active, resident cannot complete, Marshland portal locked.
- Scene reload at 9/10 preserves count and lock.
- Tenth kill: ReadyToTurnIn, portal unlocked; further kills remain capped at 10.
- Quest tracker points to the injured resident when ready to report.
- PlayerQuestInteraction.TryInteract (T-key path) completes the quest at the resident.
- Completion pays 20 yeopjeon once; configured experience reward is 1000.
- All five original HurtNPC_DIalogue lines and every class override are preserved after the added thank-you line.
- After completion, scene reload keeps the quest completed and portal unlocked, and hides the blocked hint.
- Fresh session without an accepted quest keeps the portal locked; hint visually checked.
- No Unity runtime errors during the end-to-end flow.
