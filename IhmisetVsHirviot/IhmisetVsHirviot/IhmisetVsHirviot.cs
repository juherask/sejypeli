using System;
using System.Collections.Generic;
using System.Linq;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/* TODO:
 * - Make Gatherer to return to deploy point via gather point (where it drops off resources).
 * - When a unit in deploy point leaves (is kicked off) further queue.
 * - Tree with log? Should it be plantable?
 */

public class IhmisetVsHirviot : PhysicsGame
{
    // Gameplay constants
    static double unitMoveSpeed = 50.0;
    static double maxTreeRange = 200.0;
    static double hitPerHit = -10.0;
    static double handleResourceTime = 2.0;

    static double amountPerResource = 20.0;
    static double resourcesTrickleAmount = 5.0;
    static double trickleRate = 0.33;

    Color[] BARCOLORS = new Color[] { Color.Cyan, Color.Lime, Color.Red };
    Dictionary<PlayerTeam, PlayerData> teams = new Dictionary<PlayerTeam, PlayerData>();
    List<RoadMap> paths = new List<RoadMap>();
    List<PhysicsObject> trees = new List<PhysicsObject>();
    Dictionary<PhysicsObject, Vector?> stayOnTarget = new Dictionary<PhysicsObject, Vector?>();

    Image stumpImage = LoadImage("stump");
    Image logImage = LoadImage("log");
    Image treeImage = LoadImage("tree");

    Image hPreparer = LoadImage("metsuri");
    Image hRepeller = LoadImage("reportteri");
    Image hGatherer = LoadImage("moto");

    Image mPreparer = LoadImage("haamu");
    Image mRepeller = LoadImage("hirvio");
    Image mGatherer = LoadImage("karhu");

    Image pathImage = LoadImage("ns_narrow");

    Dictionary<PlayerTeam, Vector> GatherPoints;

    public override void Begin()
    {
        SetWindowSize(1024, 768);
        Level.Background.Color = Color.GreenYellow;

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
   
        GatherPoints = new Dictionary<PlayerTeam, Vector>(){
            { PlayerTeam.Humans, new Vector(0,  Screen.Top - 135) },
            { PlayerTeam.Monsters, new Vector(0,  Screen.Bottom + 135) }
        };

        CreateMeterFactories(PlayerTeam.Humans);
        CreateMeterFactories(PlayerTeam.Monsters);

        AddPaths();

        Mouse.IsCursorVisible = true;

        Timer.SingleShot(trickleRate, () => OnResourceAdded(PlayerTeam.Humans, resourcesTrickleAmount, true));
        Timer.SingleShot(trickleRate, () => OnResourceAdded(PlayerTeam.Monsters, resourcesTrickleAmount, true));
    }

    #region LevelSetupFromFile
    void LoadLevel(string name)
    {
        TileMap kentta = TileMap.FromLevelAsset(name);
        // Paths coming from east
        kentta.SetTileMethod('w', AddPathTile, "en");
        kentta.SetTileMethod('s', AddPathTile, "ew");
        kentta.SetTileMethod('z', AddPathTile, "es");
        // Paths coming from north
        kentta.SetTileMethod('d', AddPathTile, "ne");
        kentta.SetTileMethod('f', AddPathTile, "nw");
        kentta.SetTileMethod('c', AddPathTile, "ns");
        // Paths coming from south
        kentta.SetTileMethod('y', AddPathTile, "sn");
        kentta.SetTileMethod('g', AddPathTile, "se");
        kentta.SetTileMethod('h', AddPathTile, "sw");
        // Paths coming from west
        kentta.SetTileMethod('i', AddPathTile, "wn");
        kentta.SetTileMethod('j', AddPathTile, "we");
        kentta.SetTileMethod('m', AddPathTile, "ws");
        // Tree
        kentta.SetTileMethod('t', AddTreeTile, treeImage);
        kentta.SetTileMethod('u', AddTreeTile, stumpImage);
        kentta.SetTileMethod('l', AddTreeTile, logImage);
        // Source / sink
        kentta.SetTileMethod('1', AddDeployPoint, PlayerTeam.Humans);
        kentta.SetTileMethod('2', AddDeployPoint, PlayerTeam.Monsters);

        kentta.Execute(60, 60);

        // Give some time for the added objects to be added and then build path points
        Timer.SingleShot(0.1, BuildPathsFromTiles);
    }
    void BuildPathsFromTiles()
    {
        // Start from human base and built from there
        var humanBaseTile = GetObjectAt(GatherPoints[PlayerTeam.Humans]);
        // TODO: For now, only 3 way tile is supported
        if (humanBaseTile.Tag != "esw") throw new NotSupportedException("Only 3 way base tiles are supported in the prototype");

        
        for (int i = 0; i < 3; i++)
		{
            List<Vector> naviPts = new List<Vector>();
            Vector toPt = humanBaseTile.Position;
            naviPts.Add(toPt);
			do {
                var tile = GetObjectAt(toPt);
                toPt = tile.Position; // Avoid straying from the grid
                char direction = ((string)tile.Tag)[1];
                if (tile.Tag=="esw")
                {
                    if (i==0) 
                        direction = 'e';
                    else if (i==1)
                        direction = 'w';
                    else if (i==2)
                        direction = 's';
                }
                
                switch ( direction )
	            {
                    case 'n':
                        toPt = toPt + new Vector(0, +60);// to north
                        break;
		            case 'e':
                        toPt = toPt + new Vector(-60, 0);// to east
                        break;
                    case 'w':
                        toPt = toPt + new Vector(+60, 0);// to west
                        break;
                    case 's':
                        toPt = toPt + new Vector(0, -60);// to south
                        break;
                    default:
                        break;
	            }
                naviPts.Add(tile.Position);
            } while (toPt!=GatherPoints[PlayerTeam.Monsters]);
            naviPts.Add(GatherPoints[PlayerTeam.Monsters]);

            RoadMap path = new RoadMap(naviPts);
            path.DefaultWidth = 10.0;
            path.Insert();
            paths.Add(path);

            foreach (var segment in path.Segments)
            {
                // used only for guiding units
                segment.IsVisible = false;
            }
		}
    }
    void AddPathTile(Vector pos, double width, double height, string shape)
    {
        GameObject path = new GameObject(width, height);
        // only one from-to pair image is in resources, so try both ways
        try 
	    {
            path.Image = LoadImage(shape);
	    }
	    catch (Exception)
	    {
            path.Image = LoadImage(shape.Reverse().ToString() );
	    }
        path.Position = pos;
        path.Tag = shape;
        Add(path);    
    }

    void AddTreeTile(Vector pos, double width, double height, Image treeState)
    {
        PhysicsObject tree = new PhysicsObject(width, height);
        tree.Image = treeState;
        tree.Position = pos;
        tree.IgnoresCollisionResponse = true;
        Add(tree);

        AddCollisionHandler(tree, "h", OnUnitMeetsTreeResource);
        AddCollisionHandler(tree, "m", OnUnitMeetsTreeResource);

        trees.Add(tree);
    }

    void AddDeployPoint(Vector pos, double width, double height, PlayerTeam team)
    {
        string shape = (team == PlayerTeam.Humans ? "esw" : "enw");
        GameObject crossroad = new GameObject(width, height);
        crossroad.Image = LoadImage(shape);
        crossroad.Position = pos;
        crossroad.Tag = shape;
        Add(crossroad);
        GatherPoints[team] = pos;
    }
    #endregion

    #region LevelSetupRandom
    void AddPaths()
    {
        double PATH_SEG_LEN_MIN = 50;
        double PATH_SEG_LEN_MAX = 70;
        double TO_SIDE_ANGLE = 60;

        var fromHumansToMonsters = GatherPoints[PlayerTeam.Monsters] - GatherPoints[PlayerTeam.Humans];
        var fromMonstersToHumans = GatherPoints[PlayerTeam.Humans] - GatherPoints[PlayerTeam.Monsters];
        // TODO: Load these from files OR generate progomatically
        List<Vector[]> pathsPts = new List<Vector[]>();

        for (int i = -1; i <= 1; i++)
        {
            pathsPts.Add(
                new Vector[] // first path
                {
                    GatherPoints[PlayerTeam.Humans],
                    GatherPoints[PlayerTeam.Humans] + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN,PATH_SEG_LEN_MAX),
                        fromHumansToMonsters.Angle+Angle.FromDegrees(i*TO_SIDE_ANGLE)),
                    GatherPoints[PlayerTeam.Humans] + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*2,PATH_SEG_LEN_MAX*2),
                        fromHumansToMonsters.Angle+RandomGen.NextAngle(Angle.FromDegrees(i*TO_SIDE_ANGLE-10), Angle.FromDegrees(i*TO_SIDE_ANGLE+10)) ),
                    GatherPoints[PlayerTeam.Humans] + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*3,PATH_SEG_LEN_MAX*3),
                        fromHumansToMonsters.Angle+RandomGen.NextAngle(Angle.FromDegrees(i*TO_SIDE_ANGLE-10), Angle.FromDegrees(i*TO_SIDE_ANGLE+10)) ), // midp
                    GatherPoints[PlayerTeam.Monsters] + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*2,PATH_SEG_LEN_MAX*2),
                        fromMonstersToHumans.Angle+RandomGen.NextAngle(Angle.FromDegrees(-i*TO_SIDE_ANGLE-10), Angle.FromDegrees(-i*TO_SIDE_ANGLE+10)) ),
                    GatherPoints[PlayerTeam.Monsters] + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN,PATH_SEG_LEN_MAX),
                        fromMonstersToHumans.Angle+Angle.FromDegrees(-i*TO_SIDE_ANGLE)),
                    GatherPoints[PlayerTeam.Monsters]+new Vector(0.01,0.01),
                    GatherPoints[PlayerTeam.Monsters],
                }
            );
        }

        foreach (var pathPoints in pathsPts)
        {
            RoadMap path = new RoadMap(pathPoints);
            path.DefaultWidth = 10.0;
            path.Insert();

            paths.Add(path);

            // TODO: Use CreateSegmentFunction to create path with path texture
            /*foreach (var segment in path.Segments)
            {
                segment.Image = pathImage;
                segment.TextureWrapSize = new Vector(0, segment.Height / pathImage.Height);
            }*/

            for (int i = 2; i < pathPoints.Length-2; i++)
			{
			    var point = pathPoints[i];
			    AddTrees(point, 3);
            }
        }
    }

    void AddTrees(Vector point, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Validate random point
            Vector position = point + RandomGen.NextVector(10, maxTreeRange);
            bool overlapsOtherTree = false;
            foreach (var tree in trees)
            {
                if (Vector.Distance(tree.Position, position)<40.0)
                {
                    overlapsOtherTree = true;
                    break;
                }
            }

            bool overlapsControls = false;
            if (Vector.Distance(GatherPoints[PlayerTeam.Humans], position)<200.0)
                overlapsControls = true;
            if (Vector.Distance(GatherPoints[PlayerTeam.Monsters], position)<200.0)
                overlapsControls = true;

            bool insideScreen = false;
            if (position.X > Screen.LeftSafe + 30 && position.X < Screen.RightSafe - 30 &&
                position.Y > Screen.BottomSafe + 30 && position.Y < Screen.TopSafe - 30)
                insideScreen = true;

            // Try again
            if (!insideScreen || overlapsControls || overlapsOtherTree)
            {
                i--;
                continue;
            }


            Image treeState = null;
            switch (RandomGen.NextInt(3))
	        {       
                case 0:
                    treeState = logImage;
                    break;
                case 1:
                    treeState = stumpImage;
                    break;
                case 2:
                    treeState = treeImage;
                    break;
		        default:
                    break;
	        }

            var treeObj = PhysicsObject.CreateStaticObject(treeState);
            treeObj.Position = position;
            treeObj.IgnoresCollisionResponse = true;

            Add(treeObj);
            trees.Add(treeObj);

            AddCollisionHandler(treeObj, "h", OnUnitMeetsTreeResource);
            AddCollisionHandler(treeObj, "m", OnUnitMeetsTreeResource);
        }
    }
    #endregion

    #region SetUpControls
    void AddButtons(PlayerTeam team, Vector deployPoint, int degree)
    {
        double baseAngle = team==PlayerTeam.Humans ? 205 : 125;
        double startDeg = baseAngle + 45.0 / degree;
        double stepSign = team == PlayerTeam.Humans ? 1.0 : -1.0;
        double degStep = stepSign * (180 - (45.0 / degree) * 2) / degree;

        for (int i = 0; i < degree; i++)
        {
            GameObject deployButton = new GameObject(40, 40, Shape.Triangle);
            deployButton.Angle = Angle.FromDegrees(startDeg + degStep * i - 90 );
            deployButton.Position = deployPoint + Vector.FromLengthAndAngle(75, Angle.FromDegrees(startDeg + degStep * i));
            deployButton.Color = Color.LightGray;
            Add(deployButton, 3);

            teams[team].DeployButtons[i] = deployButton;

            int j = i;
            Mouse.ListenOn(deployButton, MouseButton.Left, ButtonState.Released, () => OnDeploy(team, j), "Click do deploy unit");

            GameObject deployShadow = new GameObject(40, 40, Shape.Triangle);
            deployShadow.Angle = Angle.FromDegrees(startDeg + degStep * i - 90 );
            deployShadow.Position = deployPoint + Vector.FromLengthAndAngle(75, Angle.FromDegrees(startDeg + degStep * i)) +
                new Vector(0, -10);
            deployShadow.Color = Color.Gray;
            Add(deployShadow, 2); 
        }
    }

    void CreateMeterFactories(PlayerTeam team)
    {

        teams[team] = new PlayerData();
        for (int i = 0; i < 3; i++)
        {
            Color barBaseColor = BARCOLORS[i];
            // Next messy line just fetches enum value by index
            UnitType unit = (UnitType)(Enum.GetValues(typeof(UnitType)).GetValue(i));

            // Target when a new unit is produced
            DoubleMeter targetMeter = new DoubleMeter(0);
            targetMeter.MaxValue = 100.0; //%
            targetMeter.Value = 33.3; //%
            teams[team].UnitCreationAllocation.Add(unit, targetMeter);

            ProgressBar targetBar = new ProgressBar(100, 30, targetMeter);
            targetBar.Angle = Angle.FromDegrees(90);
            targetBar.X = (i - 1) * 50;
            targetBar.Y = team == PlayerTeam.Humans ? Screen.Top - 55 : Screen.Bottom + 55;
            targetBar.BorderColor = Color.Black;
            var bcol = Color.Darker(barBaseColor, 75);
            bcol.AlphaComponent = 125;
            targetBar.BarColor = bcol;

            targetBar.BorderColor = Color.Black;
            Add(targetBar, 2);

            Mouse.ListenOn(targetBar, MouseButton.Left, ButtonState.Released, () => ChangeResouceAllocation(team, targetBar), "Click do change resource allocation");

            // Progress to target (when a new unit is produced)
            DoubleMeter progressMeter = new DoubleMeter(0);
            progressMeter.MaxValue = 100.0; //%
            progressMeter.Value = 1.0; //%
            teams[team].UnitCreationProgress.Add(unit, progressMeter);

            //progressMeter.AddOverTime(maxGameLenInS * trickleRate, maxGameLenInS);

            ProgressBar progressBar = new ProgressBar(100, 30, progressMeter);
            progressBar.Angle = targetBar.Angle;
            progressBar.Position = targetBar.Position;
            progressBar.BorderColor = Color.Black;
            progressBar.Color = Color.Darker(barBaseColor, 150);
            progressBar.BarColor = barBaseColor;
            Add(progressBar, 1);

            progressMeter.AddTrigger(100.0, TriggerDirection.Up, () => CreateNewUnit(
                team, unit,
                targetBar.Position + new Vector(0, team == PlayerTeam.Humans ? -65 : 65),
                GatherPoints[team]
                ));
        }

        switch (team)
        {
            case PlayerTeam.Humans:
                AddButtons(team, GatherPoints[team], 3);
                break;
            case PlayerTeam.Monsters:
                AddButtons(team, GatherPoints[team], 3);
                break;
        }


    }
    #endregion

    #region ActOnSomething
    void ChangeResouceAllocation(PlayerTeam team, ProgressBar bar)
    {
        DoubleMeter clickedTarget = bar.Meter as DoubleMeter;

        // Use bar.Width because it is rotated 90deg, remember
        double wish = 100-100.0*Math.Min(1.0, Math.Max(0.0, Math.Abs(bar.Top - Mouse.PositionOnWorld.Y) / bar.Width));
        double delta = wish - clickedTarget.Value;
        clickedTarget.Value = wish;

        foreach (var item in teams[team].UnitCreationAllocation.Values)
        {
            if (item!=clickedTarget)
                item.Value += -delta / 2;
        }
    }

    void CreateNewUnit(PlayerTeam team, UnitType type, Vector spawnPoint, Vector gatherPoint)
    {
        // Use tag to detect a friend from foe
        string teamTag = "h";
        string enemyTag = "m";
        if (team==PlayerTeam.Monsters)
        {
            teamTag = "m";
            enemyTag = "h";
        }

        teams[team].UnitCreationProgress[type].Value = 0;
        PhysicsObject unit = new PhysicsObject(30, 30, Shape.Circle);
        unit.Position = spawnPoint;
        unit.Tag = teamTag;
        unit.CanRotate = false;
        DoubleMeter hp = new DoubleMeter(100.0);
        ProgressBar hpbar = new ProgressBar(30, 5, hp);
        hp.AddTrigger(50.0, TriggerDirection.Down, () => hpbar.BarColor = Color.Yellow );
        hp.AddTrigger(20.0, TriggerDirection.Down, () => hpbar.BarColor = Color.Red );
        hp.MaxValue = 100.0;
        hp.MinValue = 0.0;
        hp.LowerLimit += () => OnUnitFlee(unit);
        hpbar.Color = Color.DarkGray;
        hpbar.BarColor = Color.BrightGreen;
        hpbar.BorderColor = Color.Black;
        hpbar.Y = 15;
        Add(unit);
        unit.Add(hpbar);

        if (team==PlayerTeam.Humans)
        {
            unit.CollisionIgnoreGroup = 1;
            switch (type)
            {
                case UnitType.Preparer:
                    unit.Image = hPreparer;
                    break;
                case UnitType.Repeller:
                    unit.Image = hRepeller;
                    break;
                case UnitType.Gatherer:
                    unit.Image = hGatherer;
                    break;
                default:
                    break;
            }
        }
        else
        {
            unit.CollisionIgnoreGroup = 2;
            switch (type)
            {
                case UnitType.Preparer: 
                    unit.Image = mPreparer;
                    break;
                case UnitType.Repeller:
                    unit.Image = mRepeller;
                    break;
                case UnitType.Gatherer:
                    unit.Image = mGatherer;
                    break;
                default:
                    break;
            }
        }

        // Start moving to deploy point and add to deploy queue.
        MoveToDeployQueue(team, unit, 0);

        AddCollisionHandler(unit, enemyTag, OnEnemiesCollide);
    }

    private void MoveToDeployQueue(PlayerTeam team, PhysicsObject unit, double addResources)
    {
        Vector moveToPos = GatherPoints[team] + new Vector(40 * teams[team].DeployQueue.Count, 0);
        unit.MoveTo(moveToPos, unitMoveSpeed, () => OnCanDeploy(team));
        stayOnTarget[unit] = moveToPos;
        teams[team].DeployQueue.AddLast(unit);

        if (addResources > 0)
            OnResourceAdded(team, addResources, false);
    }

    private bool MoveToClosestTree(PhysicsObject unit, Image treeState)
    {
        // TODO. Very very ugly LINQ stuff. (OTOH it is a very short list)
        double closestTreeDistance = (trees.Where(t => t.Image == treeState)).Min(t => Vector.Distance(unit.Position, t.Position));
        PhysicsObject closestTree = (trees.Where(t => t.Image == treeState && Vector.Distance(unit.Position, t.Position) == closestTreeDistance)).First();

        // Closest full grown tree
        if (Vector.Distance(unit.Position, closestTree.Position) < maxTreeRange)
        {
            stayOnTarget[unit] = closestTree.Position;
            unit.MoveTo(closestTree.Position, unitMoveSpeed);
            return true;
        }
        return false;
    }
    #endregion

    #region EventHandlers
    void OnUnitMeetsTreeResource(PhysicsObject tree, PhysicsObject unit)
    {
        if (unit.Image == hGatherer && tree.Image == logImage)
        {
            Timer.SingleShot(handleResourceTime, () => OnOperateTree(tree, stumpImage, unit, PlayerTeam.Humans));
        }
        if (unit.Image == hPreparer && tree.Image == treeImage)
        {   
            Timer.SingleShot(handleResourceTime, () => OnOperateTree(tree, logImage, unit, PlayerTeam.Humans));
        }

        if (unit.Image == mPreparer && (tree.Image == stumpImage))
        {
            Timer.SingleShot(handleResourceTime, () => OnOperateTree(tree, treeImage, unit, PlayerTeam.Monsters));
        }
        if (unit.Image == mGatherer && tree.Image == treeImage)
        {
            Timer.SingleShot(handleResourceTime, () => OnOperateTree(tree, treeImage, unit, PlayerTeam.Monsters));
        }
    }
  
    void OnOperateTree(PhysicsObject tree, Image newState, PhysicsObject unitToQueue, PlayerTeam team)
    {
        tree.Image = newState;

        double resources = 0.0;
        if (unitToQueue.Image == mGatherer || unitToQueue.Image == mGatherer)
        {
            resources = amountPerResource;
        }

        // Unit reuse
        MoveToDeployQueue(team, unitToQueue, resources);
    }

    void OnEnemiesCollide(PhysicsObject thisCollides, PhysicsObject toThat)
    {
        if (thisCollides.Tag == "h" && toThat.Tag == "m")
        {
            OnEnemiesCollide(toThat, thisCollides);
            return;
        }

        // Already fleeing
        if (thisCollides.IgnoresCollisionResponse || toThat.IgnoresCollisionResponse)
            return;

        Vector delta = thisCollides.Position - toThat.Position;
        toThat.Hit(delta*10.0);
        thisCollides.Hit(-delta*10.0);

        double thisHitMultiplier = 1.0;
        double thatHitMultiplier = 1.0;
        //* Kauhea mörkö pelottaa metsureita ja mönkijöitä kaksinkertaisella voimalla
        if ( (thisCollides.Image == mRepeller && toThat.Image == hPreparer) ||
             (thisCollides.Image == mRepeller && toThat.Image == hGatherer) )
        {
            thisHitMultiplier = 2.0;
            thatHitMultiplier = 0.5;
        }

        //* Eläimet karkoittavat toimittajia kaksinkertaisella teholla (mörköhavainnot osoittautuivat vain metsässä jolkottelevaksi hirveksi)
        if (thisCollides.Image == mGatherer && toThat.Image == hRepeller)
        {
            thisHitMultiplier = 2.0;
            thatHitMultiplier = 0.5;
        }

        //* Mönkijä karkoittaa pärinällään eläimiä kaksinkertaisella teholla
        if (toThat.Image == hGatherer && thisCollides.Image == mGatherer)
        {
            thisHitMultiplier = 0.5;
            thatHitMultiplier = 2.0;
        }

        //* Toimittaja karkoittaa haamumörköjä ja kauheita mörköjä kaksinkertaisella teholla
        if ((toThat.Image == hRepeller && thisCollides.Image == mRepeller ) ||
            (toThat.Image == hRepeller && thisCollides.Image == mPreparer ))
        {
            thisHitMultiplier = 2.0;
            thatHitMultiplier = 0.5;
        }

        // Make sure we are locked into deadly? combat.
        //  This requires us to make sure we try to move to the end of the line.
        if ( stayOnTarget.Keys.Contains(thisCollides) && stayOnTarget[thisCollides]!=null)
            thisCollides.MoveTo(stayOnTarget[thisCollides].Value, unitMoveSpeed);
        if ( stayOnTarget.Keys.Contains(toThat) && stayOnTarget[toThat]!=null)
            thisCollides.MoveTo(stayOnTarget[toThat].Value, unitMoveSpeed);

        // First subobject is assumed to be health bar
        DoubleMeter hp1 = (DoubleMeter)(thisCollides.Objects.First() as ProgressBar).Meter;
        DoubleMeter hp2 = (DoubleMeter)(toThat.Objects.First() as ProgressBar).Meter;
        hp1.AddValue(hitPerHit * thatHitMultiplier);
        hp2.AddValue(hitPerHit * thisHitMultiplier);
    }

    void OnUnitFlee(PhysicsObject unit)
    {
        unit.StopMoveTo();
        unit.IgnoresCollisionResponse = true;
        unit.MoveTo(new Vector(unit.X > 0 ? Screen.Right + 100 : Screen.Left - 100, unit.Y), unitMoveSpeed);
    }

    void OnCanDeploy(PlayerTeam team)
    {
        foreach (var button in teams[team].DeployButtons.Values)
        {
            button.Color = Color.White;
        }
    }

    void OnDeploy(PlayerTeam team, int pathIndex)
    {
        // Disable deploy buttons
        foreach (var button in teams[team].DeployButtons.Values)
        {
            // Already disabled, cannot deploy!
            if (button.Color == Color.LightGray)
                return;

            button.Color = Color.LightGray;
        }

        // Progress the queue
        if (teams[team].DeployQueue.Count > 1)
        {
            Vector moveToPos = Vector.Zero;
            PhysicsObject moveUnit = null;
            for (int i = 2; i < teams[team].DeployQueue.Count; i++)
            {
                moveToPos = GatherPoints[team] + new Vector(40 * (i-1), 0);
                moveUnit = teams[team].DeployQueue.ElementAt(i);
                moveUnit.MoveTo(moveToPos, unitMoveSpeed);
                stayOnTarget[moveUnit] = moveToPos;
            }

            moveToPos = GatherPoints[team];
            moveUnit = teams[team].DeployQueue.ElementAt(1);
            // Allow deploy when the queue has stopped moving
            moveUnit.MoveTo(moveToPos, unitMoveSpeed, () => OnCanDeploy(team));
            stayOnTarget[moveUnit] = moveToPos;
        }

        // Deploy the first unit
        PhysicsObject unitToDeploy = teams[team].DeployQueue.First.Value;
        teams[team].DeployQueue.RemoveFirst();

        // Check that is not already fleeing
        if (!unitToDeploy.IgnoresCollisionResponse)
        {
            // determine in which turn to traverse the path
            int segmentIndex = 1;
            if (team == PlayerTeam.Monsters)
                segmentIndex = paths[pathIndex].Segments.Length - 2;

            OnMoveUnit(unitToDeploy, segmentIndex, paths[pathIndex]);
        }
    }

    void OnMoveUnit(PhysicsObject unit, int index, RoadMap path)
    {
        // Already fleeing
        if (unit.IgnoresCollisionResponse)
            return;
 
        // determine in which turn to traverse the path
        int increment = +1;
        if (unit.Tag == "m")
            increment = -1;

        if (unit.Image == hPreparer || unit.Image == mGatherer)
        {
            if (MoveToClosestTree(unit, treeImage)) return;
        }

        if (unit.Image == hGatherer)
        {
            if (MoveToClosestTree(unit, logImage)) return;
        }

        if (unit.Image == mPreparer)
        {
            if (MoveToClosestTree(unit, stumpImage)) return;
        }
    
        Vector moveToPos = path.Segments[index].Position;

        if (index + increment >= 0 && index + increment < path.Segments.Length)
        {
            unit.MoveTo(moveToPos, unitMoveSpeed, () => OnMoveUnit(unit, index + increment, path));
            stayOnTarget[unit] = null;
        }
        else
        {
            // Stop at the end of the path
            unit.MoveTo(moveToPos, unitMoveSpeed);
            stayOnTarget[unit] = moveToPos;
        }
        
    }

    void OnResourceAdded(PlayerTeam team, double amount, bool trickle)
    {
        foreach (var kvp in teams[team].UnitCreationProgress)
        {
            double portion = amount * (teams[team].UnitCreationAllocation[kvp.Key].Value / 100.0);
            kvp.Value.AddValue(portion);
        }

        if (trickle)
            Timer.SingleShot(trickleRate, () => OnResourceAdded(team, resourcesTrickleAmount, true));
    }
#endregion
}
