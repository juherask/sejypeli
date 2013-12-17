using System;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


public class CatWithOnly1Life : PhysicsGame
{

    // Game related constants
    double JUMP_POWAH_INITIAL = 500.0;
    double JUMP_POWAH = 1000.0;
    double JUMP_POWAH_MAX = 1000.0;
    double RUN_SPEED_INITIAL = 100.0;
    double RUN_SPEED_MAX = 500.0;
    double RUN_ACCELERATION = 500.0;
    double AIR_FRICTION = 0.99;

    // How easy it is to dangle to the roof
    double HANG_TOLERANCE = 15.0;

    

    // Graphics related constants
    int BUILDING_HEIGHT = 1000;
    int RUN_ANIM_MIN = 7; //FPS
    int RUN_ANIM_SPEED = 40; // if RUN_SPEED_MAX -> FPS ~ 12
    int LEVEL_LENGTH = 5; // buildings
    int BUILDING_AVG_LENGTH = 800; // buildings
    int BUILDING_DROP = 300; // buildings

    Image tileRoofSkin = LoadImage("tileroof");
    Image leftRoofSkin = LoadImage("tileroof_endl");
    Image rightRoofSkin = LoadImage("tileroof_endr");
    Image[] roofFeatures = LoadImages("feature_chimney", "feature_santa", "feature_antenna");

    Image catHitBox = LoadImage("cat_hitbox");
    Image catStandSkin = LoadImage("cat");
    Image catSitSkin = LoadImage("sitcat");
    Image catFlySkin = LoadImage("flycat");
    Image catHangsSkin = LoadImage("climbcat1");
    Image[] catClimbSkins = LoadImages("climbcat1", "climbcat2", "climbcat3", "climbcat4", "climbcat5", "climbcat6", "climbcat7");
    Image[] catRunSkins = LoadImages("runcat1", "runcat2", "runcat3", "runcat4", "runcat5", "runcat6");

    

    DoubleMeter jumpPowerMeter;
    DoubleMeter speedMeter;
    IntMeter catsSavedMeter;
    Label guideLabel;

    ClimbingPlatformCharacter player;
    GameObject flag;
    double jumpTick = 0.0;
    double walkTick = 0.0;

    public override void Begin()
    {
        // 1. Game style and size
        SetWindowSize(1024, 768);
        SmoothTextures = false; // false = use NN? scaling
        Mouse.IsCursorVisible = true;

        // 2. Level, OSD and others
        Gravity = new Vector(0, -1000);
        SetupLevel();
        CreateOSD();
        

        // 3. Cats
        player = CreateCat(Color.FromHexCode("DD7777"));
        // Other competitors
        for (int i = 0; i < 3 ; i++)
        {
            Timer.SingleShot(RandomGen.NextDouble(0.3, 3.0), AddNewRunnerCat);
        }
        Camera.Follow(player);
        
        // 4. Input devices
        Mouse.Listen(MouseButton.Left, ButtonState.Released, MouseClicked, "Jump a cat");
        Keyboard.Listen(Key.P, ButtonState.Pressed, Pause, "Pause/Unpause the game");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Exit game");
        Keyboard.Listen(Key.Right, ButtonState.Down, RunRight, "Walk/Run to right");
        Keyboard.Listen(Key.Left, ButtonState.Down, RunLeft, "Walk/Run to left");
        Keyboard.Listen(Key.Right, ButtonState.Released, Stopping, "Stop Walk/Run to right");
        Keyboard.Listen(Key.Left, ButtonState.Released, Stopping, "Stop Walk/Run to left");
        Keyboard.Listen(Key.Up, ButtonState.Pressed, PrepareToJump, "Prepare to jump! (longer you hold, bigger the jump)");
        Keyboard.Listen(Key.Up, ButtonState.Down, UpdateJumpMeter, "");
        Keyboard.Listen(Key.Up, ButtonState.Released, Jump, "Jump!");
        Keyboard.Listen(Key.Down, ButtonState.Pressed, Sit, "Sit down");

        // 5. Give directions
        guideLabel = new Label("Rescue your fellow cats\nby herding them\n(clicking them makes a cat jump).");
        guideLabel.Font = Font.DefaultLargeBold;
        guideLabel.TextColor = Color.White;
        Add(guideLabel);
        Timer.SingleShot(3, OnHideGuide);
    }

    void OnHideGuide()
    {
        guideLabel.Destroy();
    }

    #region InputHandling
    void MouseClicked()
    {
        Vector mousePos = Mouse.PositionOnWorld;
        
        //MessageDisplay.Add("mouse:"+mousePos.ToString());
        foreach (var item in GetObjectsWithTag("cat"))
        {
            // The shape loaded from image prevents smart use of this
            //if (item.Position IsInside(mousePos))
            if (mousePos.X >= item.Position.X-item.Width/2 &&
                mousePos.X <= item.Position.X+item.Width/2 &&
                mousePos.Y >= item.Position.Y-item.Height/2 &&
                mousePos.Y <= item.Position.Y+item.Height/2)
            {
                ClimbingPlatformCharacter isACat = item as ClimbingPlatformCharacter;
                isACat.Jump(JUMP_POWAH_MAX);
            }
        }
    }

    void Stopping()
    {
        walkTick = 0.0;
        speedMeter.Value = 0.0;
    }
    void Run(Direction dir)
    {
        if (walkTick == 0.0)
            walkTick = Time.SinceStartOfGame.TotalSeconds;
        double nowTick = Time.SinceStartOfGame.TotalSeconds;
        speedMeter.Value = Math.Min(RUN_SPEED_MAX, RUN_SPEED_INITIAL + RUN_ACCELERATION * (nowTick - (double)walkTick));

        int dirMult = (dir == Direction.Left) ? -1 : 1;
        player.Walk(dirMult * speedMeter.Value);
        player.AnimWalk.FPS = Math.Max(RUN_ANIM_MIN, (int)(speedMeter.Value / RUN_ANIM_SPEED));
        //MessageDisplay.Add(player.AnimWalk.FPS.ToString());
        
    }
    void RunRight()
    {
        Run(Direction.Right);
    }
    void RunLeft()
    {
        Run(Direction.Left);
    }

    void PrepareToJump()
    {
        if (player.Hangs)
            player.Climb(player.Width/2, player.Height, 8.0);
        else
            jumpTick = Time.SinceStartOfGame.TotalSeconds;
    }
    void UpdateJumpMeter()
    {
        double nowTick = Time.SinceStartOfGame.TotalSeconds;
        jumpPowerMeter.Value = Math.Min(JUMP_POWAH_MAX, JUMP_POWAH_INITIAL + (nowTick - (double)jumpTick) * JUMP_POWAH);
    }
    void Jump()
    {
        player.Jump(jumpPowerMeter.Value);
        jumpPowerMeter.Value = 0.0;
    }
    void Sit()
    {
        player.Hangs = false;
        player.Stop();
        // TODO: Change sprite to sit
    }
    #endregion

    void CreateOSD()
    {
        jumpPowerMeter = new DoubleMeter(0.0, 0.0, JUMP_POWAH_MAX);
        speedMeter = new DoubleMeter(0.0, 0.0, RUN_SPEED_MAX);
        catsSavedMeter = new IntMeter(0);

        ProgressBar jumpPB = new ProgressBar(200, 20);
        jumpPB.Position = new Vector(Screen.Left + 150, Screen.Top - 40);
        jumpPB.BindTo(jumpPowerMeter);
        jumpPB.BarColor = Color.Red;
        jumpPB.Color = Color.White;
        Add(jumpPB);

        ProgressBar speedPB = new ProgressBar(200, 20);
        speedPB.Position = new Vector(Screen.Right - 150, Screen.Top - 40);
        speedPB.BindTo(speedMeter);
        speedPB.BarColor = Color.Blue;
        speedPB.Color = Color.White;
        Add(speedPB);

        Label lives = new Label("Lives left: 1 of 1");
        lives.Y = Screen.Top - 40;
        lives.Font = Font.DefaultBold;
        lives.TextColor = Color.White;
        Add(lives);

        Label points = new Label("");
        points.IntFormatString = "Cats rescued: {0:D1}";
        points.Y = Screen.Top - 80;
        points.Font = Font.DefaultBold;
        points.TextColor = Color.White;
        points.BindTo(catsSavedMeter);
        Add(points);
    }

    void AddNewRunnerCat()
    {
        PlatformCharacter runner = CreateCat(RandomGen.NextColor());
        runner.X = RandomGen.NextInt(-500, 500);
        FollowerBrain pwb = new FollowerBrain(flag);
        //pwb.TriesToJump = false;
        //pwb.FallsOffPlatforms = false;
        //pwb.JumpSpeed = JUMP_POWAH_INITIAL;
        pwb.Speed = RUN_SPEED_MAX;
        runner.Brain = pwb;
        // TODO: Create better brain (try follower brain to flag)
    }

    ClimbingPlatformCharacter CreateCat(Color catColor)
    {
        Converter<Color, Color> tiltTo = pixelColor =>
        {
            Color col = pixelColor;
            if (pixelColor.AlphaComponent > 128 && pixelColor != Color.Black && pixelColor != Color.White)
            {
                col = new Color(
                    (pixelColor.RedComponent + catColor.RedComponent) / 2,
                    (pixelColor.GreenComponent + catColor.GreenComponent) / 2,
                    (pixelColor.BlueComponent + catColor.BlueComponent) / 2);
            }
            return col;
        };
        Image coloredStandCatSkin = catStandSkin.Clone();
        coloredStandCatSkin.ApplyPixelOperation(tiltTo);
        Image coloredSitCatSkin = catSitSkin.Clone();
        coloredSitCatSkin.ApplyPixelOperation(tiltTo);
        Image coloredFlyCatSkin = catFlySkin.Clone();
        coloredFlyCatSkin.ApplyPixelOperation(tiltTo);
        Image coloredCatHangsSkin = catHangsSkin.Clone();
        coloredCatHangsSkin.ApplyPixelOperation(tiltTo);

        Image[] coloredCatRunSkins = new Image[catRunSkins.Length];
        for (int i = 0; i < catRunSkins.Length; i++)
        {
            coloredCatRunSkins[i] = catRunSkins[i].Clone();
            coloredCatRunSkins[i].ApplyPixelOperation(tiltTo);  
        }

        Image[] coloredCatClimbSkins = new Image[catClimbSkins.Length];
        for (int i = 0; i < catClimbSkins.Length; i++)
        {
            coloredCatClimbSkins[i] = catClimbSkins[i].Clone();
            coloredCatClimbSkins[i].ApplyPixelOperation(tiltTo);
        }

        ClimbingPlatformCharacter cat = new ClimbingPlatformCharacter(79, 79);
        cat.Image = coloredStandCatSkin;
        cat.AnimIdle = coloredSitCatSkin;
        cat.AnimWalk = new Animation(coloredCatRunSkins);
        cat.AnimJump = coloredFlyCatSkin;
        cat.AnimHangs = coloredCatHangsSkin;
        cat.AnimClimb = new Animation(coloredCatClimbSkins);
        cat.Shape = Shape.FromImage(catHitBox);

        cat.Tag = "cat";

        //cat.Image = catSkin;
        //cat.CanMoveOnAir = false;
        cat.LinearDamping = AIR_FRICTION;
        Add(cat);

        AddCollisionHandler(cat, "roof", OnCatCollidesRoof);
        AddCollisionHandler(cat, "flag", OnCatCollidesFlag); 

        return cat;
    }

    void OnCatCollidesFlag(PhysicsObject cat, PhysicsObject roof)
    {
        cat.Destroy();
        catsSavedMeter.AddValue(1);


        if (cat == player)
        {
            Label endGameLabel = new Label("You rescued at least yourself. Game Over!");
            endGameLabel.Font = Font.DefaultLargeBold;
            endGameLabel.TextColor = Color.White;
            Add(endGameLabel);
            Timer.SingleShot(5, OnRestartGame);
        }
    }

    void OnRestartGame()
    {
        ClearAll();
        Begin();
    }

    void OnCatCollidesRoof(PhysicsObject cat, PhysicsObject roof)
    {
        ClimbingPlatformCharacter clingingCat = cat as ClimbingPlatformCharacter;
        bool CatAtTheEndOfRoof = cat.X + cat.Width/2 - HANG_TOLERANCE <= roof.X - roof.Width/2 ||
                                 cat.X - cat.Width/2 + HANG_TOLERANCE >= roof.X + roof.Width/2;
        bool CatNearRoofTopLevel = cat.Y+cat.Height/2 - HANG_TOLERANCE >= roof.Y - roof.Height/2 && // not under
                                   cat.Y-cat.Height/2 - HANG_TOLERANCE <= roof.Y + roof.Height/2; // not over
        if (CatAtTheEndOfRoof && CatNearRoofTopLevel)
            clingingCat.Hangs = true;
        else
            clingingCat.Hangs = false;
    }

    void SetupLevel()
    {
        Image skyGradient = Image.FromGradient(
            Math.Min(2048, (int)(Screen.Width * 1.5)),
            Math.Min(2048, (int)(Screen.Height * 1.5)),
             Color.Darker(Color.DarkCyan, 50), Color.Darker(Color.DarkBlue, 100));

        GameObject backgrounSky = new GameObject(Screen.Width, Screen.Height);
        backgrounSky.Image = skyGradient;
        Add(backgrounSky, -3);
        Layers[-3].RelativeTransition = new Vector(0.01, 0.01);

        PhysicsObject prevRoof = Level.CreateBottomBorder();
        prevRoof.Tag = "roof";

        SkinRoof(prevRoof);
        for (int i = 0; i < LEVEL_LENGTH; i++)
        {
            double roofLength = Math.Max(100, NextRandomNormallyDistributedDouble(BUILDING_AVG_LENGTH, BUILDING_AVG_LENGTH / 3));
            PhysicsObject newRoof = PhysicsObject.CreateStaticObject(roofLength, prevRoof.Height);
            newRoof.X = prevRoof.X + prevRoof.Width / 2 + newRoof.Width / 2 + RandomGen.NextDouble(-prevRoof.Width / 8, prevRoof.Width / 8);
            newRoof.Y = prevRoof.Y + RandomGen.NextDouble(-BUILDING_DROP, BUILDING_DROP);
            newRoof.Color = Color.Darker(Color.DarkRed, 50);
            Add(newRoof);
            prevRoof = newRoof;
            prevRoof.Tag = "roof";
            SkinRoof(prevRoof);

            /*PhysicsObject cheatHangsPlatform = new PhysicsObject(roofLength+40, prevRoof.Height);
            cheatHangsPlatform.Y-=prevRoof.Height;
            prevRoof.Add(cheatHangsPlatform);*/
        }

        //Whoever touches this, wins
        flag = PhysicsObject.CreateStaticObject(50, 50);
        flag.Color = Color.DarkRed;
        flag.X = prevRoof.X;
        flag.Y = prevRoof.Y+80;
        flag.Tag = "flag";
        Add(flag);
    }

    void SkinRoof(PhysicsObject roof)
    {
        int reqHorizTiles = Math.Max(0, (int)(roof.Width / tileRoofSkin.Width)-2);
        Image tiledRoofSkin = leftRoofSkin.Clone();
        for (int i = 0; i < reqHorizTiles; i++)
        {
            tiledRoofSkin = Image.TileHorizontal(tiledRoofSkin, tileRoofSkin);
            if (RandomGen.NextDouble(0, 1.0) < 0.2)
            {
                GameObject feature = new GameObject(100, 200);
                feature.Image = roofFeatures[RandomGen.NextInt(roofFeatures.Length)];
                feature.Position = new Vector(
                    (roof.X - roof.Width / 2) + (i * tileRoofSkin.Width + tileRoofSkin.Width / 2),
                    (roof.Y + roof.Height / 4) + feature.Height / 2);
                Add(feature, -1);
            }
        }
        tiledRoofSkin = Image.TileHorizontal(tiledRoofSkin, rightRoofSkin);
        roof.Image = tiledRoofSkin;

        
        GameObject building = new GameObject(roof.Width - 40, BUILDING_HEIGHT);
        building.Color = Color.Black;
        building.X = roof.X;
        building.Y = roof.Y - BUILDING_HEIGHT / 2;
        Add(building, -1);
    }

#region UTILS
    static double NextRandomNormallyDistributedDouble(double mean, double sd)
    {
        double u1 = RandomGen.NextDouble(0, 1.0);
        double u2 = RandomGen.NextDouble(0, 1.0);
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        return mean + sd * randStdNormal; //random normal(mean,stdDev^2) 
    }
#endregion
    
}
