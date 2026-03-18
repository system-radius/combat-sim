# Multiplayer Fighting Game – Client Prediction & Server Reconciliation

This project simulates a **networked 2-player fighting game** with client-side prediction, deterministic simulation, and server reconciliation. The system ensures smooth gameplay for local clients while maintaining server authority over game state.

---

## Features

1. **Player States**

  - **Idle** – Default state with no actions.
  - **Windup** – Player preparing an attack.
  - **Active** – Player executing the attack.
  - **AtkRecovery** – Recovery phase after an attack.
  - **DodgeStartup** – Player initiating a dodge.
  - **DodgeInvuln** – Dodge in progress; player is invulnerable.
  - **DefRecovery** – Recovery phase after a dodge.
  - **Hitstun** – Player hit by an attack; temporarily stunned.

  Each state has a **fixed duration in ticks** (PlayerStateDurations) to simulate consistent timing.

2. **Input Commands**

  - **Attack** – Initiates Windup → Active → AtkRecovery → Idle.
  - **Dodge** – Initiates DodgeStartup → DodgeInvuln → DefRecovery → Idle.
  - **None** – No action.

3. **Server Authority**

  - **Receives inputs** from both players.
  - **Applies inputs** only at the correct tick.
  - **Updates player states** each tick.
  - **Resolves conflicts**:
  - Active attacks put opponents in Hitstun if they are not invulnerable.

4. **Client-Side Prediction**

  - Clients maintain **Self** and **Other** player states.
  - **Self Inputs**: Applied immediately for smooth local responsiveness.
  - **Other Inputs**: Predicted deterministically; reconciled when server updates arrive.
  - **History tracking** for potential correction.

5. **Reconciliation**

  - If the server’s authoritative state differs from the client prediction:
  - The client **corrects its state**.
  - Logs reconciliation events for debugging:

  ```
  [Reconcile] ClientA: Predicted Self=Idle, Server=Windup
  [Reconcile] ClientB: Predicted PlayerA=Idle, Server=Windup
  ```

6. **Simulation & Replay**

  - **Simulation**: Runs tick-by-tick, predicting, reconciling, and logging client and server states.
  - **Replay Mode**: Verifies deterministic server results independent of client predictions.

Example usage:

  ```
  var inputs = new List
  {
  new PlayerInput { Tick = 2, PlayerName = "PlayerA", Command = InputCommand.Attack },
  new PlayerInput { Tick = 4, PlayerName = "PlayerB", Command = InputCommand.Dodge }
  };

  Simulate(inputs, 30);
  Replay(inputs, 30);
  ```

7. **Logs**

  - Logs show client predictions and server authoritative states per tick:

  ```
  Tick 02 | ClientA=Self:Windup Other:Idle || ClientB=Self:Idle Other:Windup || ServerA=Windup ServerB=Idle
  Tick 04 | ClientA=Self:Windup Other:DodgeStartup || ClientB=Self:DodgeStartup Other:Windup || ServerA=Windup ServerB=DodgeStartup
  [Reconcile] ClientA: Predicted PlayerB=Idle, Server=DodgeStartup
  ```

8. **Notes**

  - Self-inputs are only applied by the owning client to prevent prediction errors.
  - State timers decrement after processing, ensuring accurate durations.
  - The system is fully deterministic and can be used for lag compensation testing in fighting games