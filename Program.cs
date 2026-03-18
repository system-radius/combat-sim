using System;
using System.Threading;

// =======================
// ENUMS
// =======================

enum PlayerState
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

class Player
{
    public string Name;
    public PlayerState State = PlayerState.Idle;
    public int StateTimer = 0;

    public Player(string name)
    {
        Name = name;
    }

    // Apply an input if possible
    public void ApplyInput(InputCommand command)
    {
        if (State != PlayerState.Idle)
            return;

        switch (command)
        {
            case InputCommand.Attack:
                State = PlayerState.Windup;
                StateTimer = 12; // attack windup duration
                break;
            case InputCommand.Dodge:
                State = PlayerState.DodgeStartup;
                StateTimer = 3; // dodge startup
                break;
        }
    }

    // Update state machine per tick
    public void Update()
    {
        if (StateTimer > 0)
            StateTimer--;

        switch (State)
        {
            // Attack sequence
            case PlayerState.Windup:
                if (StateTimer == 0)
                {
                    State = PlayerState.Active;
                    StateTimer = 6; // active duration
                }
                break;
            case PlayerState.Active:
                if (StateTimer == 0)
                {
                    State = PlayerState.Recovery;
                    StateTimer = 18; // recovery duration
                }
                break;
            case PlayerState.Recovery:
                if (StateTimer == 0)
                    State = PlayerState.Idle;
                break;

            // Dodge sequence
            case PlayerState.DodgeStartup:
                if (StateTimer == 0)
                {
                    State = PlayerState.DodgeInvuln;
                    StateTimer = 6; // invulnerable duration
                }
                break;
            case PlayerState.DodgeInvuln:
                if (StateTimer == 0)
                {
                    State = PlayerState.Recovery;
                    StateTimer = 12; // recovery duration after dodge
                }
                break;

            // Hitstun (placeholder, not yet triggered)
            case PlayerState.Hitstun:
                if (StateTimer == 0)
                    State = PlayerState.Idle;
                break;

            case PlayerState.Idle:
            default:
                break;
        }
    }

    public bool IsInvulnerable => State == PlayerState.DodgeInvuln;
}

// =======================
// SIMULATION CLASS
// =======================

class Simulation
{
    public Player PlayerA;
    public Player PlayerB;
    public int Tick = 0;
    public int MaxTicks;

    public Simulation(Player a, Player b, int maxTicks = 30)
    {
        PlayerA = a;
        PlayerB = b;
        MaxTicks = maxTicks;
    }

    public void Run()
    {
        while (Tick < MaxTicks)
        {
            // Example inputs
            if (Tick == 2) PlayerA.ApplyInput(InputCommand.Attack);
            if (Tick == 4) PlayerB.ApplyInput(InputCommand.Dodge);

            // Update players
            PlayerA.Update();
            PlayerB.Update();

            // Output states
            Console.WriteLine($"Tick {Tick:00} | PlayerA: {PlayerA.State,-15} | PlayerB: {PlayerB.State,-15}");

            Tick++;
            Thread.Sleep(50);
        }

        Console.WriteLine("\n=== Simulation Complete ===");
    }
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

        var sim = new Simulation(playerA, playerB, maxTicks: 30);
        sim.Run();
    }
}