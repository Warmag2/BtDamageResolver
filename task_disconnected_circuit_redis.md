# Task: Make disconnected circuits stop processing Redis messages (deferred)

## Problem

The Blazor Server client registers `ResolverCommunicator`, its inner
`ClientToServerCommunicator` (a Redis subscriber), the `HubConnection` and
`UserStateController` as **scoped** services (`Startup.cs:103-107`). A scope lives
for the lifetime of a Blazor circuit.

When a circuit is *disconnected but retained* (e.g. a phone user locks the screen
mid-game), the circuit is kept alive for `DisconnectedCircuitRetentionPeriod`
(currently 1 hour) so the user can reconnect to the exact same circuit. During
that window the scope is **not** disposed, so the scoped
`ClientToServerCommunicator` keeps its Redis subscription active and keeps:

1. deserializing every game-state/player message published to that player's
   Redis channel, and
2. forwarding it via `_hubConnection.SendAsync(...)` to a SignalR connection
   whose browser is gone.

Because other players in the same game keep acting while the phone user is paused,
this is real background CPU spent on a client that cannot see it. The cost scales
with `retained-disconnected-circuits-in-active-games × message rate`.

`DisconnectedCircuitMaxRetained` is therefore a CPU bound, not just a memory bound,
in this architecture. It is currently set to 256 (`Startup.cs`), accepting this
cost deliberately.

## Why it is not urgent

A full browser reload (Ctrl-F5) when reopening the app sidesteps the issue: it
abandons the old circuit, creates a fresh one, and re-requests the current game
state from scratch. So users who fully reload are unaffected. The cost only
applies to circuits that are retained and silently draining their channel.

## Proposed fix (attempt later — difficult to test)

Add a `CircuitHandler` that pauses the Redis subscription while the circuit is
disconnected and resumes it on reconnect:

- `OnConnectionDownAsync` → stop / pause the `ClientToServerCommunicator`
  subscription so a retained circuit becomes truly idle (memory-only).
- `OnConnectionUpAsync` → restart the subscription **and force a full game-state
  refetch**, because Redis pub/sub is fire-and-forget with no replay: any updates
  published during the pause are lost and the reconnected circuit would otherwise
  show stale state.

The state-refetch-on-reconnect requirement is the tricky, regression-prone part
(it is the same stale-state failure mode that has already bitten this codebase),
which is why it is deferred and must be tested carefully.

This work overlaps the larger "double-hop architecture" item in `opus47_review.md`
(removing `ClientHub` and notifying the circuit directly). Solving that removes the
self-`SendAsync` hop entirely and is the cleaner long-term home for this fix.

## Acceptance criteria

- A disconnected-but-retained circuit performs no Redis message processing for its
  game while disconnected.
- On reconnect within the retention window, the circuit shows current game state
  (not stale state from before the pause).
- `DisconnectedCircuitMaxRetained` can be raised without a proportional background
  CPU cost.
