using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class RattleAndHum : PhysicsGame
{
    PushButton demoNappi;
    PushButton peliNappi;

    double SKAALAUS = 0.20;
    SoundEffect pallonAani;
    SoundEffect tormaysAani;
    double SAMPLENPITUUS_KERROIN = 3.0;
    double pallonNopeus = 0.1;
    double pallonSijaintiX = 0.0;
    double pallonSijaintiY = 0.0;

    double alkuNopeus = 0.0;
    bool demo = false;

    GameObject kentta;
    PhysicsObject pallo;

    // Esimerkkitapausta varten, eli pallo pyörii ympyränmuotoista rataa.
    double pallonKulma = 0.0;

    void PeliTaiDemoPaalle()
    {
        demo = (demoNappi.Text == "Demo");
        ClearAll();
        Begin();
    }

    public override void Begin()
    {
        SetWindowSize(1024, 768);
        
        pallo = new PhysicsObject(60 * SKAALAUS, 60 * SKAALAUS, Shape.Circle);
        pallo.Color = Color.Red;
        Add(pallo);
        AddCollisionHandler(pallo, PalloTormaa);

        pallonAani = LoadSoundEffect("justroll");
        tormaysAani = LoadSoundEffect("clonkroll");

        // Kaksi tilaa, demo, jossa pallo kiertää ympyrää vaihtaen vauhtiaan ja pelitila (toistaiseksi ilman mailoja)
        demoNappi = new PushButton(100, 50, "Demo");
        demoNappi.Clicked += PeliTaiDemoPaalle;
        demoNappi.X = Screen.Left + 75;
        demoNappi.Y = Screen.Top - 50;
        Add(demoNappi);    

        if (demo)
        {
            demoNappi.Text = "Peli";

            kentta = new GameObject(500, 500);
            Add(kentta, -1);
            Timer.SingleShot(pallonNopeus, PalloPyorahtaa);
        }
        else
        {
            demoNappi.Text = "Demo";

            pallo.CanRotate = false;
            pallo.KineticFriction = 0.1;

            var reunanleveys = 20;
            PhysicsObject vasenLaita = PhysicsObject.CreateStaticObject(reunanleveys, 3660 * SKAALAUS + reunanleveys*2);
            vasenLaita.X = -1220 * SKAALAUS / 2 - reunanleveys/2;
            PhysicsObject oikeaLaita = PhysicsObject.CreateStaticObject(reunanleveys, 3660 * SKAALAUS + reunanleveys * 2);
            oikeaLaita.X = +1220 * SKAALAUS / 2 + reunanleveys / 2;
            PhysicsObject p1Maali = PhysicsObject.CreateStaticObject(1220 * SKAALAUS + 0, reunanleveys);
            p1Maali.Color = Color.Red;
            p1Maali.Y = -3660 * SKAALAUS / 2 - reunanleveys / 2;
            PhysicsObject p2Maali = PhysicsObject.CreateStaticObject(1220 * SKAALAUS + 0, reunanleveys);
            p2Maali.Color = Color.Blue;
            p2Maali.Y = 3660 * SKAALAUS / 2 + reunanleveys / 2;

            // Estä laitoja törmäilemästä keskenään
            vasenLaita.CollisionIgnoreGroup = 1;
            oikeaLaita.CollisionIgnoreGroup = 1;
            p1Maali.CollisionIgnoreGroup = 1;
            p2Maali.CollisionIgnoreGroup = 1;

            kentta = new GameObject(1220 * SKAALAUS + 0, 3660 * SKAALAUS + reunanleveys * 2);
            kentta.IsVisible = false;
            Add(kentta, -1);

            // Laidat ovat kimmoisia
            var kimmoisuus = 0.99;
            vasenLaita.Restitution = kimmoisuus;
            oikeaLaita.Restitution = kimmoisuus;
            p1Maali.Restitution = kimmoisuus;
            p2Maali.Restitution = kimmoisuus;
            pallo.Restitution = kimmoisuus;

            Add(vasenLaita); Add(oikeaLaita); Add(p1Maali); Add(p2Maali);


            var suunta = new Vector(10, 300);
            alkuNopeus = suunta.Magnitude;
            pallo.Hit(suunta);

            Timer.SingleShot(0.1, PalloLiikkuu);
        }

        Mouse.IsCursorVisible = true;
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }   

    void PalloPyorahtaa()
    {
        pallonAani.Stop();

        /* go through all angles from 0 to 2 * PI radians
        if (pallonKulma > 2 * Math.PI)
        {
            pallonKulma -= 2 * Math.PI;
        }*/
        pallonKulma += 0.1;

        // laske x, y ja v (nopeus)
        pallonSijaintiX = ( Math.Cos(pallonKulma) + 1.0 )/ 2.0; // 0.0-1.0
        pallonSijaintiY = ( Math.Sin(pallonKulma) + 1.0 )/ 2.0; // 0.0-1.0
        pallonNopeus = (Math.Sin(pallonKulma/5.0) + 1.0) / 2.0; // 0.0-1.0

        //MessageDisplay.Add(String.Format("x:{0:F2}, y:{1:F2}, v:{2:F2}", pallonSijaintiX, pallonSijaintiY, pallonNopeus));

        pallo.X = kentta.Left + pallonSijaintiX * kentta.Width;
        pallo.Y = kentta.Top - pallonSijaintiY * kentta.Height;

        var odotus = PalloLiikkuuAani(pallonAani);
        Timer.SingleShot(odotus, PalloPyorahtaa);
    }

    void PalloTormaa(PhysicsObject tormaajaPallo, PhysicsObject kohde)
    {
        pallonAani.Stop();
        tormaysAani.Stop();

        var odotus = PalloLiikkuuAani(tormaysAani);
        Timer.SingleShot(odotus, PalloLiikkuu);
    }

    void PalloLiikkuu()
    {
        pallonAani.Stop();
        tormaysAani.Stop();

        // laske x, y ja v (nopeus) pelipöydän koordinaatistossa
        pallonSijaintiX = (pallo.X + (1220 * SKAALAUS / 2)) / (1220 * SKAALAUS);
        pallonSijaintiY = 1.0-(pallo.Y + (3660 * SKAALAUS / 2))/ (3660 * SKAALAUS);
        pallonNopeus = pallo.Velocity.Magnitude / alkuNopeus; // 0.0-1.0

        MessageDisplay.Add(String.Format("x:{0:F2}, y:{1:F2}, v:{2:F2}", pallonSijaintiX, pallonSijaintiY, pallonNopeus));

        var odotus = PalloLiikkuuAani(pallonAani);
        Timer.SingleShot(odotus, PalloLiikkuu);
    }

    double PalloLiikkuuAani(SoundEffect aani)
    {
        // tulkkaa x,y ja nopeus äänenvoimakkuudeksi, stereo-efektiksi ja toistonopeudeksi
        double odotus = 0.5 - pallonNopeus / 3.0;
        double toistonopeus = (-0.5 + pallonNopeus) / 5.0;
        double volume = 0.33 + pallonSijaintiY / 1.5;
        double panorointi = pallonSijaintiX*2.0-1.0;

        //MessageDisplay.Add(String.Format("pan:{0:F2}, vol:{1:F2}, pitch:{2:F2}, , wait:{3:F2}", panorointi, volume, toistonopeus, odotus));
        aani.Play(volume, toistonopeus, panorointi);

        return odotus * SAMPLENPITUUS_KERROIN;
    }
}
