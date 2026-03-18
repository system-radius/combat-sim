using System;
using System.Collections.Generic;
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
    Dodge,
    Hitstun
}

// =======================
// PLAYER CLASS
// =======================

class Player
{
    public string Name;
    public PlayerState State = PlayerState.Idle;
    public int StateTimer = 0; // ticks remaining in current state

    public Player(string name)
    {
        Name = name;
    }

    // Basic state machine tick update
    public void Update()
    {
        if (StateTimer > 0)
        {
            StateTimer--;
        }

        switch (State)
        {
            case PlayerState.Windup:
            case PlayerState.Active:
            case PlayerState.Recovery:
            case PlayerState.Dodge:
            case PlayerState.Hitstun:
                if (StateTimer == 0)
                    State = PlayerState.Idle;
                break;
            case PlayerState.Idle:
                // Nothing to do
                break;
        }
    }
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

    public Simulation(Player a, Player b, int maxTicks = 20)
    {
        PlayerA = a;
        PlayerB = b;
        MaxTicks = maxTicks;
    }

    // Runs the simulation
    public void Run()
    {
        while (Tick < MaxTicks)
        {
            // Update players
            PlayerA.Update();
            PlayerB.Update();

            // Output states
            Console.WriteLine($"Tick {Tick:00} | PlayerA: {PlayerA.State,-8} | PlayerB: {PlayerB.State,-8}");

            Tick++;
            Thread.Sleep(50); // optional: slow down output
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

        var sim = new Simulation(playerA, playerB, maxTicks: 20);
        sim.Run();
    }
}