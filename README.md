# RunStumbleFix

A little code mod for Celeste that makes the Player's `runStumble` animation play properly.

`runStumble` is supposed to play when the Player lands on ground from a height of 50 pixels (6.25 tiles) or more, and is moving left or right at running speed.

However, an oversight in the Player's code makes it so that the sprite's animation is reset to the falling animation (either `fallFast` or `fallSlow`) in the same frame,
thus `runStumble` ends up not being shown when the frame is rendered.

There's also a second issue which concerns the "Player lands on ground from a certain height" condition.
A variable called `highestAirY` is used to store the Player's lowest Y position (here, 'lowest' means 'highest') reached by a jump from the ground,
and the difference between it and the Player's Y position is used to check for that condition.
If ground is detected one pixel below the Player, then `highestAirY` is reset to the Player's Y position, and thus the difference between `highestAirY` and Y becomes zero.
The problem is, that check is done *before* calling the function that moves the Player,
which has a function that is called when the Player collides with a platform,
which contains the code for checking the conditions above and playing `runStumble` if those conditions are met.
The Player's code may detect the ground one pixel below them, before the player collides with the ground,
in which case, `highestAirY` is reset, and the "Player lands on ground from a certain height" condition ends up not being met,
which leads to `runStumble` having inconsistent success in playing.

This code mod addresses both problems with two new fields for the Player (implemented via `ConditionalWeakTable`, I have my reasons):
- The first is `justLanded`, a boolean which
  - is set when the player lands on a platform,
  - is checked alongside `onGround` when automatically updating the sprite's animation,
  - and is reset at the end of the Player's `Update` function.
- The second is `prevHighestAirY`, a float which
  - is compared with `highestAirY` in the "Player lands on ground from a certain height" condition,
  - and is set to the value of `highestAirY` at the end of the Player's `Update` function.

This ensures that the conditions are always met when they should be met, and `runStumble` plays properly and is shown on screen.

This may seem like such a small thing to fuss about, but it does make a difference in the way the character feels to watch and play as.
We cannot thank Pedro Medeiros enough for his animation work, and Maddy Thorson and Noel Berry for implementing the Player and putting the pieces together.
Their combined work has helped in making Madeline feel alive, relatable, and fun to play.

## Afterword
If any issues come up while using this mod, please let us know.