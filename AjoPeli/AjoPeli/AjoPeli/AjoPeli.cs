using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class AjoPeli : PhysicsGame
{
    const double HUIPPUNOPEUS = 1000.0;
    const double KIIHTYVYS = 100;
    const double KAANTYVYYS = 6;
    const double JARRUVOIMA = 0.05;
    PhysicsObject auto;
    public override void Begin()
    {
        //Add(FPSDisplay);
        // TODO: Kirjoita ohjelmakoodisi tähän

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Up, ButtonState.Down, Kiihdyta, "");
        Keyboard.Listen(Key.Down, ButtonState.Down, Jarruta, "");
        Keyboard.Listen(Key.Left, ButtonState.Down, Kaanny, "", Direction.Left);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaanny, "", Direction.Right);

        auto = new PhysicsObject(100, 50);
        var ikkuna = new GameObject(20, 44);
        ikkuna.Color = Color.BlueGray;
        ikkuna.X = 20;
        auto.Add(ikkuna);
        Add(auto);

        auto.LinearDamping = 0.99;
    }


    void Kiihdyta()
    {
        double huippunopeuteen = HUIPPUNOPEUS-auto.Velocity.Magnitude;

        auto.Velocity = auto.Velocity + Vector.FromLengthAndAngle(huippunopeuteen/KIIHTYVYS, auto.Angle);
    }

    void Jarruta()
    {
        auto.Velocity = auto.Velocity * (1-JARRUVOIMA);
    }

    void Kaanny(Direction suunta)
    {
        double nopeus = auto.Velocity.Magnitude;
        if (suunta == Direction.Left)
        {
            auto.Velocity = Vector.FromLengthAndAngle(nopeus, auto.Velocity.Angle + Angle.FromDegrees(KAANTYVYYS * nopeus / HUIPPUNOPEUS));
        }
        if (suunta == Direction.Right)
        {
            auto.Velocity = Vector.FromLengthAndAngle(nopeus, auto.Velocity.Angle + Angle.FromDegrees(-KAANTYVYYS * nopeus/HUIPPUNOPEUS));
        }
        auto.Angle = auto.Velocity.Angle;
    }
}
