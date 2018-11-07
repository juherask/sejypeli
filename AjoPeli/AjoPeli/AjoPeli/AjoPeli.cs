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
    RoadMap rata;

    public override void Begin()
    {
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
        Add(auto, 1);
        auto.LinearDamping = 0.99;

        List<Vector> kauttakulkuPisteet = new List<Vector>();
        kauttakulkuPisteet.Add(new Vector(0,0));
        kauttakulkuPisteet.Add(new Vector(300, 200));
        kauttakulkuPisteet.Add(new Vector(350, 300));
        kauttakulkuPisteet.Add(new Vector(300, 400));
        kauttakulkuPisteet.Add(new Vector(150, 500));
        kauttakulkuPisteet.Add(new Vector(-250, 300));
        kauttakulkuPisteet.Add(new Vector(-300, 250));
        kauttakulkuPisteet.Add(new Vector(-350, 200));
        kauttakulkuPisteet.Add(new Vector(0, 0));
        kauttakulkuPisteet.Add(new Vector(300, -200));
        kauttakulkuPisteet.Add(new Vector(450, -400));
        kauttakulkuPisteet.Add(new Vector(350, -400));
        kauttakulkuPisteet.Add(new Vector(-150, -400));
        kauttakulkuPisteet.Add(new Vector(-300, -300));
        kauttakulkuPisteet.Add(new Vector(-250, -100));
        kauttakulkuPisteet.Add(new Vector(0, 0));
        rata = new RoadMap(kauttakulkuPisteet);
        rata.DefaultWidth = 200;
        rata.Insert();
    }

    void Kiihdyta()
    {
        double huippunopeuteen = HUIPPUNOPEUS - auto.Velocity.Magnitude;
        // Velocity on vektori, johon lisäämme vauhdin lisääntyessä lyhenevän kiihdytyksen
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
            // Käännä nopeusvektorin suuntaa (suunta, johon auto liikkuu) vasemmalle.
            auto.Velocity = Vector.FromLengthAndAngle(nopeus, auto.Velocity.Angle + Angle.FromDegrees(KAANTYVYYS * nopeus / HUIPPUNOPEUS));
        }
        if (suunta == Direction.Right)
        {
            // Käännä nopeusvektorin suuntaa (suunta, johon auto liikkuu) oikealle.
            auto.Velocity = Vector.FromLengthAndAngle(nopeus, auto.Velocity.Angle + Angle.FromDegrees(-KAANTYVYYS * nopeus / HUIPPUNOPEUS));
        }
        // Käännä auton nokka suuntaan, johon ollaan menemässä.
        auto.Angle = auto.Velocity.Angle;
    }

    protected override void Update(Time time)
    {
        base.Update(time);
        if (rata.IsInside(auto.Position))
        {
            auto.LinearDamping = 0.99;
        }
        else
        {
            // Radalta pudonnut auto hidastuu nopeammin
            auto.LinearDamping = 0.90;
        }
    }
}
