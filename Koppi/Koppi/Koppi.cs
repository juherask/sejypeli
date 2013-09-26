using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class Koppi : PhysicsGame
{
    IntMeter pisteLaskuri;
    IntMeter elamat = new IntMeter(3, 0, 5);
    int level = 1;
    int omenoitaIlmassa = 1;

    void LuoElamalaskuri()
    {
        Label elamaNaytto = new Label();
        elamaNaytto.BindTo(elamat);
        elamaNaytto.X = Screen.Right - 50.0;
        elamaNaytto.Y = Screen.Top - 50.0;
        Add(elamaNaytto);
    }

    void LuoPistelaskuri()
    {
        pisteLaskuri = new IntMeter(0);
        Label pisteNaytto = new Label();
        pisteNaytto.BindTo(pisteLaskuri);
        pisteNaytto.X = Screen.Left + 50.0;
        pisteNaytto.Y = Screen.Top - 50.0;
        Add(pisteNaytto);
    }

    public override void Begin()
    {
        LuoPistelaskuri();
        LuoElamalaskuri();

        UusiOmena(level);
        omenoitaIlmassa = level;
        
        // Luo reunat
        Level.CreateLeftBorder();
        Level.CreateRightBorder();
        PhysicsObject pohja =
            Level.CreateBottomBorder();
        AddCollisionHandler(pohja, PutosiMaahan);

        Gravity = new Vector(0.0, -100.0);

        IsMouseVisible = true;
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }

    void UusiOmena(int level)
    {
        for (int i = 0; i < level; i++)
        {
            PhysicsObject omena = new PhysicsObject(50, 50);
            omena.Shape = Shape.Circle;
            omena.Color = Color.Red;
            omena.Y = Screen.Top;
            GameObject lehti = new GameObject(20, 20);
            lehti.Shape = Shape.Heart;
            lehti.Color = Color.Green;
            Add(omena);
            lehti.Y = 30;
            omena.Add(lehti);
            Mouse.ListenOn(omena, MouseButton.Left,
                ButtonState.Pressed, OmenaaKlikattu,
                "omenaa klikattu", omena);

            omena.Hit(RandomGen.NextVector(50, 100));
        }
    }

    void PutosiMaahan(
        PhysicsObject maa,
        PhysicsObject omena)
    {
        if (omena.Color != Color.Black)
        {
            elamat.AddValue(-1);
            omena.Color = Color.Black;
            omenoitaIlmassa = omenoitaIlmassa - 1;
        }

        OnkoKaikkiKiinni();
    }

    void OnkoKaikkiKiinni()
    {
        if (omenoitaIlmassa == 0)
        {
            level = level + 1;
            UusiOmena(level);
            omenoitaIlmassa = level;
        }
    }

    void OmenaaKlikattu(PhysicsObject klikattuOmena)
    {
        klikattuOmena.Destroy();
        pisteLaskuri.AddValue(100);
        omenoitaIlmassa = omenoitaIlmassa - 1;

        OnkoKaikkiKiinni();
    }
}
