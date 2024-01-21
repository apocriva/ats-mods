Source: https://github.com/ats-mods/StormUX

# Against the Storm: StormUX

A mod for Against the Storm that adds a few user experience changes.
* Configurable hotkeys for:
	* Encyclopedia menu. (Hotkey for each tab, default F1 through F5)
	* Select worker by slot (default Numpad 1 through 3) or by race (default unbound)

# Installation

Dependencies:
* **(Required)** [BepInEx](https://github.com/BepInEx). (Currently built againast [version 5.4.21](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21))
* _(Recommended)_ [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager). Supported by BepInEx for managing mod settings.
* _(Recommended)_ [OptionsExtensions](https://github.com/ats-mods/OptionsExtensions). Adds game UI support for some StormUX settings.

After running the game once with the mod installed,
`BepInEx\config\ExtraHotkeys.cfg` can be modified to set the desired hotkeys.
Much easier to have ConfigurationManager + OptionsExtensions!

# Changelog

## v1.0.0

* Initial release, combining former WorkerHotkeys and ExtraHotkeys mods into one.