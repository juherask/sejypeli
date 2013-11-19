using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jypeli;
using Jypeli.Widgets;

/// <summary>
/// Esinevalikko.
/// </summary>
class Inventory : Widget
{
    /// <summary>
    /// Tapahtuma, kun esine on valittu.
    /// </summary>
    public event Action<GameObject> ItemSelected;

    /// <summary>
    /// Luo uuden esinevalikon.
    /// </summary>
    public Inventory()
        : base(new HorizontalLayout())
    {
    }

    /// <summary>
    /// Lisää esineen.
    /// </summary>
    /// <param name="item">Lisättävä esine.</param>
    /// <param name="kuva">Esineen ikoni, joka näkyy valikossa.</param>
    public void AddItem(GameObject item, Image kuva)
    {
        PushButton icon = new PushButton(kuva);
        icon.BorderColor = Color.Black;
        Add(icon);
        icon.Clicked += delegate() { SelectItem(item); };
    }

    static public Image ScaleImageDown(Image source, int scaler)
    {
        Image target = new Image(source.Width / scaler, source.Height / scaler, Color.Black);
        for (int x = 0; x < target.Width; x++)
        {
            for (int y = 0; y < target.Height; y++)
            {
                target[y,x] = source[Math.Max(0, y * scaler - 1),Math.Max(0, x * scaler - 1)];
            }
        }
        return target;

    }

    public void SelectItem(GameObject item)
    {
        if (ItemSelected != null)
        {
            ItemSelected(item);
        }
    }
}
