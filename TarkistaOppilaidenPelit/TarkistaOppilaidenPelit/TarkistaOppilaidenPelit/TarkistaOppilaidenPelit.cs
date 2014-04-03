using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;
using System.Linq;
using System.IO;

// Oppilas, PelinNimi, Repo, Lista checkoutattavista tiedostoista/kansioista, Solutiontiedosto
class OppilasTietue 
{ 
  public string Tekija;
  public string PelinNimi;
  public string SVNRepo;
  public List<string> Noudettavat;
  public string Sol;
}

public class TarkistaOppilaidenPelit : Game
{
    string SVN_EXE = @"";
    public override void Begin()
    {
        // Kirjoita ohjelmakoodisi tähän

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        List<OppilasTietue> lista = HaeKovakoodattuListaPeleista();
        List<string> tekijat = lista.Select(ot => ot.Tekija).Distinct().ToList();

        if (lista.Count != tekijat.Count)
            throw new ArgumentException("Tekijöiden pitää olla yksilöllisiä");

        foreach (var item in lista)
        {
            if (!Directory.Exists(item.Tekija))
            {
                Directory.CreateDirectory(item.Tekija);

                // Checkout käyttäen tortoiseSVN:n cli:tä
                //  veläpä niin, että otetaan vain osa tiedostoista
                svn checkout <url_of_big_dir> <target> --depth empty
                cd <target>
                svn up <file_you_want>
            }
        }
    }

    List<OppilasTietue> HaeKovakoodattuListaPeleista()
    {
        // Jos monta oppilasta tekee samaa peliä, käytä nimenä molempia "Jaakko & Jussi"
        return new List<OppilasTietue>(){
            new OppilasTietue(){
                Tekija="Alex & JoonaR",
                PelinNimi="Zombie Swing",
                SVNRepo="https://github.com/magishark/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/Rope Swing"
                },
                Sol=@"trunk/Rope Swing/Rope Swing.sln"},

            new OppilasTietue(){
                Tekija="Antti-Jussi",
                PelinNimi="?",
                SVNRepo=@"https://github.com/aj-pelikurssi2014/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/Tasohyppelypeli1"
                },
                Sol=@"trunk/Tasohyppelypeli1/Tasohyppelypeli1.sln"},

            new OppilasTietue(){
                Tekija="Atte",
                PelinNimi="Crazy Greg",
                SVNRepo=@"https://github.com/JeesMies00/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/GrazyGreg.sln",
                    @"/trunk/GrazyGreg"
                },
                Sol=@"trunk/GrazyGreg.sln"},

            new OppilasTietue(){
                Tekija="Dani",
                PelinNimi="bojoing",
                SVNRepo=@"https://github.com/daiseri45/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/bojoing",
                },
                Sol=@"trunk/bojoing/bojoing.sln"},

            new OppilasTietue(){
                Tekija="Emil-Aleksi",
                PelinNimi="Rainbow Fly",
                SVNRepo=@"https://github.com/EA99/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/RainbowFly",
                },
                Sol=@"trunk/RainbowFly/RainbowFly.sln"},

            new OppilasTietue(){
                Tekija="Jere",
                PelinNimi="?",
                SVNRepo=@"https://github.com/jerekop/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/FysiikkaPeli1",
                    @"/trunk/FysiikkaPeli1.sln",
                },
                Sol=@"trunk/FysiikkaPeli1.sln"},

            new OppilasTietue(){
                Tekija="Joel",
                PelinNimi="Urhea Sotilas",
                SVNRepo=@"https://github.com/JopezSuomi/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/UrheaSotilas",
                },
                Sol=@"trunk/UrheaSotilas/UrheaSotilas.sln"},

            new OppilasTietue(){
                Tekija="JoonaK",
                PelinNimi="_insert name here_",
                SVNRepo=@"https://github.com/kytari/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/_Insert name here_",
                    @"/trunk/_Insert name here_.sln",
                },
                Sol=@"trunk/_Insert name here_.sln"},


            new OppilasTietue(){
                Tekija="Saku & Joeli",
                PelinNimi="Flappy derp",
                SVNRepo=@"https://github.com/EXIBEL/sejypeli.git",
                Noudettavat=new List<string>(){
                    @"/trunk/Falppy derp Saku",
                },
                Sol=@"trunk/Falppy derp Saku/Falppy derp Saku.sln"},
        };
    }
}
