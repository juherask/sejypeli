using System;
using Jypeli;
using Jypeli.Controls;
using Jypeli.Widgets;

/* TODO:
 * pelattavuuden hierontaa: Näppäimen painaminen pohjaan rekisteröidään heti, mutta toistoja tehdään harvemmalla sykkeellä (timer?). Poistetaan timer kun näppäin nostetaan ylös.
 */
public class JyTetris : Game
{
    //** Vakiot **//

    int ALKUPAIKKA = 7;
    // Käytämme perustetris-asettelua 10x20
    int KUILUN_LEVEYS = 10;
    int KUILUN_KORKEUS = 20;
    double PalikanKoko; // säätyy ruudun mukaan

    //** Pelitilanne **//
    
    Timer paivitysAjastin;
    GameObject pelattavaPalikka;
    GameObject seuraavaPalikka;
    int rivejaPoistettu = 0;

    //** Näytöt **//

    IntMeter pisteLaskuri = new IntMeter(0);
    IntMeter tasoLaskuri = new IntMeter(1);

    //** Pelin koodi **//

    public override void Begin()
    {
        // Palikan koko määräytyy kuvaruudun koon mukaan. Käytämme perustetris-
        //  asettelua 10x20 + 2 ylärivin piiloriviä + 1 marginaalia joka suuntaan.
        PalikanKoko = Screen.Height / (KUILUN_KORKEUS + 2 + 1 + 1);

        LisaaNaytot();
        AsetaTaso(0, 1);
        LisaaReunat();
        

        seuraavaPalikka = UusiPalikka();
        pelattavaPalikka = UusiPalikka();
        // Tuo pelattava seuraavan palikan paikalta keskelle
        pelattavaPalikka.X -= PalikanKoko*(KUILUN_LEVEYS / 2 + 5);

        // Säädä ruudun päivitysnopeutta, jotta näppäimenpainannuksia ei tule liikaa
        this.TargetElapsedTime = TimeSpan.FromSeconds(1f / 30);
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Left, ButtonState.Pressed, SirraPalikkaa, "Siirrä palikkaa vasemmalle", -1, 0);
        Keyboard.Listen(Key.Right, ButtonState.Pressed, SirraPalikkaa, "Siirrä palikkaa oikealle", 1, 0);
        Keyboard.Listen(Key.Down, ButtonState.Down, SirraPalikkaa, "Pudota palikkaa", 0, 1);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, PyoraytaPalikkaa, "Pyöräytä palikkaa");
    }

    void LisaaNaytot()
    {
        Label pisteNaytto = new Label();
        pisteNaytto.X = PalikanKoko * (KUILUN_LEVEYS / 2 + 5);
        pisteNaytto.Y = PalikanKoko * (ALKUPAIKKA-4);
        pisteNaytto.TextColor = Color.White;
        pisteNaytto.Title = "PISTEET";
        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);

        Label tasoNaytto = new Label();
        tasoNaytto.X = PalikanKoko * (KUILUN_LEVEYS / 2 + 5);
        tasoNaytto.Y = PalikanKoko * (ALKUPAIKKA-3);
        tasoNaytto.TextColor = Color.White;
        tasoNaytto.Title = "TASO";
        tasoNaytto.BindTo(tasoLaskuri);
        Add(tasoNaytto);

        // Kutsutaan tason nopeuden asettavaa aliohjelmaa aina kun laskurin arvo muuttuu.
        tasoLaskuri.Changed += AsetaTaso;
    }

    void LisaaReunat()
    {
        var oikeaReuna = new GameObject(PalikanKoko, PalikanKoko * KUILUN_KORKEUS);
        oikeaReuna.Color = Color.DarkGray;
        oikeaReuna.X = PalikanKoko * -(KUILUN_LEVEYS / 2 + 1);
        oikeaReuna.Y = PalikanKoko * -0.5;
        oikeaReuna.Tag = "reuna";
        Add(oikeaReuna);

        var vasenReuna = new GameObject(PalikanKoko, PalikanKoko * KUILUN_KORKEUS);
        vasenReuna.Color = Color.DarkGray;
        vasenReuna.X = PalikanKoko * (KUILUN_LEVEYS / 2 + 0);
        vasenReuna.Y = PalikanKoko * -0.5;
        vasenReuna.Tag = "reuna";
        Add(vasenReuna);

        var pohja = new GameObject(PalikanKoko * (KUILUN_LEVEYS+2), PalikanKoko);
        pohja.Color = Color.DarkGray;
        pohja.X = PalikanKoko * -0.5;
        pohja.Y = PalikanKoko * -(KUILUN_LEVEYS+1);
        pohja.Tag = "reuna";
        Add(pohja);
    }

    void AsetaTaso(int edellinenTaso, int nykyinenTaso)
    {
        if (paivitysAjastin!=null)
        {
            paivitysAjastin.Stop();
        }
           
        // Ajastin tikittää kuin kello ja aina tasaisin väliajoin vie peliä
        //  eteenpäin kutsumalla PaivitaPelitilanne-aliohjelmaa.
        paivitysAjastin = new Timer();
        paivitysAjastin.Timeout += PaivitaPelitilanne;
        paivitysAjastin.TimesLimited = false;
        paivitysAjastin.Interval = 1.0 / nykyinenTaso;
        paivitysAjastin.Start();
    }

    void LisaaAliNelio(GameObject palikka, double sivulle, double alas)
    {
        var nelio = new GameObject(PalikanKoko-2, PalikanKoko-2);
        nelio.Color = palikka.Color;
        nelio.Position = new Vector(sivulle * PalikanKoko, alas * PalikanKoko);
        palikka.Add(nelio);
    }

    // Kaikki palikat tehdään tässä. Palikka koostuu nurkkaneliöstä (GameObject) ja 
    //  sen lapsiobjekteiksi tehdyistä lisäneliöistä.
    //  -> http://i.stack.imgur.com/JLRFu.png
    GameObject UusiPalikka()
    {
        // Valitse satunnainen palikkatunniste ... (käytetään standardinmukaisia termiinien nimiä)
        char satunnaisPalikkaTunniste = RandomGen.SelectOne('O'/*neliö*/, 'L', 'J', 'I', 'T', 'S', 'Z' );

        /// ... ja luo sitä vastaava palikka
        var palikka = new GameObject(0, 0);
        palikka.X = PalikanKoko * (KUILUN_LEVEYS / 2 + 5);
        palikka.Y = PalikanKoko * ALKUPAIKKA;
        switch (satunnaisPalikkaTunniste)
	    {
            case 'O':
                palikka.Color = Color.Yellow;
                LisaaAliNelio(palikka, 0.5, 0.5);
                LisaaAliNelio(palikka, -0.5, 0.5);
                LisaaAliNelio(palikka, 0.5, -0.5);
                LisaaAliNelio(palikka, -0.5, -0.5);
                // Nämä tarvitaan, jotta palikka osuu "jakoon"
                palikka.X += PalikanKoko * 0.5;
                palikka.Y += PalikanKoko * 0.5;
                break;
            case 'L':
                palikka.Color = Color.Orange;
                LisaaAliNelio(palikka, 0, 0);
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, 1, 0);
                LisaaAliNelio(palikka, 1, 1);
                break;
            case 'J':
                palikka.Color = Color.Blue;
                LisaaAliNelio(palikka, 0, 0);
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, -1, 1);
                LisaaAliNelio(palikka, 1, 0);
                break;
            case 'I':
                palikka.Color = Color.DarkCyan;
                LisaaAliNelio(palikka, -1.5, 0.5);
                LisaaAliNelio(palikka, -0.5, 0.5);
                LisaaAliNelio(palikka, 0.5, 0.5);
                LisaaAliNelio(palikka, 1.5, 0.5);
                palikka.X += PalikanKoko * 0.5;
                palikka.Y += PalikanKoko * 0.5;
                break;
            case 'T':
                palikka.Color = Color.Purple;
                LisaaAliNelio(palikka, 0, 0);
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, 0, 1);
                LisaaAliNelio(palikka, 1, 0);
                break;
            case 'S':
                palikka.Color = Color.Green;
                LisaaAliNelio(palikka, 0, 0);
                LisaaAliNelio(palikka, -1, 0);
                LisaaAliNelio(palikka, 0, 1);
                LisaaAliNelio(palikka, 1, 1);
                break;
            case 'Z':
                palikka.Color = Color.Red;
                LisaaAliNelio(palikka, 0, 0);
                LisaaAliNelio(palikka, 1, 0);
                LisaaAliNelio(palikka, 0, 1);
                LisaaAliNelio(palikka, -1, 1);
                break;
		    default:
                break;
	    }
        palikka.Tag = satunnaisPalikkaTunniste;
        Add(palikka);
        return palikka;
    }

    // Törmääkö palikka toiseen palikkaan (Tägi on merkki, eli char) tai reunaan (Tägi on "reuna")
    bool TarkistaTormays(GameObject palikalle)
    {
        foreach (var nelio in palikalle.Objects)
        {
            foreach (GameObject tormays in GetObjectsAt(nelio.AbsolutePosition))
            {
                if ((tormays.Tag is char || tormays.Tag=="reuna") &&
                    tormays != palikalle &&
                    !palikalle.Objects.Contains(tormays))
                    return true;
            }
        }
        return false;
    }
    
    // Tätä kutsuttaessa pudota palikoita, laske pisteitä ym.
    void PaivitaPelitilanne()
    {
        var painovoima = new Vector(0, -PalikanKoko);
        pelattavaPalikka.Position += painovoima;

        // Törmäsikö palikka alaspäin siirryttyään maahan?
        if (TarkistaTormays(pelattavaPalikka))
        {
            pelattavaPalikka.Position -= painovoima;
            pelattavaPalikka = seuraavaPalikka;
            pelattavaPalikka.X -= PalikanKoko * (KUILUN_LEVEYS / 2 + 5);
            seuraavaPalikka = UusiPalikka();

            int rivejaPoistettiinLkm = EtsiJaPoistaTaydetRivit();
            int pisteitaLisaa = LaskePisteet(tasoLaskuri.Value, rivejaPoistettiinLkm);
            pisteLaskuri.AddValue(pisteitaLisaa);

            rivejaPoistettu += rivejaPoistettiinLkm;
            if (rivejaPoistettu / 10 + 1 != tasoLaskuri.Value)
            {
                tasoLaskuri.SetValue(rivejaPoistettu / 10 + 1);
            }
        }
    }

    // Tarkistetaan täydet rivit
    int EtsiJaPoistaTaydetRivit()
    {
        int taydetRivitLkm = 0;

        // Käy läpi kaikki rivit alkupaikasta ruudun alalaitaan
        for (double y = PalikanKoko * ALKUPAIKKA; y > -Screen.Height / 2; y-=PalikanKoko )
        {
            int palikoitaRivilla = 0;
            // Peliobjekti tunnistetaan palikaksi siitä, että täginä on yksikirjaiminen palikan koodi 'O', 'L' jne.
            foreach (GameObject palikka in GetObjects(ob => ob.Tag is char))
            {
                foreach (var nelio in palikka.Objects)
                {
                    // Liukuluvut (double/float) eivät koskaan ole ihan samat, siksi tutkitaan
                    //  ovatko ne "likipitäen", eli PalikanKoko/2 päässä, samat.
                    if (Math.Abs(nelio.AbsolutePosition.Y - y) < PalikanKoko / 2)
                    {
                        palikoitaRivilla+=1;
                    }
                }
            }
            // Löysimme täyden rivin... 
            if (palikoitaRivilla == KUILUN_LEVEYS)
            {
                taydetRivitLkm += 1;

                // ... joten tehdään tarvittavat toimenpiteet ...
                foreach (GameObject palikka in GetObjects(ob => ob.Tag is char))
                {
                    if (palikka == pelattavaPalikka || palikka==seuraavaPalikka)
                    {
                        continue;
                    }
                    foreach (var nelio in palikka.Objects)
                    {
                        // ... kuten poistetaan neliöt täydeltä riviltä ... 
                        if (Math.Abs(nelio.AbsolutePosition.Y - y) < PalikanKoko / 2)
                        {
                            nelio.Destroy();
                        }

                        // ... ja pudotetaan kaikkia yläpuolella olevia alaspäin.
                        else if (nelio.AbsolutePosition.Y > y + PalikanKoko / 2)
                        {
                            // Isäntäpalikka voi olla pyörähtänyt, joten siirretään sen näkökulmasta "alaspäin"
                            nelio.Position += Vector.FromLengthAndAngle(PalikanKoko, -palikka.Angle-Angle.RightAngle);
                        }
                    }
                    // Jos kaikki palikan neliöt on poistettu koko palikka voidaan tuhota.
                    if (palikka.Objects.Count == 0)
                    {
                        palikka.Destroy();
                    }
                }
            }
        }

        return taydetRivitLkm;
    }

    int LaskePisteet(int taso, int riveja)
    {
        return 100 * (riveja*2) * taso;
    }

    void SirraPalikkaa(int askeleitaSivuttain, int askeleitaYlosalas)
    {
        var siirto = new Vector(PalikanKoko * askeleitaSivuttain, -PalikanKoko * askeleitaYlosalas);
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
