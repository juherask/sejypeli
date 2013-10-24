using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class RuudukkoEsimerkki : PhysicsGame
{
    Image pelaajaKuva = LoadImage("pelaaja");

    PhysicsObject LuoBetoniseina()
    {
        PhysicsObject betoni = new PhysicsObject(50, 50);
        betoni.Color = Color.Gray;
        return betoni;
    }

    PhysicsObject LuoTiiliseina()
    {
        PhysicsObject tiili = new PhysicsObject(50, 50);
        tiili.Color = Color.Brown;
        return tiili;
    }

    PhysicsObject LuoPelaaja()
    {
        PhysicsObject pelaaja = new PhysicsObject(50, 50);
        pelaaja.Image = pelaajaKuva;
        return pelaaja;
    }

    void LuoKentta()
    {
        TileMap kentta = TileMap.FromFile("taso1.txt");
        kentta['#'] = LuoBetoniseina;
        kentta['='] = LuoTiiliseina;
        kentta['p'] = LuoPelaaja;
        kentta.Insert(50, 50);
    }

    public override void Begin()
    {
        LuoKentta();

        // Kirjoita ohjelmakoodisi tähän

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

    }
}
