# World Edit Tweaks

Extends the vanilla object modification system.

Install on all clients and the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

## Motivation

Valheim has a system that allows modifying object properties. This mod extends that system by adding more properties.

Hopefully the vanilla system will be extended in the future and this mod can be removed.

The system can be used by the vanilla `spawn` command but the best way is to use `spawn_object field=` and `object field=` commands from the World Edit Commands mod (provides autocomplete).

## Changes

With this mod:

- Locations can be modified.
  - For example boss altars.
- ItemDrop can be set.
  - For example the item from beehives.
- EffectList can be set.
  - Mainly used to spawn sound and visual effects when the player is doing something.
  - Multiple effects can be set (separated by ,).
- Character.Faction can be set.
  - For example to make a creature friendly.
- GameObject supports child transforms.
  - GameObject properties have two uses. Either to spawn a new object or to modify the object itself.
  - Vanilla system only supports setting a new object.
  - `field` autocomplete either shows object ids or child transforms depending on the property.
- bool supports `false` value.
  - Technically the vanilla system also supports `false` but it's removed on world load.
  - With this mod, `false` is saved as -1 which won't be removed.
- ZNetView syncInitialScale can be set to allow scaling.
  - Vanilla system doesn't support this because the scaling is checked before the modification is loaded.

## Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-world_edit_tweaks)

Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)
