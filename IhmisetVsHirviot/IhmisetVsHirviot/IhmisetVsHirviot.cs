using System;
using System.Collections.Generic;
using System.Linq;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/* TODO:
 * - Make preparer / Gatherer to move to closest resource and back home.
 */

public class IhmisetVsHirviot : PhysicsGame
{
    // Gameplay constants
    double amountPerResource = 20.0;
    double maxGameLenInS = 600.0;
    double trickleRate = 2.0;
    double unitMoveSpeed = 50.0;
    double maxTreeRange = 100.0;
    double hitPerHit = -10.0;


    double MOVE_TO_QUEUE_SPEED = 50.0;
    Color[] BARCOLORS = new Color[] { Color.Cyan, Color.Lime, Color.Red };
    Dictionary<PlayerTeam, PlayerData> teams = new Dictionary<PlayerTeam, PlayerData>();
    List<RoadMap> paths = new List<RoadMap>();
    List<PhysicsObject> trees = new List<PhysicsObject>();

    Image stumpImage = LoadImage("stump");
    Image logImage = LoadImage("log");
    Image treeImage = LoadImage("tree");

    Image hPreparer = LoadImage("metsuri");
    Image hRepeller = LoadImage("reportteri");
    Image hGatherer = LoadImage("moto");

    Image mPreparer = LoadImage("haamu");
    Image mRepeller = LoadImage("hirvio");
    Image mGatherer = LoadImage("karhu");

    static Vector GatherPoint(PlayerTeam team)
    {
        return new Vector(0, team == PlayerTeam.Humans ? Screen.Top - 135 : Screen.Bottom + 135);
    }

    public override void Begin()
    {
        SetWindowSize(1024, 768);
        Level.Background.Color = Color.GreenYellow;

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
   
        CreateMeterFactories(PlayerTeam.Humans);
        CreateMeterFactories(PlayerTeam.Monsters);

        AddPaths();

        Mouse.IsCursorVisible = true;

        // FOR TESTING ONLY REMOVE THESE 
        Timer.SingleShot(RandomGen.NextDouble(0.1, 4.0), () => OnResourceAdded(PlayerTeam.Humans, amountPerResource));
        Timer.SingleShot(RandomGen.NextDouble(0.1, 4.0), () => OnResourceAdded(PlayerTeam.Monsters, amountPerResource));
    }

    void AddPaths()
    {
        double PATH_SEG_LEN_MIN = 50;
        double PATH_SEG_LEN_MAX = 70;
        double TO_SIDE_ANGLE = 60;

        var fromHumansToMonsters = GatherPoint(PlayerTeam.Monsters) - GatherPoint(PlayerTeam.Humans);
        var fromMonstersToHumans = GatherPoint(PlayerTeam.Humans) - GatherPoint(PlayerTeam.Monsters);
        // TODO: Load these from files OR generate progomatically
        List<Vector[]> pathsPts = new List<Vector[]>();

        for (int i = -1; i <= 1; i++)
        {
            pathsPts.Add(
                new Vector[] // first path
                {
                    GatherPoint(PlayerTeam.Humans),
                    GatherPoint(PlayerTeam.Humans) + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN,PATH_SEG_LEN_MAX),
                        fromHumansToMonsters.Angle+Angle.FromDegrees(i*TO_SIDE_ANGLE)),
                    GatherPoint(PlayerTeam.Humans) + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*2,PATH_SEG_LEN_MAX*2),
                        fromHumansToMonsters.Angle+RandomGen.NextAngle(Angle.FromDegrees(i*TO_SIDE_ANGLE-10), Angle.FromDegrees(i*TO_SIDE_ANGLE+10)) ),
                    GatherPoint(PlayerTeam.Humans) + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*3,PATH_SEG_LEN_MAX*3),
                        fromHumansToMonsters.Angle+RandomGen.NextAngle(Angle.FromDegrees(i*TO_SIDE_ANGLE-10), Angle.FromDegrees(i*TO_SIDE_ANGLE+10)) ), // midp
                    GatherPoint(PlayerTeam.Monsters) + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN*2,PATH_SEG_LEN_MAX*2),
                        fromMonstersToHumans.Angle+RandomGen.NextAngle(Angle.FromDegrees(-i*TO_SIDE_ANGLE-10), Angle.FromDegrees(-i*TO_SIDE_ANGLE+10)) ),
                    GatherPoint(PlayerTeam.Monsters) + Vector.FromLengthAndAngle( RandomGen.NextDouble(PATH_SEG_LEN_MIN,PATH_SEG_LEN_MAX),
                        fromMonstersToHumans.Angle+Angle.FromDegrees(-i*TO_SIDE_ANGLE)),
                    GatherPoint(PlayerTeam.Monsters)+new Vector(0.01,0.01),
                    GatherPoint(PlayerTeam.Monsters),
                }
            );
        }

        foreach (var pathPoints in pathsPts)
        {
            RoadMap path = new RoadMap(pathPoints);
            path.DefaultWidth = 10.0;
            path.Insert();

            paths.Add(path);

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
            Vector position = point + RandomGen.NextVector(40, maxTreeRange);

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
        }
    }

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

            progressMeter.AddOverTime(maxGameLenInS*trickleRate, maxGameLenInS);

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
                GatherPoint(team)
                ));
        }

        switch (team)
        {
            case PlayerTeam.Humans:
                AddButtons(team, GatherPoint(team), 3);
                break;
            case PlayerTeam.Monsters:
                AddButtons(team, GatherPoint(team), 3);
                break;
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
        Vector moveToPos = gatherPoint + new Vector(40 * teams[team].DeployQueue.Count, 0);
        unit.MoveTo(moveToPos, MOVE_TO_QUEUE_SPEED);
        if (teams[team].DeployQueue.Count == 0)
        {
            double untilNext = DistanceAndSpeedToTime(moveToPos.Magnitude, MOVE_TO_QUEUE_SPEED);
            Timer.SingleShot(untilNext, () => OnCanDeploy(team));
        }
        teams[team].DeployQueue.AddLast(unit);

        AddCollisionHandler(unit, enemyTag, OnEnemiesCollide);
    }

    void OnEnemiesCollide(PhysicsObject thisCollides, PhysicsObject toThat)
    {
        if (thisCollides.Tag == "h" && thisCollides.Tag == "m")
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

        // Lock into deadly? combat 
        
        thisCollides.MoveTo(toThat.Position, unitMoveSpeed);
        toThat.MoveTo(thisCollides.Position, unitMoveSpeed);

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
        unit.MoveTo(new Vector(unit.X > 0 ? Screen.Right + 100 : Screen.Left - 100, unit.Y), MOVE_TO_QUEUE_SPEED);
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
            for (int i = 1; i < teams[team].DeployQueue.Count; i++)
            {
                moveToPos = GatherPoint(team) + new Vector(40 * (i-1), 0);
                var moveUnit = teams[team].DeployQueue.ElementAt(i);
                moveUnit.MoveTo(moveToPos, MOVE_TO_QUEUE_SPEED);
            }

            // Allow deploy when the queue has stopped moving
            double untilNext = DistanceAndSpeedToTime(moveToPos.Magnitude, MOVE_TO_QUEUE_SPEED);
            Timer.SingleShot(untilNext, () => OnCanDeploy(team));
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
 
        Vector moveToPos = path.Segments[index].Position;
        unit.MoveTo(moveToPos, unitMoveSpeed);

        // determine in which turn to traverse the path
        int increment = +1;
        if (unit.Tag == "m")
            increment = -1;

        // TODO: Determine if to gather resources

        // Stop at the end of the path
        if (index + increment >= 0 && index + increment < path.Segments.Length)
        {
            double untilNext = DistanceAndSpeedToTime(moveToPos.Magnitude, unitMoveSpeed);
            Timer.SingleShot(untilNext, () => OnMoveUnit(unit, index + increment, path));
        }
    }

    static double DistanceAndSpeedToTime(double distance, double speed)
    {
        return 1.0+distance / (speed*5);
    }

    void OnResourceAdded(PlayerTeam team, double amount)
    {
        foreach (var kvp in teams[team].UnitCreationProgress)
        {
            double portion = amount * (teams[team].UnitCreationAllocation[kvp.Key].Value / 100.0);
            kvp.Value.AddValue(portion);
        }

        // FOR TESTING ONLY REMOVE THESE 
        Timer.SingleShot(RandomGen.NextDouble(0.1, 4.0), () => OnResourceAdded(team, amountPerResource));
    }

}
