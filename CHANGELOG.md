# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- HullDown cover for vehicles.
- Div-based UI layout and much better draggable items.
- Spectator mode.
- Units that violate game rules have their whole background drawn in red.
- Exporter for unit data.
- Invalid target is now shown in red when editing units.
- Unit rules validation.
- Ammo.
- Stances for infantry/mechs.
- Quirks.
- Parametrize all passwords for practical security reasons before release.
- Multiple quirks implemented.
- Show password protection in game list.
- Redis-based API and client communication.
- Only show possible movement types and amounts as per unit type and penalties.
- Display incorrectly input fields in red.
- MASC.
- Unit base speed can be altered.
- Heat speed effects.
- Listing of heat effects.
- Heat tracking and hit probability effects.
- EMP tracking and hit probability effects.
- Floating Critical Rule.
- Weather.
- Attacker ammo expenditure tracking.
- Defender AMS ammo expenditure tracking.
- Game owner can kick players from the game.
- GM can force a ready state for all players.
- GM can move units from a player to another.
- Players can gift units to other players.
- A player can choose to see ALL damage reports in the damage reports tab, not just those associated with him/herself.
- Game remembers damage reports and sends them to joining units.
- Damage reports contain unit names in addition to unit IDs, so that players can correctly see which units were damaged, even if the units have already been removed from the game.
- Total damage from attack action is now visible in the damage report.
- Just writing the correct weapon name selects that weapon.
- Default unit type is now a mech.
- Now all numeric items can be picked, not just distance.
- Flak.
- Weather effects, can be set by game creator.
- AMS Immune tag for future missiles which need it, such as Arrow IV.

### Added (internal)

- Refactored Gunnery/Piloting out of base unit class. Now in UnitEntry so that unit loading works correctly.
- Server sends metadata with error messages.
- Got rid of UnitActor.
- Refactor code for .NET 7 and Orleans 7.
- Logic reflection for easier expansions.
- Faster and stabler repositories.
- Better parser.
- Data sent over network is compressed with LZMA.
- Make ResolverCommunicator a scoped service in ResolverClient. This avoids having to pass it to components and makes the system more resilient to bugs.
- All public actions in the game now require authentication by a token given to the user upon connection. API can be published and anybody can make a client of their own without disturbing other users with illegal actions.
- Make ConnectedToGame a deduced property (from Game State). This avoids having to ever set/unset it, which is bug-prone.
- Make the connection a bit more resilient. If the server has lost its connection to the client grain observer, the messages that have not been delivered are stored, and will be resent when the player reconnects. Automatic reconnection happens when the player tries to update his/her state and the grain informs him that the connection is faulted.
- Reverted to sending target number updates as separate events instead of weapon data, reducing bandwidth usage and increasing reliability.
- PlayerActor only keeps one unsent gamestate in memory.
- PlayerActor sends damage reports in bulk -> better compression.
- All compressed data is now sent with the same method, reducing complexity.
- Refactor code for .NET 10 and Orleans 10.
- Weapon rework, now all rules are simply weapon special features.
- Proper single missile attack handling.
- Weapon bays.
- Capital ships.
- Brotli compressor, move away from LZMA.
- Reduce amount of over/redraw - key components more carefully and trust user view when sending data.
- ShouldRender guards for multiple items, and massive performance optimization.
- Continuous integration via GitHub Actions: builds the server solution and runs the test suite on push/PR.
- Obsolete and unused nuget removal pass. Also remove Newtonsoft vestige.
- Reverted outdated Orleans messaging timeouts to current framework defaults, and removed the obsolete SignalR receive-buffer override (game state is no longer transported over SignalR).
- Completely reworked docker image creation. Use Alpine images, use repository root as base, add dockerignore, add container for dataexporter for no-fuss exporting.
- Password hashing upgraded from single-iteration SHA-512 to PBKDF2 (HMAC-SHA256, 100,000 iterations). Invalidates pre-existing password hashes.
- Decompression of incoming state messages moved off the Blazor circuit thread, improving UI responsiveness.
- Removed the double-hop SignalR architecture (eliminated the self-looping ClientHub round-trip), reducing latency and complexity.
- Eliminated a broadcast storm: editing a combobox no longer triggers a full gamestate update per-keystroke.
- MathExpression parse results are now cached when possible.
- Removed Serilog in favour of the built-in Microsoft.Extensions.Logging.

### Fixed

- Ammo displays were nonsense and separate between Artemis/Indirect, for example.
- When starting up, user needs to wait for the import task to finish before using client.
- Units were not displayed correctly on first page refresh after overdraw fixes.
- Data display errors, especially for Weapons.
- Burst/HeatConverted are visible on mechs etc. Fix by more carefully cleaning damage reports of invalid damage type per receiving unit type.
- Backspace cannot be held when manipulating any combobox.
- Null unit list will be loaded for users who load their state after inactivity.
- Damage is not visible on hover on all damage reports.
- MML ammo types cause a crash on turn resolution.
- Battle armor and infantry weapons had 0 minimum range. Now -1 -> no effect.
- Damage tooltip not visible in damagereports screen, only in dashboard.
- Regression bug with weapons which do not have properties before ammo gets applied.
- Vehicle motive hit rolls modified by vehicle type could get results over 12.
- Forced critical hits (such as that from AC AP ammo) would inflict critical hits against targets which could not take critical hits (such as infantry).
- Critical hit modifiers might cause result selection to go out-of-bounds.
- Melee charge by ground vehicles worked incorrectly to non-prone mechs.
- SRMs and other non-1 clusterdamage weapons did incorrect damage to infantry.
- Grafana bar graph does not display zero for intervals which are zero.
- Grafana login is admin/admin.
- Joining games with a password does not work from the modal dialog.
- Deleting a damage report does not refresh.
- Unit name is not saved correctly when saving into unit repository.
- Game list does not refresh automatically / at all.
- Task for resolving a turn was ignored so exceptions could not be caught. User received no information at all about errors.
- Charge attacks did not work against units which could not take motive hits.
- Selecting any tab other than Server with no game connection crashes the client.
- Selecting a weapon item may select another item with a similar name.
- Cannot set <no target>.
- Grey inactive weapons do not read "inactive".
- Faulty unit breaks the whole game instead of discarding invalid values.
- LBX shooting VTOL: "hit modifier from weapon features".
- Toggle static parameters does nothing.
- Unit that cannot hit still produces a damage report.
- If absolutely no weapons are active, damage report is still produced.
- Attack log shows weapons that could not hit.
- Rapid fire weapon heat was calculated incorrectly.
- Attacker heat was not displayed, now shown on a separate line.
- Exploit fix: Authentication was not required for some public actions.
- User could not separate critical damage from critical damage threats.
- Weapons may go missing.
- Cannot join game after disconnection, password is not accepted.
- Cluster weapons now always do maximum damage against buildings.
- Battle armor should be able to fire at range 0 units.
- Now you cannot pick a distance if you do not have weapons.
- Clicking "Pick" again now closes the picker.
- Race condition in TN reporting, target list flickers and displays wrong values.
  - _Note:_ Target number updates are no longer sent. Target number update updates unit state directly.
  - _Note:_ This has since been reverted. Target number updates are again sent, but seem to work this time.
- Target list refreshes every update -> difficult to select targets if somebody is doing changes to his/her units.
  - _Note:_ Target list is only refreshed if the number of targets actually changes. It will still flicker when this happens, but this is unavoidable.
- Penalty from shooting VTOL should be communicated to player (in UI).
  - _Note:_ Now all target calculation information is communicated to player if target number is hovered over.
- BA trooper attacking armored car had TN 6 and hit roll 9 => nothing still happened.
- Can set 0 troopers to BA squad.
  - _Note:_ These were linked. BA Trooper count was zero and thus 0 weapon attacks were generated. Setting minimum number of troopers to 1. Units are now also always generated with 1 trooper.
- Battle armor squads with many troopers and infantry burst damage do not work correctly (at least, may be that BA squads with many troopers do not work at all).
  - _Note:_ Multiple bugs were present here, and BA never did damage to infantry from multiple troopers. Should now be fixed for both normal damage and burst damage.
- Generated damage reports should not be printed for next turn.
  - _Note:_ They are now printed for the turn that last ran its weapons phase.
- Logging no longer grows memory unbounded when the database is unavailable (retries are now bounded), and log writes are batched.
- Fixed a per-circuit Redis subscription leak.

### Wontfix / Needs discussion

- Unit type is not visible after toggling static parameters.
  - _Note:_ This is a feature, as you won't change it after you set it. Why should it stay visible? If this needs to stay visible, what other things need to be displayed after you've already set them and want to hide them?
- Can't manual select 3 if bracket is 3-4 => field resets to 4.
  - _Note:_ Can't find bug. You can always select a distance manually, but selecting PICK sets the distance to the highest number in the bracket (X-Y). Selecting a specific number inside the bracket should make no difference with respect to any targeting decisions and/or target number information, so the player should not care about selecting a specific number once the bracket is selected. If the bug reporter meant something else, reproduction steps are needed.
