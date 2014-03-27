using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

enum PlayerTeam
{
    Humans,
    Monsters
}

enum UnitType
{
    Preparer = 0,
    Repeller = 1,
    Gatherer = 2
}

class PlayerData
{
    public Dictionary<UnitType, DoubleMeter> UnitCreationAllocation = new Dictionary<UnitType, DoubleMeter>();
    public Dictionary<UnitType, DoubleMeter> UnitCreationProgress = new Dictionary<UnitType, DoubleMeter>();
    public LinkedList<GameObject> DeployQueue = new LinkedList<GameObject>();
    public Dictionary<int, GameObject> DeployButtons = new Dictionary<int, GameObject>();
}
