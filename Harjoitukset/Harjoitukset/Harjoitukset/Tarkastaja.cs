using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public abstract class Tarkistaja : PhysicsGame
{
    int tehtava = 1;

    public override void Begin()
    {
        Timer.SingleShot(0.33, TarkistaTehtava);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }

    private void TarkistaTehtava()
    {
        bool oikein = true;
        switch (tehtava)
        {
            case 1:
                for (int i = 0; i < 100; i++)
                {
                    int a = RandomGen.NextInt(10);
                    int b = RandomGen.NextInt(10);
                    if ( a+b != Tehtava1(a,b ) )
                    {
                        oikein = false;
                        break;
                    }
                }
                break;
            case 2:
                double TOLERANCE = 0.01;
                for (int i = 0; i < 100; i++)
                {
                    double x = RandomGen.NextDouble(-10.0, 10.0);
                    double y = RandomGen.NextDouble(-10.0, 10.0);
                    double z = RandomGen.NextDouble(-10.0, 10.0);
                    double ka = (x + y + z) / 3;
                    if ( Math.Abs(ka - Tehtava2(x,y,z)) > TOLERANCE )
                    {
                        oikein = false;
                        break;
                    }
                }
                break;
            case 3:
                //TODO: Aki/Jussi tarkista
                break;
            case 4:
                //TODO: Aki/Jussi tarkista
                break;
            case 5:
                //TODO: Aki/Jussi tarkista
                break;
            case 6:
                //TODO: Aki/Jussi tarkista
                break;
            case 7:
                //TODO: Aki/Jussi tarkista
                break;
            case 8:
                //TODO: Aki/Jussi tarkista
                break;
            case 9:
                //TODO: Aki/Jussi tarkista
                break;
            case 10:
                //TODO: Aki/Jussi tarkista
                break;
            default:
                //TODO: Aki/Jussi tarkista
                oikein = false;
                break;
        }

        if (oikein)
        {
            tehtava++;
        }
        // Try as long as it takes.
        Timer.SingleShot(0.33, TarkistaTehtava);
    }


    /*
     * Tehtävänanto: Palauta muuttujien a ja b summa (plus lasku)
     */
    public abstract int Tehtava1(int a, int b);

    /*
     * Tehtävänanto: Palauta muuttujien z, y ja z keskiarvo
     * http://tilastokoulu.stat.fi/verkkokoulu_v2.xql?page_type=sisalto&course_id=tkoulu_tlkt&lesson_id=4&subject_id=4
     */
    public abstract double Tehtava2(double x, double y, double z);

    /*
     * Tehtävänanto: Lisää peliin pallo 
     */
    public abstract void Tehtava3();

    /*
     * Tehtävänanto: Lisää PUNAINEN pallo peliin satunnaiseen paikkaan
     * Tässä tehdään satunnainen paikka korkeintaan 100.0 päähän ruudun keskipisteestä
     *   Vector paikka = RandomGen.NextVector(0.0, 100.0);
     */
    public abstract void Tehtava4();

    /*
     * Tehtävänanto: Lisää peliin n kpl satunnaisessa paikassa olevaa VALKOISTA palloa
     * (vinkki, käytä for-silmukkaa)
     */
    public abstract void Tehtava5(int n);

    /*
     * Tehtävänanto: Lisää peliin reunat joka puolelle
     */
    public abstract void Tehtava6();
    
    /*
     * Tehtävänanto: Lyö PUNAISELLE pallolle vauhtia satunnaiseen suuntaan
     *  (vinkki, tee Tehtava4-aliohjelmassa lisättävästä pallosta luokkamuuttuja) 
     */
    public abstract void Tehtava7();

    /*
     * Tehtävänanto: Kun kaksi VALKOISTA palloa osuu toisiinsa, pistä ne katoamaan
     *  lisäpisteitä jos saat ne räjähtämään (kersku siitä kaverille ja opettajille :)
     *  (vinkki, tarvitset törmäyskäsittelijää ja uuden aliohjelman)
     */
    public abstract void Tehtava8();
        

    /*
     * Tehtävänanto: Aina kun välilyöntiä painetaan, lyö PUNAISELLE pallolle lisää vauhtia.
     * (vinkki: uudelleenkäytä Tehtava7-aliohjelmaa kutsumalla sitä näin "Tehtava7();")
     */
    public abstract void Tehtava9();

    /*
     * Tehtävänanto: Lisää ruudulle laskuri, joka pitää kirjaa siitä montako VALKOISTA
     *  palloa on vielä pelissä. Kun kaikki valkoiset pallot ovat kadonneet, lisää uusi
     *  satsi palloja käyttäen Tehtava5()-aliohjelmaa.
     */
    public abstract void Tehtava10();
}
