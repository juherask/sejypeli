using System;
using System.Collections.Generic;
using System.Linq;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

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

public class MorotVsIhmiset : PhysicsGame
{
    // Gameplay constants
    static double unitMoveSpeed = 50.0;
    static double maxTreeRange = 100.0;
    static double hitPerHit = -10.0;
    static double handleResourceTime = 2.0;
    static double treeGrowsTime = 30.0;

    static double amountPerResource = 20.0;
    static double resourcesTrickleAmount = 3.0;
    static double trickleRate = 0.33;

    static int randomTreePlacementRetries = 10;

    static Color[] barColors = new Color[] { Color.Cyan, Color.Lime, Color.Red };

    // Game state
    Dictionary<PlayerTeam, PlayerData> teams = new Dictionary<PlayerTeam, PlayerData>();
    List<RoadMap> paths = new List<RoadMap>();
    List<PhysicsObject> trees = new List<PhysicsObject>();
    Dictionary<PhysicsObject, Tuple<Vector, Action>> moveOrStayTarget = new Dictionary<PhysicsObject, Tuple<Vector, Action>>();
    Dictionary<PlayerTeam, Vector> GatherPoints;

    // Resources
    Image stumpImage = LoadImage("stump");
    Image logImage = LoadImage("log");
    Image treeImage = LoadImage("tree");
    Image saplingImage = LoadImage("taimi");

    Image hPreparer = LoadImage("metsuri");
    Image hRepeller = LoadImage("reportteri");
    Image hGatherer = LoadImage("moto");

    Image mPreparer = LoadImage("haamu");
    Image mRepeller = LoadImage("hirvio");
    Image mGatherer = LoadImage("karhu");

    Image pathSkinImage = LoadImage("ns_narrow");

    Dictionary<string, Image> preloadedPathImages = new Dictionary<string, Image>(){
        {"enw",LoadImage("enw")},
        {"esw",LoadImage("esw")},
        {"ew",LoadImage("ew")},
        {"es",LoadImage("es")},
        {"en",LoadImage("en")},
        {"ns",LoadImage("ns")},
        {"nw",LoadImage("nw")},
        {"sw",LoadImage("sw")}
    };

    #region GameSetup
    public override void Begin()
    {
        SetWindowSize(1024, 768);
        Level.Background.Color = Color.GreenYellow;
        Level.Background.Image = LoadImage("bg");
        Level.Background.TileToLevel();


        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
   
        GatherPoints = new Dictionary<PlayerTeam, Vector>(){
            { PlayerTeam.Humans, new Vector(0,  Screen.Top - 165) },
            { PlayerTeam.Monsters, new Vector(0,  Screen.Bottom + 165) }
        };

        CreateMeterFactories(PlayerTeam.Humans);
        CreateMeterFactories(PlayerTeam.Monsters);

        MultiSelectWindow mainMenu = new MultiSelectWindow("Pelin alkuvalikko", "Satunnainen kenttä", "Esimerkkikenttä", "Lataa tiedostosta", "Lopeta");
        mainMenu.AddItemHandler(0, CreateRandomLevel);
        mainMenu.AddItemHandler(1, () => LoadLevel("lev1.txt"));
        mainMenu.AddItemHandler(2, () => AskFileAndLoad());
        mainMenu.AddItemHandler(3, Exit);
        Add(mainMenu);

        Mouse.IsCursorVisible = true;
    }

    void StartGame()
    {
        Timer.SingleShot(trickleRate, () => OnResourceAdded(PlayerTeam.Humans, resourcesTrickleAmount, true));
        Timer.SingleShot(trickleRate, () => OnResourceAdded(PlayerTeam.Monsters, resourcesTrickleAmount, true));
    }

    void AskFileAndLoad()
    {
        InputWindow fileNameInput = new InputWindow("Pelikansiossa olevan kenttätiedoston nimi");
        fileNameInput.TextEntered += (input) => LoadLevel(input.InputBox.Text);
        Add(fileNameInput);
    }
    #endregion

    #region LevelSetupFromFile
    /*
     * ICD = Incoming Cardinal Direction E,W,N,S + ►
     * OCD = Outgoing Cardinal Direction E,W,N,S
     *
     *              N          N          N         N 
     *             [W]         ▼         [Y]       [I]
     *           E ► [S]W  E[D] [F]W  E[G] [H]  E[J] ◄ W
     *             [Z]        [C]         ▲        [M]
     *              S          S          S         S
     */
    void LoadLevel(string name)
    {
        TileMap kentta = TileMap.FromFile(name);
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
        kentta.SetTileMethod('T', AddTreeTile, treeImage);
        kentta.SetTileMethod('t', AddTreeTile, saplingImage);
        kentta.SetTileMethod('\'', AddTreeTile, stumpImage);
        kentta.SetTileMethod('_', AddTreeTile, logImage);
        // Source / sink
        kentta.SetTileMethod('1', AddDeployPoint, PlayerTeam.Humans);
        kentta.SetTileMethod('2', AddDeployPoint, PlayerTeam.Monsters);

        kentta.Execute(60, 60);

        // Give some time for the added objects to be added and then build path points
        Timer.SingleShot(0.1, BuildPathsFromTiles);

        StartGame();
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
			do {
                // 1. Get next path tile
                
                GameObject tile = null;
                foreach (var go in GetObjectsAt(toPt))
                {
                    string pathImageName = String.Concat(((string)go.Tag).OrderBy(c => c));
                    if (preloadedPathImages.Keys.Contains(pathImageName))
                    {
                        tile = go;
                        break;
                    }
                }
                if (tile==null)
                {
                    throw new NotSupportedException("All paths must be continuous from human base to monster base.");
                }


                // 2.Generate a navigation point for the tile for a RoadMap 

                toPt = tile.Position; // Avoid straying from the grid
                char direction = ((string)tile.Tag)[1];
                if (tile.Tag=="esw")
                {
                    if (i==0) 
                        direction = 'e';
                    else if (i==1)
                        direction = 's';
                    else if (i==2)
                        direction = 'w';
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
            // Needs one more point, which should not be exactly the same!
            naviPts.Add(GatherPoints[PlayerTeam.Monsters] + new Vector(0.01, 0.01));
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
        string pathImageName = String.Concat(shape.OrderBy(c => c));
        path.Image = LoadImage(pathImageName);
        path.Position = pos;
        path.Tag = shape;
        Add(path, -2);    
    }

    void AddTreeTile(Vector pos, double width, double height, Image treeState)
    {
        CreateTree(pos, treeState);
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
    void CreateRandomLevel()
    {
        double PATH_SEG_LEN_MIN = 60;
        double PATH_SEG_LEN_MAX = 110;
        double TO_SIDE_ANGLE = 60;
        double ANGLE_DEV = 20;

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
                        fromHumansToMonsters.Angle+RandomGen.NextAngle(Angle.FromDegrees(i*TO_SIDE_ANGLE-ANGLE_DEV), Angle.FromDegrees(i*TO_SIDE_ANGLE+ANGLE_DEV)) ),
                    GatherPoints[PlayerTeam.Humans] + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*3,PATH_SEG_LEN_MAX*3),
                        fromHumansToMonsters.Angle+RandomGen.NextAngle(Angle.FromDegrees(i*TO_SIDE_ANGLE-ANGLE_DEV), Angle.FromDegrees(i*TO_SIDE_ANGLE+ANGLE_DEV)) ), // midp
                    GatherPoints[PlayerTeam.Monsters] + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*2,PATH_SEG_LEN_MAX*2),
                        fromMonstersToHumans.Angle+RandomGen.NextAngle(Angle.FromDegrees(-i*TO_SIDE_ANGLE-ANGLE_DEV), Angle.FromDegrees(-i*TO_SIDE_ANGLE+ANGLE_DEV)) ),
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

            // Add Path as overlay
            // TODO: Use CreateSegmentFunction to create path with path texture
            Vector? fromPt = null;
            Vector? toPt = null;
            foreach (var segment in path.Segments)
            {
                fromPt = toPt;
                toPt = segment.Position;
                if (fromPt != null)
                {
                    Vector fromToVec = toPt.Value - fromPt.Value;
                    if (fromToVec != Vector.Zero)
                    {
                        var pathSkin = new GameObject(10, fromToVec.Magnitude);
                        pathSkin.Image = pathSkinImage;
                        pathSkin.Position = fromPt.Value + fromToVec / 2;
                        pathSkin.TextureFillsShape = true;
                        pathSkin.TextureWrapSize = new Vector(1.0, fromToVec.Magnitude / pathSkinImage.Height);
                        pathSkin.Angle = fromToVec.Angle + Angle.FromDegrees(90);
                        Add(pathSkin, -2);
                    }
                }
                
                segment.IsVisible = false;
            }

            for (int i = 2; i < pathPoints.Length-2; i++)
			{
			    var point = pathPoints[i];
                RandomTrees(point, 3);
            }
        }

        StartGame();
    }

    void RandomTrees(Vector point, int amount)
    {
        int retriesLeft = randomTreePlacementRetries;
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
                retriesLeft--;
                if (retriesLeft == 0)
                    break;
                continue;
            }


            Image treeState = null;
            switch (RandomGen.NextInt(4))
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
                case 3:
                    treeState = saplingImage;
                    break;
		        default:
                    break;
	        }

            CreateTree(position, treeState);
            
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
            Color barBaseColor = barColors[i];
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

    #region DoSomething
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
    void CreateTree(Vector position, Image treeState)
    {
        var tree = PhysicsObject.CreateStaticObject(treeState);
        tree.Position = position;
        tree.IgnoresCollisionResponse = true;

        Add(tree, -1);
        trees.Add(tree);

        if (treeState == saplingImage)
        {
            Timer.SingleShot(treeGrowsTime, () => tree.Image = treeImage);
        }
        if (treeState == logImage)
        {
            Timer.SingleShot(treeGrowsTime, () => 
            { 
                if (tree.Image == logImage)
                    tree.Image = stumpImage;
            });
        }

        AddCollisionHandler(tree, "h", OnUnitMeetsTreeResource);
        AddCollisionHandler(tree, "m", OnUnitMeetsTreeResource);
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
        Add(unit, 1);
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
    void MoveToDeployQueue(PlayerTeam team, PhysicsObject unit, double addResources)
    {
        Vector moveToPos = GatherPoints[team] + new Vector(
            40 * teams[team].DeployQueue.Count,
            Math.Min(1, 40 * teams[team].DeployQueue.Count)*( team==PlayerTeam.Humans?40:-40) );

        Action afterMove = () => ProgressDeployQueue(team);
        if (addResources > 0)
            afterMove = () => {
                OnResourceAdded(team, addResources, false);
                ProgressDeployQueue(team);
            };

        unit.MoveTo(moveToPos, unitMoveSpeed, afterMove);
        moveOrStayTarget[unit] = new Tuple<Vector, Action>(moveToPos, afterMove);
        teams[team].DeployQueue.AddLast(unit);

    }
    void ProgressDeployQueue(PlayerTeam team)
    {
        // Progress the queue
        if (teams[team].DeployQueue.Count > 0)
        {
            Vector moveToPos = Vector.Zero;
            PhysicsObject moveUnit = null;
            for (int i = 0; i < teams[team].DeployQueue.Count; i++)
            {
                moveToPos = GatherPoints[team] + new Vector(
                     40 * i,
                     Math.Min(1,i) * (team == PlayerTeam.Humans ? 40 : -40) );

                moveUnit = teams[team].DeployQueue.ElementAt(i);
                if (i == 0)
                {
                    Action afterMove = () => OnCanDeploy(team);
                    moveUnit.MoveTo(moveToPos, unitMoveSpeed, afterMove);
                    moveOrStayTarget[moveUnit] = new Tuple<Vector, Action>(moveToPos, afterMove);
                }
                else
                {
                    moveUnit.MoveTo(moveToPos, unitMoveSpeed);
                    moveOrStayTarget[moveUnit] = new Tuple<Vector, Action>(moveToPos, null);
                }
            }
        }
    }

    bool MoveToClosestTree(PhysicsObject unit, Image treeState)
    {
        if (trees.Where(t => t.Image == treeState).Count() == 0) return false;

        // TODO. Very very ugly LINQ stuff. (OTOH it is a very short list)
        double closestTreeDistance = (trees.Where(t => t.Image == treeState)).Min(t => Vector.Distance(unit.Position, t.Position));
        PhysicsObject closestTree = (trees.Where(t => t.Image == treeState && Vector.Distance(unit.Position, t.Position) == closestTreeDistance)).First();

        // Closest full grown tree
        if (Vector.Distance(unit.Position, closestTree.Position) < maxTreeRange)
        {
            unit.MoveTo(closestTree.Position, unitMoveSpeed);
            moveOrStayTarget[unit] = new Tuple<Vector, Action>(closestTree.Position, null);
            return true;
        }
        return false;
    }


    private void RedoMoveTo(PhysicsObject unit)
    {
        if (unit.IgnoresCollisionResponse == true)
            OnUnitFlee(unit);

        if (moveOrStayTarget.Keys.Contains(unit) && moveOrStayTarget[unit] != null)
        {
            Vector movePos = moveOrStayTarget[unit].Item1;
            Action afterMove = moveOrStayTarget[unit].Item2;

            if (afterMove != null)
            {
                unit.MoveTo(movePos, unitMoveSpeed, afterMove);
            }
            else
            {
                unit.MoveTo(movePos, unitMoveSpeed);
            }
        }
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
            Timer.SingleShot(handleResourceTime, () => OnOperateTree(tree, saplingImage, unit, PlayerTeam.Monsters));
        }
        if (unit.Image == mGatherer && tree.Image == saplingImage)
        {
            Timer.SingleShot(handleResourceTime, () => OnOperateTree(tree, saplingImage, unit, PlayerTeam.Monsters));
        }
    }
  
    void OnOperateTree(PhysicsObject tree, Image newState, PhysicsObject unitToQueue, PlayerTeam team)
    {
        if (newState == saplingImage && tree.Image != saplingImage)
            Timer.SingleShot(treeGrowsTime, () => tree.Image = treeImage);
        if (newState == logImage && tree.Image != logImage)
            Timer.SingleShot(treeGrowsTime, () => { if (tree.Image == logImage) tree.Image = stumpImage; });

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

        // Push them apart and then togehter.
        Vector delta = thisCollides.Position - toThat.Position;
        toThat.Hit(toThat.Position + delta * 6.0);
        thisCollides.Hit(thisCollides.Position - delta * 6.0);
        Timer.SingleShot(0.1, () =>
        {
            toThat.MoveTo(thisCollides.Position, unitMoveSpeed);
            thisCollides.MoveTo(toThat.Position, unitMoveSpeed);
        });



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
            thisHitMultiplier = 0.5;
            thatHitMultiplier = 2.0;
        }

        // First subobject is assumed to be health bar
        DoubleMeter hpThis = (DoubleMeter)(thisCollides.Objects.First() as ProgressBar).Meter;
        DoubleMeter hpThat = (DoubleMeter)(toThat.Objects.First() as ProgressBar).Meter;
        hpThis.AddValue(hitPerHit * thatHitMultiplier);
        hpThat.AddValue(hitPerHit * thisHitMultiplier);

        // Make sure we are locked into deadly? combat.
        //  This requires us to make sure we try to move to the end of the line.
        // TODO: Find out why sometimes the unit shoots away. This quickfix tries to fix it)
        Timer.SingleShot(0.25, () => RedoMoveTo(thisCollides));
        // TODO: Find out why sometimes the unit shoots away. This quickfix tries to fix it)
        Timer.SingleShot(0.25, () => RedoMoveTo(toThat));
    }

    void OnUnitFlee(PhysicsObject unit)
    {
        foreach (var team in teams.Keys)
		{
		    if (teams[team].DeployQueue.Contains(unit))
            {
                teams[team].DeployQueue.Remove(unit);
                ProgressDeployQueue(team);
            }	 
		}

        unit.StopMoveTo();
        unit.IgnoresCollisionResponse = true;
        unit.MoveTo(new Vector(unit.X > 0 ? Screen.Right + 100 : Screen.Left - 100, unit.Y), unitMoveSpeed, ()=>unit.Destroy());
        moveOrStayTarget[unit] = null;
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

        // Deploy the first unit
        PhysicsObject unitToDeploy = teams[team].DeployQueue.First.Value;
        teams[team].DeployQueue.RemoveFirst();

        ProgressDeployQueue(team);

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

        if (unit.Image == hPreparer)
        {
            if (MoveToClosestTree(unit, treeImage)) return;
        }
        if (unit.Image == mGatherer)
        {
            if (MoveToClosestTree(unit, saplingImage)) return;
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
            Action afterMove = () => OnMoveUnit(unit, index + increment, path);
            unit.MoveTo(moveToPos, unitMoveSpeed, afterMove);
            moveOrStayTarget[unit] = new Tuple<Vector,Action>(moveToPos, afterMove);
        }
        else
        {
            // Stop at the end of the path
            unit.MoveTo(moveToPos, unitMoveSpeed);
            moveOrStayTarget[unit] = new Tuple<Vector, Action>(moveToPos, null);
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
