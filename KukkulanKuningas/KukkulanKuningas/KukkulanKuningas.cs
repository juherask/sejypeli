using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class KukkulanKuningas : PhysicsGame
{
    PlatformCharacter pelaaja;

    void LuoPelaaja(Vector paikka, double korkeus, double leveys)
    {
        pelaaja = new PlatformCharacter(2*leveys/3, korkeus - 1);
        pelaaja.Shape = Shape.Rectangle;
        pelaaja.Position = paikka;
        pelaaja.Color = Color.Red;
        Add(pelaaja);

        AddCollisionHandler(pelaaja, PelaajaTormasi);
    }

    void LuoVihollinen(Vector paikka, double korkeus, double leveys)
    {
        PlatformCharacter vihu = new PlatformCharacter(2*leveys/3, korkeus - 1);
        vihu.Color = Color.DarkForestGreen;
        vihu.Shape = Shape.Rectangle;
        vihu.Position = paikka;
        vihu.Tag = "vihollinen";

        // Haluamme, että vihollinen tekee jotain, joten annetaan sille aivot
        PlatformWandererBrain vihollisenAivot = new PlatformWandererBrain();
        vihollisenAivot.JumpSpeed = 350;
        vihollisenAivot.TriesToJump = true;
        vihollisenAivot.Speed = 100;

        vihu.Brain = vihollisenAivot;
        
        Add(vihu);
    }
    void LuoMaata(Vector paikka, double korkeus, double leveys)
    {
        PhysicsObject maa = PhysicsObject.CreateStaticObject(korkeus, leveys);
        maa.Position = paikka;
        maa.Tag = "maa";
        maa.CollisionIgnoreGroup = 1;
        maa.Color = Color.Brown;
        Add(maa);
    }
    void LuoReuna(Vector paikka, double korkeus, double leveys)
    {
        PhysicsObject maa = PhysicsObject.CreateStaticObject(korkeus, leveys);
        maa.Position = paikka;
        maa.CollisionIgnoreGroup = 1;
        Add(maa);
    }
    void LuoKentta()
    {
        ColorTileMap kentta = ColorTileMap.FromLevelAsset("kukkula");
        kentta.SetTileMethod(Color.Black, LuoReuna);
        kentta.SetTileMethod(Color.DarkGray, LuoMaata);
        kentta.SetTileMethod(Color.FromHexCode("00FF21"), LuoPelaaja);
        kentta.SetTileMethod(Color.Red, LuoVihollinen);
        kentta.Execute(30, 30);
    }

    void AseValittu(GameObject ase)
    {
        pelaaja.Weapon = ase as Weapon;
        pelaaja.Weapon.ProjectileCollision = AmmusOsui;
    }

    void LuoAseValikko()
    {
        // Asevalikko
        Inventory inventory = new Inventory();
        inventory.Y = Screen.Top - 20;
        Add(inventory);

        Weapon rynkky = new AssaultRifle(20, 10);
        Weapon plasma = new PlasmaCannon(20, 10);
        Weapon kanuuna = new Cannon(20,10);
        kanuuna.Image = Inventory.ScaleImageDown(kanuuna.Image, 4);

        inventory.AddItem(rynkky, rynkky.Image);
        inventory.AddItem(plasma, plasma.Image);
        inventory.AddItem(kanuuna, kanuuna.Image);
        inventory.ItemSelected += AseValittu;

        // Valitse pelin aluksi rynkky
        inventory.SelectItem(rynkky);
    }

    // Kuten Minecraftissa, hyppää kun törmätään vaudissa seinään
    void PelaajaTormasi(PhysicsObject pelaaja, PhysicsObject maa)
    {
        if (maa.Tag == "maa")
        {
            if (pelaaja.Y < maa.Y + 10 && pelaaja.Y > maa.Y - 10)
            {
                Hyppaa(400.0);
            }   
        }
    }

    // Ammus tuhoaa vihollisen ja maata (ruskeat tiilet)
    void AmmusOsui(PhysicsObject ammus, PhysicsObject kohde)
    {
        if (kohde.Tag == "vihollinen" || kohde.Tag == "maa")
        {
            kohde.Destroy();
        }
        ammus.Destroy();
    }

    // Nämä kolme aliohjelmaa pistävät pelaajan tekemään
    //  asioita kun näppäimistön nappeja painellaan

    void Liikuta(Direction suunta)
    {
        //pelaaja.Walk(suunta);    
        pelaaja.Walk(suunta == Direction.Left ? -200.0 : 200.0);
    }
    void Hyppaa(double korkeus)
    {
        pelaaja.Jump(korkeus);
    }
    void Ammu()
    {
        pelaaja.Weapon.Shoot();
    }

    // Tämä alustaa ja käynnistää pelin
    public override void Begin()
    {
        LuoKentta();
        LuoAseValikko();
        Gravity = new Vector(0, -1000);

        Mouse.IsCursorVisible = true;
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Pelaaja liikkuu", Direction.Left);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Pelaaja liikkuu", Direction.Right);
        Keyboard.Listen(Key.Up, ButtonState.Down, Hyppaa, "Pelaaja hyppaa", 400.0);
        Keyboard.Listen(Key.Space, ButtonState.Pressed, Ammu, "Ammu aseella");
    }
}
