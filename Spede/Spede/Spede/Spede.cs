using System;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

public class Spede : Game
{
    GameObject punainenNappi;
    GameObject sininenNappi;
    GameObject keltainenNappi;
    GameObject vihreaNappi;

    // Pelin tila
    Color syttyneenVari;
    GameObject syttynytNappi = null;
    GameObject painettuNappi = null;
    Timer ajastin = null;

    IntMeter pisteet;

    GameObject LisaaNappi(Color vari, double x)
    {
        GameObject nappi = new GameObject(200, 200, Shape.Circle);
        nappi.Color = vari;
        nappi.X = x;
        Add(nappi);

        GameObject reuna = new GameObject(200, 200, Shape.Circle);
        reuna.Color = Color.Darker(vari, 100);
        reuna.X = x;
        reuna.Y = -20;
        Add(reuna, -1);

        return nappi;
    }
    public override void Begin()
    {
        punainenNappi = LisaaNappi(Color.DarkRed, -600.0);
        sininenNappi = LisaaNappi(Color.DarkBlue, -200.0);
        keltainenNappi = LisaaNappi(Color.Yellow, 200.0);
        vihreaNappi = LisaaNappi(Color.DarkGreen, 600.0);

        // Annetaan kaksi sekuntia pelaajalle aikaa valmistautua
        ajastin = new Timer();
        ajastin.Interval = 2.0;
        ajastin.Timeout += SytytaSatunnainenNappi;
        ajastin.Start();

        // Laskuri
        pisteet = new IntMeter(0);
        Label pisteNaytto = new Label();
        pisteNaytto.BindTo(pisteet);
        Add(pisteNaytto);

        Mouse.IsCursorVisible = true;

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Mouse.Listen(MouseButton.Left, ButtonState.Pressed, HiirtaNapautettu, "Naksauta hiirellä syttyvää nappia");
        Mouse.Listen(MouseButton.Left, ButtonState.Released, PalautaYlos, "");
    }

    void SytytaSatunnainenNappi()
    {
        // Napin pitää olla sammunut ennen uuden napin sytyttämistä
        if (syttynytNappi != null)
        {
            GameOver();
        }
        else
        {
            int nappiIndeksi = RandomGen.NextInt(4);
            if (nappiIndeksi == 0)
            {
                syttynytNappi = punainenNappi;
            }
            else if (nappiIndeksi == 1)
            {
                syttynytNappi = sininenNappi;
            }
            else if (nappiIndeksi == 2)
            {
                syttynytNappi = keltainenNappi;
            }
            else if (nappiIndeksi == 3)
            {
                syttynytNappi = vihreaNappi;
            }

            syttyneenVari = syttynytNappi.Color;
            syttynytNappi.Color = Color.Lighter(syttynytNappi.Color, 150);

            // 2 s päästä, sytytä toinen nappi.
            ajastin.Reset();
            ajastin.Interval = 2.0;
            ajastin.Start();
        }
    }

    void GameOver()
    {
        ajastin.Stop();
        Mouse.DisableAll();
        MessageDisplay.Add("Teit virheen, peli loppui!");
    }

    void HiirtaNapautettu()
    {
        GameObject nappi = GetObjectAt(Mouse.PositionOnScreen);
        if (nappi!=null)
        {
            painettuNappi = nappi;
            PainaAlas();

            if (painettuNappi != syttynytNappi)
            {
                GameOver();
            }
            else
            {
                pisteet.AddValue(1);
            }

            if (syttynytNappi!=null)
            {
                // Palauta väri
                syttynytNappi.Color = syttyneenVari;
                syttynytNappi = null;

                ajastin.Reset();
                ajastin.Interval = 0.5;
                ajastin.Start();
            }
        }
    }

    void PainaAlas()
    {
        if (painettuNappi != null)
        {
            painettuNappi.Y -= 15;
        }
    }

    void PalautaYlos()
    {
        if (painettuNappi != null)
        {
            painettuNappi.Y += 15;
        }
        painettuNappi = null;
    }
}
