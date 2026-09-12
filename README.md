# winners-pot-buttons

BepInEx 5 plugin for Black Jacket. Adds coin buttons for the **winners pot** and for **your own
coins** (the player's pot) on every screen: matches, shops and the campaign map.

Each pot gets eight buttons:

- `MIN` – empty it
- `-10`, `-5`, `-1` – remove coins
- `+1`, `+5`, `+10` – add coins
- `MAX` – fill it up to its cap

The buttons are placed next to whichever counter is currently visible (the match HUD, the shop HUD
or the campaign HUD). The winners pot cap is read live, so cap upgrades bought in a shop (for
example `+5` winners pot size) work immediately. While a match or shop is running, the player's
coins and the pot are the real table zones, so the game synchronises them into your run as usual;
on the campaign map the values are written directly to the campaign state.

Settings live in `BepInEx/config/com.blackjacket.mods.winnerspotbuttons.cfg`:

- `ShowWinnersPotButtons`, `ShowPlayerButtons` – toggle either button grid.
- `IgnorePotCap` – let the winners pot exceed its cap.
- `ButtonSize`, `Spacing`, `FontSize`, `Margin` – layout.
- `NudgeX`, `NudgeY` – fine-tune the button position if needed.
