using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/* TODO:
 * törmäykset ja kiertojen salliminen tulisi tarkistaa vasta seuraavan updaten jälkeen? (tai update paussille siksi aikaa kun tsekkaus tehdään)
 * tulevan uuden palikan näyttö
 * pistelasku ja tasoissa eteneminen
 * katoavat rivit ja putoavat jämäpäalat
 * L ja J palikoissa jotain mätää
 */
public class JyTetris : Game
{
    //** Vakiot **//
    double PalikanKoko = 0.0;

    //** Pelitilanne **//
    int Taso = 1;
    Timer paivitysAjastin;
    GameObject pelattavaPalikka;
    GameObject palikkaPino;

    //** Aliohjelmat **//

    public override void Begin()
    {
        // Palikan koko määräytyy kuvaruudun koon mukaan. Käytämme perustetris
        //  asettelua 10x20 + 2 ylärivin piiloriviä + 1 marginaalia joka suuntaan.
        PalikanKoko = Screen.Height / (20+2+1+1);

        AsetaTaso(Taso);
        TeeReunat();

        pelattavaPalikka = UusiPalikka();

        // Säädä ruudun päivitysnopeutta, jotta näppäimenpainannuksia ei tule liikaa
        this.TargetElapsedTime = TimeSpan.FromSeconds(1f / 20);
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Left, ButtonState.Down, SirraPalikkaa, "Siirrä palikkaa vasemmalle", -1, 0);
        Keyboard.Listen(Key.Right, ButtonState.Down, SirraPalikkaa, "Siirrä palikkaa oikealle", 1, 0);
        Keyboard.Listen(Key.Down, ButtonState.Down, SirraPalikkaa, "Pudota palikkaa", 0, 1);
        Keyboard.Listen(Key.Up, ButtonState.Down, PyoraytaPalikkaa, "Pyöräytä palikkaa");
    }

    void TeeReunat()
    {
        var oikeaReuna = new GameObject(PalikanKoko, PalikanKoko * 20);
        oikeaReuna.Color = Color.DarkGray;
        oikeaReuna.X = PalikanKoko * -(10/2+1);
        oikeaReuna.Y = PalikanKoko * -0.5;
        oikeaReuna.Tag = ' ';
        Add(oikeaReuna);

        var vasenReuna = new GameObject(PalikanKoko, PalikanKoko * 20);
        vasenReuna.Color = Color.DarkGray;
        vasenReuna.X = PalikanKoko * (10/2+0);
        vasenReuna.Y = PalikanKoko * -0.5;
        vasenReuna.Tag = ' ';
        Add(vasenReuna);

        var pohja = new GameObject(PalikanKoko * 12, PalikanKoko);
        pohja.Color = Color.DarkGray;
        pohja.X = PalikanKoko * -0.5;
        pohja.Y = PalikanKoko * -11;
        pohja.Tag = ' ';
        Add(pohja);

        // Kerätään pudonneet palikat pohjalle
        palikkaPino = pohja;
    }

    void AsetaTaso(int taso)
    {
        if (paivitysAjastin!=null)
        {
            paivitysAjastin.Stop();
        }
            
        paivitysAjastin = new Timer();
        paivitysAjastin.Timeout += PaivitaPelitilanne;
        paivitysAjastin.TimesLimited = false;
        paivitysAjastin.Interval = 1.0 / taso;
        paivitysAjastin.Start();
    }

    void LisaaAliNelio(GameObject palikka, int sivulle, int alas)
    {
        var nelio = new GameObject(PalikanKoko, PalikanKoko);
        nelio.Color = palikka.Color;
        nelio.Position = new Vector(sivulle * PalikanKoko, alas * PalikanKoko);
        palikka.Add(nelio);
    }
    // Kaikki palikat tehdään tässä. Palikka koostuu nurkkaneliöstä (GameObject) ja 
    //  sen lapsiobjekteiksi tehdyistä lisäneliöistä.
    GameObject UusiPalikka()
    {
        // Valitse satunnainen palikkatunniste ... (käytetään standardinmukaisia termiinien nimiä)
        char satunnaisPalikkaTunniste = RandomGen.SelectOne('O'/*neliö*/, 'L', 'J', 'I', 'T', 'S', 'Z' );

        /// ... ja luo sitä vastaava palikka
        var palikka = new GameObject(PalikanKoko, PalikanKoko);
         
        switch (satunnaisPalikkaTunniste)
	    {
            case 'O':
                palikka.Color = Color.Yellow;
                LisaaAliNelio(palikka, 1, 0);
                LisaaAliNelio(palikka, 1, 1);
                LisaaAliNelio(palikka, 0, 1);
                break;
            case 'L':
                palikka.Color = Color.Orange;
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, -1, 1);
                LisaaAliNelio(palikka, 0, 1);
                break;
            case 'J':
                palikka.Color = Color.Blue;
                LisaaAliNelio(palikka, 1, 0);
                LisaaAliNelio(palikka, 1, 1);
                LisaaAliNelio(palikka, 0, -1);
                break;
            case 'I':
                palikka.Color = Color.DarkCyan;
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, 1, 0);
                LisaaAliNelio(palikka, 2, 0);
                break;
            case 'T':
                palikka.Color = Color.Purple;
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, 0, -1);
                LisaaAliNelio(palikka, 1, 0);
                break;
            case 'S':
                palikka.Color = Color.Green;
                LisaaAliNelio(palikka, 1, 0);
                LisaaAliNelio(palikka, 0, -1);
                LisaaAliNelio(palikka, -1, -1);
                break;
            case 'Z':
                palikka.Color = Color.Red;
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, 0, -1);
                LisaaAliNelio(palikka, 1, -1);
                break;
		    default:
                break;
	    }
        // Tämä on "haamuneliö", jota käytetään törmaysten tunnistukseen
        LisaaAliNelio(palikka, 0, 0);
        palikka.Tag = satunnaisPalikkaTunniste;
        palikka.Y = PalikanKoko * 7;
        Add(palikka);
        return palikka;
    }


    bool TarkistaTormays(GameObject palikalle)
    {
        foreach (var lapsi in palikalle.Objects)
        {
            GameObject tormaus = GetObjectAt(palikalle.Position+lapsi.Position);
            if (tormaus != null && tormaus!=palikalle && !palikalle.Objects.Contains(tormaus))
                return true;
        }
        return false;
    }
    
    // Tätä kutsuttaessa pudota palikoita, laske pisteitä ym.
    void PaivitaPelitilanne()
    {
        var painovoima = new Vector(0, -PalikanKoko);
        pelattavaPalikka.Position += painovoima;
        if (TarkistaTormays(pelattavaPalikka))
        {
            pelattavaPalikka.Position -= painovoima;
            pelattavaPalikka = UusiPalikka();
        }
    }

    void SirraPalikkaa(int sivuttain, int ylosalas)
    {
        var siirto = new Vector(PalikanKoko * sivuttain, -PalikanKoko * ylosalas);
        pelattavaPalikka.Position += siirto;
        if (TarkistaTormays(pelattavaPalikka))
        {
            pelattavaPalikka.Position -= siirto;
        }
    }

    void PyoraytaPalikkaa()
    {
        pelattavaPalikka.Angle += Angle.FromDegrees(90);
        if (TarkistaTormays(pelattavaPalikka))
        {
            pelattavaPalikka.Angle -= Angle.FromDegrees(90);
        }
    }
}
