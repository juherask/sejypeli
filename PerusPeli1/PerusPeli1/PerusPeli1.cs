using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class PerusPeli1 : Game
{
    void TekstiSyotetty(InputWindow ikkuna)
    {
        // Luetaan käyttäjän syöttämä luku
        string syote = ikkuna.InputBox.Text;
        int luku = int.Parse(syote);

        // Tulosta "morjens" luku-kertaa.
        for (int i = 0; i < luku; i++)
        {
            MessageDisplay.Add("Morjentes, kamu");
        }
    }

    public override void Begin()
    {
        InputWindow lukuIkkuna =
            new InputWindow("Anna luku, plz");
        lukuIkkuna.TextEntered += TekstiSyotetty;
        Add(lukuIkkuna);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

    }
}
