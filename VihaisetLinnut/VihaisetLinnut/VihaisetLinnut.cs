using System;
using Jypeli;
using Jypeli.Controls;
using Jypeli.Assets;

public class VihaisetLinnut : PhysicsGame
{
    PhysicsObject Lintu;
    PhysicsObject Maa;
    bool OnkoLintuAmmuttu = false;

    public override void Begin() {
        Lintu = new PhysicsObject(40, 40);
        Add(Lintu);
        Lintu.Image = LoadImage("lintu");
        Lintu.Position = new Vector(-400, -350);
        Lintu.IgnoresGravity = true;

        PhysicsObject Possu = new PhysicsObject(40, 40);
        Add(Possu);
        Possu.Image = LoadImage("possu");
        Possu.Position = new Vector(+400, -350);
        AddCollisionHandler(Possu, PossuunOsui);

        LisaaKentta1();

        IsMouseVisible = true;
        Gravity = new Vector(0, -500);
        Maa = Level.CreateBottomBorder();
        SmoothTextures = false;
        
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta");
        Mouse.Listen(MouseButton.Left, ButtonState.Released, AmmuLintu, "Ammu");
    }
    void LisaaKentta1() {
        PhysicsObject palikka1 = new PhysicsObject(150, 20);
        palikka1.Position = new Vector(+400, -200);
        Add(palikka1);
        PhysicsObject palikka2 = new PhysicsObject(20, 150);
        palikka2.Position = new Vector(+450, -350);
        Add(palikka2);
        PhysicsObject palikka3 = new PhysicsObject(20, 150);
        palikka3.Position = new Vector(+350, -350);
        Add(palikka3);
    }
    void PossuunOsui(PhysicsObject osuja, PhysicsObject kohde) {
        if (kohde != Maa)
        {
            Explosion PUM = new Explosion(200);
            PUM.Position = osuja.Position;
            Add(PUM);
            osuja.Destroy();
        }
    }
    void AmmuLintu() {
        if (OnkoLintuAmmuttu==false)
        {
            Vector suunta = (Lintu.Position - Mouse.PositionOnScreen) * 3;
            Lintu.Hit(suunta);
            Lintu.IgnoresGravity = false;
            OnkoLintuAmmuttu = true;
        }
    }
}
