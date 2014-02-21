using System;
using Jypeli;
using Jypeli.Controls;

using PyO = Jypeli.PhysicsObject;
using BS = Jypeli.ButtonState;
using Rng = Jypeli.RandomGen;
using Vec = Jypeli.Vector;

/*
 * Minimalistinen Flappy Bird klooni.
 */
public class Flappy : PhysicsGame
{
    PyO lintu;
    PyO maa;

    public override void Begin()
    {
        lintu = new PyO(40, 20, Shape.Circle);
        Add(lintu);

        maa = Level.CreateBottomBorder();
        Gravity = new Vector(0, -800);

        LuoPutket();

        AddCollisionHandler(lintu, AloitaAlusta);
        Keyboard.Listen(Key.Space,BS.Released,Flap,"");
        Keyboard.Listen(Key.Escape,BS.Released,Exit,"");
    }

    void LuoPutket()
    {
        // Jotta putket ei törmäile maahan
        maa.CollisionIgnoreGroup = 1;

        double ht = Screen.Height;
        for (int i = 1; i < 10; i++)
        {    
            double y=Rng.NextDouble(-ht/5,ht/5);
            LuoPutki(250*i, y+0+ht/2);
            LuoPutki(250*i, y-200-ht/2);
        }
    }

    void LuoPutki(double x, double y)
    {
        double ht = Screen.Height;

        PyO putki = new PyO(10, ht);

        // Painovoima ei vaikuta
        putki.IgnoresPhysicsLogics = true; 
        putki.CanRotate = false;
        // Ei törmäile maahan
        putki.CollisionIgnoreGroup = 1; 
        
        putki.Position = new Vector(x, y);
        Add(putki);
        
        // Pistä putket tulemaan lintua kohti
        Vec movePos = new Vec(-ht, y);
        putki.MoveTo(movePos, 100);
    }

    void AloitaAlusta(PyO lintu, PyO kohde)
    {
        ClearAll();
        Begin();
    }

    void Flap()
    {
        lintu.Stop();
        Vector up = new Vector(0, 300);
        lintu.Hit(up);
    }
}

