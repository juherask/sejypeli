using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class KukkulanKuningas : PhysicsGame
{
    PlatformCharacter2 pelaaja;

    void LuoPelaaja(Vector paikka, double korkeus, double leveys)
    {
        pelaaja = new PlatformCharacter2(leveys/2, korkeus-1);
        pelaaja.Position = paikka;
        pelaaja.Color = Color.Red;
        pelaaja.Weapon = new PlasmaCannon(20, 10);
        Add(pelaaja);
    }
    void LuoVihollinen(Vector paikka, double korkeus, double leveys)
    {
        PlatformCharacter2 vihu = new PlatformCharacter2(leveys / 2, korkeus - 1);
        vihu.Color = Color.DarkForestGreen;
        vihu.Position = paikka;
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
        kentta.SetTileMethod(Color.FromHexCode("00FF21"), LuoVihollinen);
        kentta.SetTileMethod(Color.Red, LuoPelaaja);
        kentta.Optimize(Color.Black);
        kentta.Execute(30, 30);

        Gravity = new Vector(0, -1000);

    }

    void Liikuta(Direction suunta)
    {
        pelaaja.Walk(suunta);    
    }
    void Hyppaa(double korkeus)
    {
        pelaaja.Jump(korkeus);
    }

    void Ammu()
    {
        pelaaja.Weapon.Shoot();
    }
    
    public override void Begin()
    {
        LuoKentta();

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape,
            ButtonState.Pressed,
            ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down,
            Liikuta, "Pelaaja liikkuu", Direction.Left);
        Keyboard.Listen(Key.Right, ButtonState.Down,
            Liikuta, "Pelaaja liikkuu", Direction.Right);
        Keyboard.Listen(Key.Up, ButtonState.Down,
            Hyppaa, "Pelaaja hyppaa", 400.0);
        Keyboard.Listen(Key.Space, ButtonState.Pressed,
            Ammu, "Ammu aseella");
    }
}
