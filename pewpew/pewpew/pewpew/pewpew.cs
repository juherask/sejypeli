using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class pewpew : PhysicsGame
{
    public void LisaaKivi()
    {
        PhysicsObject kivi = new PhysicsObject(200, 200, Shape.Hexagon);
        kivi.Tag = "isokivi";
        kivi.Position = RandomGen.NextVector(Screen.Left + 100, Screen.Bottom + 100, Screen.Right- 100, Screen.Top-100);
        Add(kivi);
    }
    public override void Begin()
    {
        Level.Size = Screen.Size;
        Surfaces borders = Level.CreateBorders();
        Camera.ZoomToLevel();

        PhysicsObject pelaaja = new PhysicsObject(50, 100, Shape.Triangle);
        Weapon ase = new LaserGun(20, 20);
        ase.IsVisible = false;
        ase.Angle = Angle.FromDegrees(90);
        ase.ProjectileCollision = LaserOsui;
        pelaaja.Add(ase);
        Add(pelaaja);

        LisaaKivi();
        LisaaKivi();
        LisaaKivi();

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Space, ButtonState.Pressed, Ammu, "Ammu rakettia", ase);

        Keyboard.Listen(Key.Up, ButtonState.Down, KaytaRakettia, "Käytä rakettia", pelaaja);
        Keyboard.Listen(Key.Left, ButtonState.Down, Kaanny, "Käänny oikealle", pelaaja, 5.0);
        Keyboard.Listen(Key.Left, ButtonState.Released, Kaanny, "", pelaaja, 0.0);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaanny, "Käytä rakettia", pelaaja, -5.0);
        Keyboard.Listen(Key.Right, ButtonState.Released, Kaanny, "", pelaaja, 0.0);
    }

    void Ammu(Weapon ase)
    {
        PhysicsObject ammus = ase.Shoot();
    }

    void LaserOsui(PhysicsObject ammus, PhysicsObject kohde)
    {
        ammus.Destroy();

        if (kohde.Tag == "isokivi" || kohde.Tag == "kivi")
        {
            kohde.Destroy();
            if (kohde.Tag == "isokivi")
            {
                for (int i = 0; i < 3; i++)
                {
                    PhysicsObject sirpale = new PhysicsObject(50, 50, Shape.Rectangle);
                    sirpale.Tag = "kivi";
                    sirpale.Position = kohde.Position + RandomGen.NextVector(30, 40);
                    Add(sirpale);
                }
            }

            Explosion boom = new Explosion(100);
            boom.Force = boom.Force / 1000;
            //boom.UseShockWave = false;
            boom.Position = kohde.Position;
            Add(boom);
        }
    }

    void Kaanny(PhysicsObject pelaaja, double kulmaNopeus)
    {
        pelaaja.AngularVelocity = kulmaNopeus;
        //pelaaja.ApplyTorque(-1000);
    }

    void KaytaRakettia(PhysicsObject pelaaja)
    { 
        pelaaja.Hit(Vector.FromLengthAndAngle(10, pelaaja.Angle+Angle.FromDegrees(90) ));
    }
}
