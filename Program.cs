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

    public override string ToString() => $"{State}({Timer})";
}

class Player
{
    public string Name;
    private PlayerState CurrentState;
    public PlayerStateType State
    {
        get => CurrentState.State;
        set => CurrentState.State = value;
    }

    public int StateTimer
    {
        get => CurrentState.Timer;
        set => CurrentState.Timer = value;
    }

    public Player(string name)
    {
        Name = name;
        CurrentState = new PlayerState(PlayerStateType.Idle);
    }

    public void SetState(PlayerStateType newState)
    {
        State = newState;
        StateTimer = PlayerState.PlayerStateDurations[newState];
    }

    public void ApplyInput(InputCommand command)
    {
        if (State != PlayerStateType.Idle)
            return;

        switch (command)
        {
            case InputCommand.Attack:
                SetState(PlayerStateType.Windup);
                break;
            case InputCommand.Dodge:
                SetState(PlayerStateType.DodgeStartup);
                break;
        }
    }

    public void Update()
    {

        switch (State)
        {
            case PlayerStateType.Windup:
                if (StateTimer == 0)
                    SetState(PlayerStateType.Active);
                break;
            case PlayerStateType.Active:
                if (StateTimer == 0)
                    SetState(PlayerStateType.Recovery);
                break;
            case PlayerStateType.Recovery:
                if (StateTimer == 0)
                    SetState(PlayerStateType.Idle);
                break;
            case PlayerStateType.DodgeStartup:
                if (StateTimer == 0)
                    SetState(PlayerStateType.DodgeInvuln);
                break;
            case PlayerStateType.DodgeInvuln:
                if (StateTimer == 0)
                    SetState(PlayerStateType.Recovery);
                break;
            case PlayerStateType.Hitstun:
                if (StateTimer == 0)
                    SetState(PlayerStateType.Idle);
                break;
        }

        if (StateTimer > 0)
            StateTimer--;
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

    // Buffer inputs keyed by tick
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
        // PlayerA hits PlayerB
        if (PlayerA.State == PlayerStateType.Active && !PlayerB.IsInvulnerable && PlayerB.State != PlayerStateType.Hitstun)
        {
            PlayerB.State = PlayerStateType.Hitstun;
            PlayerB.StateTimer = 6; // 100 ms
        }

        // PlayerB hits PlayerA
        if (PlayerB.State == PlayerStateType.Active && !PlayerA.IsInvulnerable && PlayerA.State != PlayerStateType.Hitstun)
        {
            PlayerA.State = PlayerStateType.Hitstun;
            PlayerA.StateTimer = 6; // 100 ms
        }
    }
}

// =======================
// PROGRAM ENTRY
// =======================

class Program
{
    static void Main()
    {
        int maxTicks = 30;
        var playerA = new Player("PlayerA");
        var playerB = new Player("PlayerB");

        var server = new Server(playerA, playerB);

        // Example inputs
        var inputs = new List<PlayerInput>
        {
            new PlayerInput { Tick = 2, PlayerName = "PlayerA", Command = InputCommand.Attack },
            new PlayerInput { Tick = 4, PlayerName = "PlayerB", Command = InputCommand.Dodge }
        };

        // Send all inputs to server buffer
        foreach (var input in inputs)
            server.ReceiveInput(input);

        // Run 30 ticks
        for (int t = 0; t < maxTicks; t++)
        {
            server.Step();
            Console.WriteLine($"Tick {t:00} | PlayerA: {server.PlayerA.State,-15} | PlayerB: {server.PlayerB.State,-15}");
            Thread.Sleep(50);
        }

        Console.WriteLine("\n=== Simulation Complete ===");
    }
}