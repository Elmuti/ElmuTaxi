using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;

public class ElmuTaxi : PhysicsGame
{
    public PhysicsObject Player;
    public const double PlayerSpeedX = 5;

    protected override void Update(Time time)
    {
        base.Update(time);
        Console.WriteLine("update " + time.SinceStartOfGame.TotalSeconds.ToString());
        //if (Player != null)
            //Console.WriteLine("player abs pos: " + Player.AbsolutePosition.ToString() + "level center: " + Level.Center.ToString());

            //Player.Position = Mouse.PositionOnWorld;
    }



    private void MovePlayer(Vector dir)
    {
        Vector pos = Player.Position;
        Vector newPos = new Vector(pos.X + dir.X, pos.Y + dir.Y);
        Player.Position = newPos;
    }



    private void ConnectListeners()
    {
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.A, ButtonState.Down, MovePlayer, null, new Vector(-10, 0));
        Keyboard.Listen(Key.D, ButtonState.Down, MovePlayer, null, new Vector(10, 0));
    }


    public override void Begin()
    {
        Console.WriteLine("Game start");

        Player = new PhysicsObject(LoadImage("taxi"));
        //Player.Position = Level.Center;
        Console.WriteLine("level size:" + Level.Size);
        Player.Position = new Vector(400, 200);
        //Level.Size = new Vector();
        //Player.Image = LoadImage("taxi");
        Camera.ZoomToLevel();
        Player.Shape = Shape.Rectangle;
        Add(Player, 3);
        ConnectListeners();
    }
}

