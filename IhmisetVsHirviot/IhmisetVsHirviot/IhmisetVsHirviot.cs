using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

public class IhmisetVsHirviot : Game
{
    double MOVE_TO_QUEUE_SPEED = 50.0;
    Color[] BARCOLORS = new Color[] { Color.Cyan, Color.Lime, Color.Red };
    Dictionary<PlayerTeam, PlayerData> teams = new Dictionary<PlayerTeam, PlayerData>();
    

    public override void Begin()
    {
        SetWindowSize(800, 600);
        // Kirjoita ohjelmakoodisi tähän

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
   
        CreateMeterFactories(PlayerTeam.Humans);
        CreateMeterFactories(PlayerTeam.Monsters);

        // FOR TESTING ONLY REMOVE THESE 
        Timer.SingleShot(RandomGen.NextDouble(0.1, 4.0), () => ResourceAdded( PlayerTeam.Humans ) );
        Timer.SingleShot(RandomGen.NextDouble(0.1, 4.0), () => ResourceAdded( PlayerTeam.Monsters ) );
    }

    void CreateMeterFactories(PlayerTeam team)
    {
        Vector gatherPoint = new Vector(0, team==PlayerTeam.Humans ? Screen.Top - 175 : Screen.Bottom + 175);

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
            teams[team].UnitCreationTargets.Add(unit, targetMeter);
            
            ProgressBar targetBar = new ProgressBar(100, 30, targetMeter);
            targetBar.Angle = Angle.FromDegrees(90);
            targetBar.X = (i - 1) * 50;
            targetBar.Y = team == PlayerTeam.Humans ? Screen.Top - 75 : Screen.Bottom + 75;
            targetBar.BorderColor = Color.Black;

            targetBar.Color = Color.Darker(barBaseColor, 100);
            targetBar.BarColor = Color.Darker(barBaseColor, 50);
            targetBar.BorderColor = Color.Black;
            Add(targetBar, -1);


            // Progress to target (when a new unit is produced)
            DoubleMeter progressMeter = new DoubleMeter(0);
            progressMeter.MaxValue = 100.0; //%
            progressMeter.Value = 1.0; //%
            teams[team].UnitCreationProgress.Add(unit, progressMeter);

            progressMeter.AddOverTime(600, 600);

            ProgressBar progressBar = new ProgressBar(100, 30, progressMeter);
            progressBar.Angle = targetBar.Angle;
            progressBar.Position = targetBar.Position;
            progressBar.BorderColor = Color.Black;
            progressBar.BarColor = barBaseColor;
            Add(progressBar, 1);

            progressMeter.AddTrigger(33.3, TriggerDirection.Up, () => CreateNewUnit(
                team, unit,
                targetBar.Position + new Vector(0, team == PlayerTeam.Humans ? -75 : 75),
                gatherPoint
                ));
        }
    }

    void CreateNewUnit(PlayerTeam team, UnitType type, Vector spawnPoint, Vector gatherPoint)
    {
        GameObject unit = new GameObject(20, 20, Shape.Circle);
        unit.Position = spawnPoint;
        Add(unit);

        Vector moveToPos = gatherPoint + new Vector(40 * teams[team].DeployQueue.Count, 0);
        unit.MoveTo(moveToPos, MOVE_TO_QUEUE_SPEED);

        if (teams[team].DeployQueue.Count==0)
            Timer.SingleShot( moveToPos.Magnitude / MOVE_TO_QUEUE_SPEED, () => CanDeploy(team) );

        teams[team].DeployQueue.AddLast(unit);

        teams[team].UnitCreationProgress[type].Value -= teams[team].UnitCreationTargets[type].Value;

        // TODO: Create a new unit to specified position
    }

    void CanDeploy(PlayerTeam team)
    {
        //if (teams[team].DeployQueue.Count > 0)
    }

    void Deploy(PlayerTeam team)
    {
    }

    void ResourceAdded(PlayerTeam team)
    {
        foreach (var progress in teams[team].UnitCreationProgress.Values)
        {
            progress.AddValue(3.33);
        }

        // FOR TESTING ONLY REMOVE THESE 
        Timer.SingleShot(RandomGen.NextDouble(0.1, 4.0), () => ResourceAdded(team));
    }

}
