using System;

static class Ohjelma
{
#if WINDOWS || XBOX
    static void Main(string[] args)
    {
        using (VihaisetLinnut game = new VihaisetLinnut())
        {
#if !DEBUG
            game.IsFullScreen = true;
#endif
            game.Run();
        }
    }
#endif
}
