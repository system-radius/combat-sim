\# Multiplayer Fighting Game – Client Prediction \& Server Reconciliation



This project simulates a \*\*networked 2-player fighting game\*\* with client-side prediction, deterministic simulation, and server reconciliation. The system ensures smooth gameplay for local clients while maintaining server authority over game state.



\---



\## Features



1\. \*\*Player States\*\*

&#x20;  - \*\*Idle\*\* – Default state with no actions.

&#x20;  - \*\*Windup\*\* – Player preparing an attack.

&#x20;  - \*\*Active\*\* – Player executing the attack.

&#x20;  - \*\*AtkRecovery\*\* – Recovery phase after an attack.

&#x20;  - \*\*DodgeStartup\*\* – Player initiating a dodge.

&#x20;  - \*\*DodgeInvuln\*\* – Dodge in progress; player is invulnerable.

&#x20;  - \*\*DefRecovery\*\* – Recovery phase after a dodge.

&#x20;  - \*\*Hitstun\*\* – Player hit by an attack; temporarily stunned.



&#x20;  Each state has a \*\*fixed duration in ticks\*\* (`PlayerStateDurations`) to simulate consistent timing.



2\. \*\*Input Commands\*\*

&#x20;  - \*\*Attack\*\* – Initiates `Windup → Active → AtkRecovery → Idle`.

&#x20;  - \*\*Dodge\*\* – Initiates `DodgeStartup → DodgeInvuln → DefRecovery → Idle`.

&#x20;  - \*\*None\*\* – No action.



3\. \*\*Server Authority\*\*

&#x20;  - \*\*Receives inputs\*\* from both players.

&#x20;  - \*\*Applies inputs\*\* only at the correct tick.

&#x20;  - \*\*Updates player states\*\* each tick.

&#x20;  - \*\*Resolves conflicts\*\*:

&#x20;    - Active attacks put opponents in `Hitstun` if they are not invulnerable.



4\. \*\*Client-Side Prediction\*\*

&#x20;  - Clients maintain \*\*Self\*\* and \*\*Other\*\* player states.

&#x20;  - \*\*Self Inputs\*\*: Applied immediately for smooth local responsiveness.

&#x20;  - \*\*Other Inputs\*\*: Predicted deterministically; reconciled when server updates arrive.

&#x20;  - \*\*History tracking\*\* for potential correction.



5\. \*\*Reconciliation\*\*

&#x20;  - If the server’s authoritative state differs from the client prediction:

&#x20;    - The client \*\*corrects its state\*\*.

&#x20;    - Logs reconciliation events for debugging:

&#x20;      ```

&#x20;      \[Reconcile] ClientA: Predicted Self=Idle, Server=Windup

&#x20;      \[Reconcile] ClientB: Predicted PlayerA=Idle, Server=Windup

&#x20;      ```



6\. \*\*Simulation \& Replay\*\*

&#x20;  - \*\*Simulation\*\*: Runs tick-by-tick, predicting, reconciling, and logging client and server states.

&#x20;  - \*\*Replay Mode\*\*: Verifies deterministic server results independent of client predictions.



&#x20;  Example usage:



&#x20;  ```csharp

&#x20;  var inputs = new List<PlayerInput>

&#x20;  {

&#x20;      new PlayerInput { Tick = 2, PlayerName = "PlayerA", Command = InputCommand.Attack },

&#x20;      new PlayerInput { Tick = 4, PlayerName = "PlayerB", Command = InputCommand.Dodge }

&#x20;  };



&#x20;  Simulate(inputs, 30);

&#x20;  Replay(inputs, 30);



7\. \*\*Logs\*\*

&#x20;  - Logs show client predictions and server authoritative states per tick:

&#x20;       ```

&#x20;       Tick 02 | ClientA=Self:Windup   Other:Idle   || ClientB=Self:Idle   Other:Windup   || ServerA=Windup   ServerB=Idle

&#x20;       Tick 04 | ClientA=Self:Windup   Other:DodgeStartup || ClientB=Self:DodgeStartup Other:Windup || ServerA=Windup   ServerB=DodgeStartup

&#x20;       \[Reconcile] ClientA: Predicted PlayerB=Idle, Server=DodgeStartup

&#x20;       ```



8\. \*\*Notes\*\*

&#x20;  - Self-inputs are only applied by the owning client to prevent prediction errors.

&#x20;  - State timers decrement after processing, ensuring accurate durations.

&#x20;  - The system is fully deterministic and can be used for lag compensation testing in fighting games.

