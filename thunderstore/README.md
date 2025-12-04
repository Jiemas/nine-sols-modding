# Enlightened Ji

>**"Ji decided to read the hexagrams one last time and learned he did not need to hold back."**

A mod that makes Ji harder by increasing health, attack sequence length, move variety, and speed. The goal was to make the boss fight around as hard as unmodded Normal Eigong.

# Installation
- Can use a mod manager of your choice or manual installation.
- For mod manager, no extra steps required
- For manual installation, in addition to placing the dll file appropriately, the zip file includes 'EnlightenedJiBundle'
   - Move 'EnlightenedJiBundle' to the AppData folder of NineSols
      - Ex: Users\XXX\Data\LocalLow\RedCandleGames\NineSols
   - Incorrect placement will result in a normal-colored Ji
- More info on modding is on the [Nine Sols Wiki Page on Modding](https://ninesols.wiki.gg/wiki/Community:Modding).

# Changes
## General
- Phase 1 HP increased to 6500
- Phase 2 HP increased to 10750
- Animation speed increased by 20% generally. All further speed increases are in addition to this 20% increase.
- Included attacks originally only available in phase 2 into phase 1
- Minimized time Ji is vulnerable to attack
- Disabled hurt interrupt except during divination
- Increased Ji's speed of deploying altars (Health, Lasers, Black Holes)
- Every attack sequence now ends with a "sneak attack" that may be an accelerated sword attack, laser altar, black hole, or crimson attack
- Modified Ji's sprite to a darker color theme, inspired by [Ji Fanart by MOMONIAW](https://x.com/othername_/status/1989268097065517482?t=JuFWv_3NP4yR8Ky-SXweEQ&s=19)
- Changed crimson attack color to golden

## BepinEx Configuration Manager
- Allows you to modify different parts of the mod
- All modifications require a retry to fully take effect

### General Options
- Disables/Enables changes to HP, attack sequence, speed, and sprite color

### Speed Options
- Must have enabled speed changes
- Modify the base speed at which Ji's attacks occur

### HP Options
- Must have enabled HP changes
- Modify Ji's health at phase 1
- Modify the ratio used to calculate Ji's phase 2 HP (Phase 1 HP * ratio)

### Color Options
- Must have enabled color changes
- Modify keyboard shortcut to reload material and shaders used to modify colors
- Modify the colors based on RGB value

## Phase 1
- Changed boss title from Ji to Enlightened Ji
- All attack sequences end with 2 crimson attacks and a sneak attack
- Chance for blizzard attack, certain sword attacks, and crimson attacks to be accelerated
- Small chance for any attack to be accelerated
- Accelerated big black hole attack
- Accelerated hard laser altar attack

### Sword Attack Sequence
- Modified to now be 4 flying sword attacks

### Altar Attack Sequence
- Modified to now be a easy laser altar, small black hole, easy laser altar again, and a sword attack

### Small Black Hole Attack Sequence
- Modified to now be a small black hole, easy laser altar, blizzard, and a sword attack

### Blizzard Attack Sequence
- Modified to now be an easy laser altar, blizzard, a sword attack, and a blizzard again

## Phase 2
- Changed boss title from Ji to The Kunlun Immortal
- All attack sequences end with a hard laser altar, 3 crimson attacks, and a sneak attack
- Increased chance for blizzard attack, specific sword attacks, and crimson attacks to be accelerated
- Increased chance for any attack to be accelerated
- Accelerated big black hole attack
- Accelerated hard laser altar attack

### Opening
- Modified to be a sequence of 17 attacks that is every common combo usually experienced during phase 2
- Getting through this means you can probably get through phase 2

### Sword Attack Sequence
- Modified to now be 4 flying sword attacks

### Altar Attack Sequence
- Modified to now be a hard laser altar, blizzard, and 2 long sword attacks

### Small Black Hole Attack Sequence
- Modified now to be a small black hole, long sword attack, hard laser altar, and blizzard
- This attack sequence contains probably the hardest of the common combos

### Blizard Attack Sequence
- Modified to now be a blizzard, quick sword attack, blizzard again, and quick sword attack again

### Big Black Hole Attack Sequence
- Modified to now be a big black hole, hard laser altar, big black hole again, and long sword attack

### Health Altar Attack Sequence
- Can occur in both phases
- Once this sequence ends, it transitions to a random attack sequence
- Modified to now be a crimson attack or hard laser altar, big black hole, 2 long sword attacks, and a sneak attack
- This sequence does not end with the usual crimson, sneak attack combo

## Phase Transition
- Increased variety of messages that can appear during phase transition
- None of the messages are canon, just something I can imagine Ji saying
- Chance for nonsensical messages

## Known Bugs/Issues
- Flying swords have a chance to move harmlessly through Yi, not causing damage or allowing a parry
- Black holes have a chance to lower into the ground, most likely due to the speed increase
- Yi can rarely take damage from seemingly nowhere
   - I suspect this is due to the attack sequence modifications somehow creating an invisible damage source during a laser altar
   - This issue most commonly occurs while near an altar (Laser or Black Hole) and Ji launches an attack
   - While not frequent, this can be a run-ender, especially when on low health or attempting hitless
   - I am working on identifying and fixing this issue
- Small graphical errors are visible in certain animations, due to material and shader modifications

## Tips
- You can and should unbounded counter black holes. This prevents them from damaging Yi.
   - Note that if the black hole has suddenly gone halfway into the ground, it can damage Yi again even if it has been unbounded countered
- Being airborne during fast blizzard attacks allows for parries without needing to rapidly change orientation
- Swift Descent Jade is especially useful in this fight as there are many attacks that require careful positioning and dashing
   - With this jade equipped, Yi can perform an aeriel down dash through a laser circle without taking damage
- Steely Jade is especially useful for this fight, as Ji does not give a lot of opportunities to do a full 5-Qi blast
- A consistent ranged attack is helpful for destroying laser altars too far away when Yi's movement is prevented by a black hole
- Ji is not stunned by a 5 qi charge full control unless while doing divination
- Avoid trying to heal in the middle of an attack sequence unless the Quick Dose Jade is equipped
- There is enough time to perform unbounded counters on both crimson attacks, even in their accelerated state

## Showcase
- Youtube Video: [Nine Sols Enlightened Ji Mod, V1.1.0](https://youtu.be/eClaACkCi6k)
- Github Repository: [NineSolsEnlightenedJi](https://github.com/Jiemas/NineSolsEnlightenedJi)

## Acknowledgements
- Code was written through heavy reference of MicheliniDev's Eigong Prime Mod, KaitoMajima's Promised Eigong Mod, and Jakob Hellermann's Example Mod.
- The Unity Explorer Mod was essential to figuring out object paths and attack logic
- ChatGPT was used, especially for the shader and material code (that stuff is black magic), so thanks to all the Unity discussions online that OpenAI stole
- Thanks to everyone in the modding discord for their comments and suggestions!