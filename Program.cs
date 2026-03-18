using System;
using System.Collections.Generic;
using System.Threading;

// =======================
// ENUMS
// =======================

enum PlayerStateType
{
    Idle,
    Windup,
    Active,
    Recovery,
    DodgeStartup,
    DodgeInvuln,
    Hitstun
}

enum InputCommand
{
    None,
    Attack,
    Dodge
}

// =======================
// PLAYER CLASS
// =======================

class PlayerState
{
    public PlayerStateType State;
    public int Timer;

    public PlayerState(PlayerStateType state)
    {
        State = state;
        Timer = PlayerStateDurations[state];
    }

    public static readonly Dictionary<PlayerStateType, int> PlayerStateDurations = new()
    {
        { PlayerStateType.Idle, 0 },
        { PlayerStateType.Windup, 12 },       // 200 ms / 60Hz
        { PlayerStateType.Active, 6 },        // 100 ms
        { PlayerStateType.Recovery, 18 },     // 300 ms
        { PlayerStateType.DodgeStartup, 3 },  // 50 ms
        { PlayerStateType.DodgeInvuln, 6 },   // 100 ms
        { PlayerStateType.Hitstun, 6 }        // 100 ms
    };

    public PlayerState Clone() => new PlayerState(State) { Timer = Timer };

    public override string ToString() => $"{State}({Timer})";
}

class Player
{
    public string Name;
    public PlayerState Current;

    public PlayerStateType State
    {
        get => Current.State;
        set => Current.State = value;
    }

    public int StateTimer
    {
        get => Current.Timer;
        set => Current.Timer = value;
    }

    public Player(string name)
    {
        Name = name;
        Current = new PlayerState(PlayerStateType.Idle);
    }

    public void SetState(PlayerStateType newState)
    {
        State = newState;
        StateTimer = PlayerState.PlayerStateDurations[newState];
    }

    public void ApplyInput(InputCommand command)
    {
        if (State != PlayerStateType.Idle) return;

        switch (command)
        {
            case InputCommand.Attack: SetState(PlayerStateType.Windup); break;
            case InputCommand.Dodge: SetState(PlayerStateType.DodgeStartup); break;
        }
    }

    public void Update()
    {

        switch (State)
        {
            case PlayerStateType.Windup:
                if (StateTimer == 0) SetState(PlayerStateType.Active);
                break;
            case PlayerStateType.Active:
                if (StateTimer == 0) SetState(PlayerStateType.Recovery);
                break;
            case PlayerStateType.Recovery:
                if (StateTimer == 0) SetState(PlayerStateType.Idle);
                break;
            case PlayerStateType.DodgeStartup:
                if (StateTimer == 0) SetState(PlayerStateType.DodgeInvuln);
                break;
            case PlayerStateType.DodgeInvuln:
                if (StateTimer == 0) SetState(PlayerStateType.Recovery);
                break;
            case PlayerStateType.Hitstun:
                if (StateTimer == 0) SetState(PlayerStateType.Idle);
                break;
        }

        if (StateTimer > 0) StateTimer--;
    }

    public bool IsInvulnerable => State == PlayerStateType.DodgeInvuln;
}

// =======================
// INPUT COMMAND WITH TICK
// =======================

class PlayerInput
{
    public int Tick;
    public string PlayerName = "";
    public InputCommand Command;
}

// =======================
// SERVER CLASS
// =======================

class Server
{
    public Player PlayerA;
    public Player PlayerB;
    public int Tick = 0;

    private Dictionary<int, List<PlayerInput>> inputBuffer = new();

    public Server(Player a, Player b)
    {
        PlayerA = a;
        PlayerB = b;
    }

    public void ReceiveInput(PlayerInput input)
    {
        if (!inputBuffer.ContainsKey(input.Tick))
            inputBuffer[input.Tick] = new List<PlayerInput>();
        inputBuffer[input.Tick].Add(input);
    }

    public void Step()
    {
        // Apply inputs scheduled for this tick
        if (inputBuffer.ContainsKey(Tick))
        {
            foreach (var input in inputBuffer[Tick])
            {
                if (input.PlayerName == PlayerA.Name) PlayerA.ApplyInput(input.Command);
                else if (input.PlayerName == PlayerB.Name) PlayerB.ApplyInput(input.Command);
            }
        }

        // Update players
        PlayerA.Update();
        PlayerB.Update();

        // Resolve Active → Hit vs Invulnerable
        ResolveConflicts();

        Tick++;
    }

    private void ResolveConflicts()
    {
        if (PlayerA.State == PlayerStateType.Active && !PlayerB.IsInvulnerable && PlayerB.State != PlayerStateType.Hitstun)
        {
            PlayerB.SetState(PlayerStateType.Hitstun);
        }

        if (PlayerB.State == PlayerStateType.Active && !PlayerA.IsInvulnerable && PlayerA.State != PlayerStateType.Hitstun)
        {
            PlayerA.SetState(PlayerStateType.Hitstun);
        }
    }
}

// =======================
// CLIENT CLASS
// =======================

class Client
{
    public string Name;
    public Player Self;
    public Player Other;
    public List<PlayerInput> Inputs = new();
    private Dictionary<int, PlayerState> historySelf = new();
    private Dictionary<int, PlayerState> historyOther = new();

    public Client(string name, Player self, Player other)
    {
        Name = name;
        Self = new Player(self.Name) { Current = self.Current.Clone() };
        Other = new Player(other.Name) { Current = other.Current.Clone() };
    }

    public void AddInput(PlayerInput input) => Inputs.Add(input);

    public void Predict()
    {
        // Save history
        historySelf[Self.CurrentTimerTick()] = Self.Current.Clone();
        historyOther[Other.CurrentTimerTick()] = Other.Current.Clone();

        // Predict own player
        var ownInputs = Inputs.FindAll(i => i.Tick == Self.CurrentTimerTick());
        foreach (var input in ownInputs)
            Self.ApplyInput(input.Command);
        Self.Update();

        // Predict other player (deterministic: keep previous state if no input)
        var otherInputs = Inputs.FindAll(i => i.Tick == Other.CurrentTimerTick());
        foreach (var input in otherInputs)
            Other.ApplyInput(input.Command);
        Other.Update();
    }

    public void Reconcile(Player authoritativeSelf, Player authoritativeOther)
    {
        // Reconcile self
        if (Self.State != authoritativeSelf.State)
        {
            Console.WriteLine($"\n[Reconcile] {Name}: Predicted Self={Self.State}, Server={authoritativeSelf.State}");
            Self.Current = authoritativeSelf.Current.Clone();
        }

        // Reconcile other
        if (Other.State != authoritativeOther.State)
        {
            Console.WriteLine($"\n[Reconcile] {Name}: Predicted {Other.Name}={Other.State}, Server={authoritativeOther.State}");
            Other.Current = authoritativeOther.Current.Clone();
        }
    }
}

// =======================
// EXTENSIONS
// =======================

static class PlayerExtensions
{
    public static int CurrentTimerTick(this Player p) => p.StateTimer; // can be replaced with Tick if needed
}

// =======================
// PROGRAM ENTRY
// =======================

class Program
{
    static void Main()
    {
        var playerA = new Player("PlayerA");
        var playerB = new Player("PlayerB");
        var server = new Server(playerA, playerB);

        var clientA = new Client("ClientA", playerA, playerB);
        var clientB = new Client("ClientB", playerB, playerA);

        var inputs = new List<PlayerInput>
        {
            new PlayerInput { Tick = 2, PlayerName = "PlayerA", Command = InputCommand.Attack },
            new PlayerInput { Tick = 4, PlayerName = "PlayerB", Command = InputCommand.Dodge }
        };

        // Send inputs to server and clients
        foreach (var input in inputs)
        {
            server.ReceiveInput(input);
            clientA.AddInput(input);
            clientB.AddInput(input);
        }

        // Run 30 ticks
        for (int t = 0; t < 30; t++)
        {
            clientA.Predict();
            clientB.Predict();

            server.Step();

            clientA.Reconcile(server.PlayerA, server.PlayerB);
            clientB.Reconcile(server.PlayerB, server.PlayerA);

            Console.WriteLine($"Tick {t:00} | ClientA=Self:{clientA.Self.State,-12} Other:{clientA.Other.State,-12} || ClientB=Self:{clientB.Self.State,-12} Other:{clientB.Other.State,-12} || ServerA={server.PlayerA.State,-12} ServerB={server.PlayerB.State,-12}");
            Thread.Sleep(50);
        }

        Console.WriteLine("\n=== Simulation Complete ===");
    }
}