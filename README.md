# VocalKnight

A mod for Hollow Knight v1.5.x that takes speech input and performs in game actions based on predefined keywords. The mod contains 60+ effects and up to 4 potential keywords per effect. Here is the link to the [keyword index](https://docs.google.com/document/d/1BbaO1pJV2KUgSY1maqH4CtloD41PyoLs0HxU9do1_oc/edit?usp=sharing) which lists all possible effects and their associated triggers. If you plan to stream this content live, I would recommend providing your chatters with command access to the index, with or without first studying it yourself, depending on how stressful you would prefer the initial experience to be. However, one command you as the player should be aware of is "Neutralize," which undoes any lasting effects, though most spawned game objects will remain. Most effects will self-neutralize upon transitioning between scenes.

## System Requirements & Setup

The player must be running the Windows 10 or Windows 11 operating system, as the mod relies on Windows Online Speech Recognition for translating speech into text.

To activate Online Speech Recognition, open your system's Settings app and navigate to Privacy --> Speech. From this page you can turn on the feature. The change is not permanent, and you can deactivate it at any time.

To install the mod, download the most recent release and unzip the contents into a folder called VocalKnight within your Hollow Knight mods directory. On your system, this may look like:

"C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods\VocalKnight\[.pdb, .dll, and README]"

Please seek explicit permission from Makeit_Hardcore before using a pre-release version of the mod. You may reach him on Discord at Makeit_Hardcore#1992.

## Required Dependency Mods

The following additional mods are required for the successful operation of VocalKnight:
- Satchel
- SFCore
- Vasi

These can all be installed using [Scarab](https://github.com/fifty-six/Scarab).

## Suggested Companion Mods

The following additional mods are highly recommended to be installed and enabled when using VocalKnight:
- Benchwarp
- DebugMod
- QoL

Again, these can all be installed using Scarab.

## Additional Mod Compatibility

The following additional mods are known to be *compatible* with VocalKnight:
- Custom Knight
- MoreSaves
- Randomizer 4
- EnemyHPBar

The following additional mods are known to be *incompatible* with VocalKnight:
- HueShifter
- HKTool

## Mod Settings

The mod comes equipped with settings to change the number of potential keywords which may trigger an individual effect (1 to 4 keywords per effect), toggles for enabling and disabling individual effects, and a toggle for the mod itself.

**PLANNED**: A "Randomize" setting which will randomly enable anywhere from 10 to 30 effects and disable the remainder, useful in the event 60+ effects are too overbearing and you as the player would rather leave it up to chance what is active.

## Words of Warning

One of the effects plays music which does not originate from Hollow Knight. The exerpt is from [Funky Dealer by Hideki Naganuma](https://www.youtube.com/watch?v=CwE2k0HMDfo), an original track from the SEGA videogame Jet Set Radio Future. AFAIK the song will not trigger a DMCA violation or otherwise create problems for those of you who wish to stream and/or record gameplay with this mod. However, that is still a risk you assume, and I bear no responsibility for what happens. If you are uncomfortable using the track, you can disable the "PARTY" effect in the mod settings.

The mod, as it is still in beta, does have some persistent/known bugs. Every now and then the speech recognizer engine will freeze up, but it should restart on its own after 20 seconds. Certain iterations of the mod have caused the game to crash when quitting to menu, activating a particular effect in an untested location, or toggling the mod in settings. Hopefully if you are beta-testing, Makeit_Hardcore will be watching in stream to help diagnose and hot-fix the issue.  If an issue does arise, especially if the mod crashes, it would be greatly appreciated if you could provide your modlog.txt and player.log files to Makeit_Hardcore for hot-fixes and future revisions.

## Credits

While this mod was fully developed by Makeit_Hardcore, much of the underlying code was borrowed and adapted from existing sources, listed below:
- [HollowTwitch](https://github.com/Sid-003/HKTwitch/): The original base upon which VocalKnight was constructed, responsible for the effect & cooldown systems, as well as a large number of the available effects. This mod was made by Sid-003, fifty-six, and a2659802.
- [Benchwarp](https://github.com/homothetyhk/HollowKnight.BenchwarpMod/): Responsible for the "bench" effect. This mod was made by homothetyhk, dpinela, flibber-hk, fifty-six, seresharp, pseudorandomhk, HKLab, and ygsbzr.
- [HKHKHKHKHK](https://github.com/SFGrenade/HKHKHKHKHK/): Responsible for the "gravup" effect. This mod was made by SFGrenade.
- [AdditionalChallenge](https://github.com/TheMulhima/AdditionalChallenge): Responsible for a number of effects, including "gorb," "nightmare," and "sheo." This mod was made by TheMulhima.
- [ChallengeMode](https://github.com/Hoo-Knows/HollowKnight.ChallengeMode): Responsible for a number of effects, including "grimmchild," "hungry," "charmcurse," "timewarp," and "aspidrancher." This mod was made by Hoo-Knows.
- [PressGToDab](https://github.com/Link459/PressGToDab/): Contributed to the "party" effect. This mod was made by Link459.
- [CustomBGM](https://github.com/SFGrenade/CustomBgm/): Contributed to the "party" effect. This mod was made by SFGrenade.
- [HueShifter](https://github.com/beesnation/HueShifter): Contributed to the "party" effect. This mod was made by beesnation.

And lastly, a big shoutout to my beta-testers:
- [spilled_oj_](https://www.twitch.tv/spilled_oj_)
- [TheMulhima](https://github.com/TheMulhima)
- [Fireb0rn](https://www.youtube.com/c/fireb0rngg)
