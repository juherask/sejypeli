/* 
 * A prototype of a game where Monsters and Humans wrestle for a ownership of a patch of forest.
 * This source code is licensed by CC BY-NC-ND 4.0 (Attribution-NonCommercial-NoDerivatives) license.
 * http://creativecommons.org/licenses/by-nc-nd/4.0/
 * 
 * Being a prototype some shortcuts in coding convetions have been used:
 * - Member visibility has not been explicitly defined
 * - Some resource names are in Finnish which is inconsistent
 * - Class is aready bit longish, code should be split to few different files (using regions is bandaid)
 * 
 * TODO:
 * - Make Gatherer to return to deploy point via gather point (where it drops off resources).
 * - When a unit in deploy point leaves (is kicked off) further queue.
 * - Log sprite has a stump? Should it be plantable? ->
 *      Would make the implementation more complex. Therefore postponed. Otherwise I see no reason why not.
 */

using System;

static class Ohjelma
{
#if WINDOWS || XBOX
    static void Main(string[] args)
    {
        using (MorotVsIhmiset game = new MorotVsIhmiset())
        {
#if !DEBUG
            game.IsFullScreen = true;
#endif
            game.Run();
        }
    }
#endif
}
