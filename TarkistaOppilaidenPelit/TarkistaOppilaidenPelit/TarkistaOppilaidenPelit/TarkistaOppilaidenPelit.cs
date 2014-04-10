using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

// Oppilas, PelinNimi, Repo, Lista checkoutattavista tiedostoista/kansioista, Solutiontiedosto
class GameRecord 
{ 
  public string Author;
  public string GameName;
  public string SVNRepo;
  public List<string> ToFetch;
  public string Solution;
}

class User32
{
    [DllImport("user32.dll")]
    public static extern void SetWindowPos(uint Hwnd, int Level, int X, int Y, int W, int H, uint Flags);
}

/*
* For checking out we use
* > svn checkout <repo> <author_folder> --depth empty
* > cd <author_folder>/trunk
* > svn up <files/folders_you_want>
*/
public class TarkistaOppilaidenPelit : Game
{
    static int HWND_TOP = 0;
    static int HWND_TOPMOST = -1;

    string SVN_CLI_EXE = @"C:\Users\opetus01\Downloads\svn-win32-1.8.8\svn-win32-1.8.8\bin\svn.exe";
    string MSBUILD_EXE = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe";
    //double TASK_COMPLETION_POLL_INTERVAL = 2.0;
    double PROCESS_CHECK_INTERVAL = 1.0;
    int MAX_GAME_RUN_TIME = 3000;

    Queue<Tuple<GameRecord, Task>> taskQueue;
    List<GameRecord> listOfGames;
    Process activeCliProcess;
    bool processing = false;
    bool paused = false;
    bool topmost = true;

    Mutex stateQueueMutex = new Mutex();
    Queue<string> messageQueue = new Queue<string>();
    Queue<Tuple<GameRecord, Task, Status>> stateQueue = new Queue<Tuple<GameRecord, Task, Status>>();


    Thread processingThread;

    enum Task
    {
        Checkout,
        UpdateListed,
        Compile,
        RunGame,
        None
    }

    enum Status
    {
        Wait,
        OK,
        Fail,
        NA
    }

    Dictionary<Task, string> taskToLabel = new Dictionary<Task, string>()
    {
        {Task.Checkout, "Nouto"},
        {Task.UpdateListed, "Update"},
        {Task.Compile, "Kääntäminen"},
        {Task.RunGame, "Pelin ajo"},
    };
   
    

    public override void Begin()
    {
        //SetWindowSize(800, 600);
        SetWindowTopmost(topmost);
        
        IsMouseVisible = true;

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.P, ButtonState.Pressed, PauseProcess, "Pistä tauolle");
        Keyboard.Listen(Key.T, ButtonState.Pressed, ToggleTopmost, "Pistä tauolle");

        // TODO: There should be other ways to get this, a plain text file for example?
        //  but for now hard coded list will do.
        listOfGames = GetHardCodedList();

        // Simple validation
        List<string> authors = listOfGames.Select(ot => ot.Author).Distinct().ToList();
        if (listOfGames.Count != authors.Count)
            throw new ArgumentException("Tekijöiden pitää olla yksilöllisiä");

        // Create indicators
        taskQueue = new Queue<Tuple<GameRecord, Task>>();
        for (int i = 0; i < listOfGames.Count; i++)
        {
            var record = listOfGames[i];

            var indicator = new GameObject(40, 40, Shape.Circle);
            indicator.Color = Color.Gray;
            indicator.Tag = record.Author + "_indicator";
            indicator.X = Screen.Left+100 + ((Screen.Right-Screen.Left-100)/listOfGames.Count)*i;
            Add(indicator);

            var nameLabel = new Label(120, 40);
            //nameLabel.Font = Font.DefaultLargeBold;
            nameLabel.Text = record.Author;
            nameLabel.Position = new Vector(indicator.X, 70);
            Add(nameLabel);

            var statusLabel = new Label(120, 40);
            //nameLabel.Font = Font.DefaultLargeBold;
            statusLabel.Tag = record.Author + "_status";
            statusLabel.Text = "odota";
            statusLabel.Position = new Vector(indicator.X, -70);
            Add(statusLabel);

            // Always 1st try (or verify) that the files have been checked out
            taskQueue.Enqueue( new Tuple<GameRecord, Task>(record, Task.Checkout) );
        }

        ThreadedTaskListProcessor();
    }

    #region KeypressHandlers
    void PauseProcess()
    {
        paused = !paused;
    }

    void ToggleTopmost()
    {
        topmost = !topmost;
        SetWindowTopmost(topmost);
    }
    #endregion

    void SetWindowTopmost(bool topmost)
    {
        int screenHt = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        int screenWt = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        if (topmost)
        {
            User32.SetWindowPos((uint)this.Window.Handle, HWND_TOPMOST, 0, 0, screenWt, screenHt, 0);
        }
        else
        {
            User32.SetWindowPos((uint)this.Window.Handle, HWND_TOP, 0, 0, screenWt, screenHt, 0);
        }
    }

    /// <summary>
    /// Update processes asynchronous messaging and state changes from the worker (processing) thread.
    /// </summary>
    /// <param name="time"></param>
    protected override void Update(Time time)
    {
        base.Update(time);
        var hasOne = stateQueueMutex.WaitOne(10);
        if (hasOne)
        {
            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                MessageDisplay.Add(message);
            }

            while (stateQueue.Count > 0)
            {
                var stateChange = stateQueue.Dequeue();
                var author = stateChange.Item1.Author;

                var indicator = GetObjectsWithTag(author + "_indicator").First();
                var label = GetObjectsWithTag(author + "_status").First() as Label;
                label.Text = taskToLabel[stateChange.Item2];

                switch (stateChange.Item3)
                {
                    case Status.Wait:
                        indicator.Color = Color.Gray;        
                        break;
                    case Status.OK:
                        if (stateChange.Item2==Task.RunGame)
                            indicator.Color = Color.Green;
                        else
                            indicator.Color = Color.Yellow;
                        break;
                    case Status.Fail:
                        indicator.Color = Color.Red;
                        break;
                    default:
                        break;
                }
            }
            stateQueueMutex.ReleaseMutex();
        }
    }

    #region TaskProcessing
    private void ThreadedTaskListProcessor()
    {
        processing = false;
        processingThread = new Thread(new ThreadStart(ProcessTaskList));

        // Start the thread
        processingThread.Start();

        // Spin for a while waiting for the started thread to become
        // alive:
        while (!processingThread.IsAlive) ;
    }

    void ProcessTaskList()
    {
        while (true)
        {
            if (!processing)
                System.Threading.Thread.Sleep( (int)(PROCESS_CHECK_INTERVAL*1000) );

            while (paused)
                System.Threading.Thread.Sleep((int)(PROCESS_CHECK_INTERVAL * 1000));

            if (taskQueue.Count == 0)
                break;

            var task = taskQueue.Dequeue();
            switch (task.Item2)
            {   
                case Task.Checkout:
                    stateQueueMutex.WaitOne();
                    messageQueue.Enqueue("Checking out for " + task.Item1.Author);
                    stateQueueMutex.ReleaseMutex();
                    ProcessCheckoutRepo(task.Item1);
                    break;
                case Task.UpdateListed:
                    stateQueueMutex.WaitOne();
                    messageQueue.Enqueue("Updating files from " + task.Item1.Author);
                    stateQueueMutex.ReleaseMutex();
                    ProcessUpdateListed(task.Item1);
                    break;
                case Task.Compile:
                    stateQueueMutex.WaitOne();
                    messageQueue.Enqueue("Compiling project of " + task.Item1.Author);
                    stateQueueMutex.ReleaseMutex();
                    ProcessCompile(task.Item1);
                    break;
                case Task.RunGame:
                    stateQueueMutex.WaitOne();
                    messageQueue.Enqueue("Running game of " + task.Item1.Author);
                    stateQueueMutex.ReleaseMutex();
                    ProcessRunGame(task.Item1);
                    break;
                default:
                    break;
            }
            processing = false;
        }
    }

    void ProcessCheckoutRepo(GameRecord record)
    {
        // Directory existance implies existing checkout
        if (Directory.Exists(record.Author))
        {
            taskQueue.Enqueue(new Tuple<GameRecord, Task>(record, Task.UpdateListed));
        }
        else
        {
            Directory.CreateDirectory(record.Author);
            Task currentTask = Task.Checkout;
            Task nextTask = Task.UpdateListed;
            string command = String.Format("\"{0}\" co {1} \"{2}\" --depth empty", SVN_CLI_EXE, record.SVNRepo, Path.Combine(Directory.GetCurrentDirectory(), record.Author));

            GenericProcessor(record, currentTask, nextTask, command);
        }
    }

    /// <summary>
    /// Update the files/folders in the record from svn. A new batch of tasks to update each is added to the queue.
    /// Fail: Does not update for some reason.
    /// OK: Updated w/o problems.
    /// After: Task.Compile
    /// </summary>
    void ProcessUpdateListed(GameRecord record)
    {
        Task currentTask = Task.UpdateListed;
            
        foreach (var toUpdate in record.ToFetch)
        {
            Task nextTask = Task.None;
            bool addRetry = false;
            // The success of the last update will determine if to try again or if to try and compile.
            if (toUpdate == record.ToFetch.Last())
            {
                nextTask = Task.Compile;
                addRetry = true;    
            }
            string command = String.Format("\"{0}\" up \"{1}\"", SVN_CLI_EXE, Path.Combine(Directory.GetCurrentDirectory(), record.Author, toUpdate));
            if GenericProcessor(record, currentTask, nextTask, command, -1, addRetry);
        }
    }

    /// <summary>
    /// Compile the .sln with msbuild.
    /// Fail: Does not compile for some reason.
    /// OK: Game compiles without error.
    /// After: Task.UpdateListed
    /// </summary>
    void ProcessCompile(GameRecord record)
    {
        Task currentTask = Task.Compile;
        Task nextTask = Task.RunGame;
        string command = String.Format("\"{0}\" /nologo /noconlog \"{1}\"", MSBUILD_EXE, Path.Combine(Directory.GetCurrentDirectory(), record.Author, record.Solution));
        GenericProcessor(record, currentTask, nextTask, command);
    }

    /// <summary>
    /// Run game for the duration of MAX_GAME_RUN_TIME and of no problems arise, kill it. 
    /// Fail: game does not start or crashes
    /// OK: Game runs fone for MAX_GAME_RUN_TIME 
    /// After: Task.UpdateListed
    /// </summary>
    void ProcessRunGame(GameRecord record)
    {
        string gameExeName = "";
        foreach (string file in Directory.EnumerateFiles(
            record.Author, "*.exe", SearchOption.AllDirectories))
        {
            if (gameExeName == "")
            {
                gameExeName = file;
            }
            else
            {
                stateQueueMutex.WaitOne();
                messageQueue.Enqueue("Multiple game exes for the game, using " + gameExeName);
                stateQueueMutex.ReleaseMutex();
                break;
            }
        }
        if (gameExeName == "")
        {
            // No game to run. Skip to update.
            taskQueue.Enqueue(new Tuple<GameRecord, Task>(record, Task.UpdateListed));
        }
        else
        {
            Task currentTask = Task.RunGame;
            Task nextTask = Task.UpdateListed;
            string command = String.Format("\"{0}\"", gameExeName);
            GenericProcessor(record, currentTask, nextTask, command, MAX_GAME_RUN_TIME);
        }
    }



    private Status GenericProcessor(GameRecord record, Task currentTask, Task nextTask, string command, int runTimeout = -1, bool addRetry = true)
    {
        Status returnStatus = Status.NA;
        if (activeCliProcess == null)
        {
            stateQueueMutex.WaitOne();
            stateQueue.Enqueue(new Tuple<GameRecord, Task, Status>(record, currentTask, Status.Wait));
            stateQueueMutex.ReleaseMutex();

            // split
            string exepart = command.Substring(0, command.IndexOf(".exe\"")+5);
            string argpart = command.Substring(command.IndexOf(".exe\"")+5);

            activeCliProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            startInfo.FileName = exepart;
            startInfo.Arguments = argpart;

            /*
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C "+command;
            */

            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;

            activeCliProcess.StartInfo = startInfo;
            activeCliProcess.Start();

            //messageQueueMutex.WaitOne();
            //messageQueue.Enqueue("Running command " + command);
            //messageQueueMutex.ReleaseMutex();

            if (runTimeout == -1)
                activeCliProcess.WaitForExit();
            else
            {
                activeCliProcess.WaitForExit(runTimeout);
            }
        }
        if (activeCliProcess.HasExited)
        {

            // THIS is probably CMD.exe exitcode
            if (activeCliProcess.ExitCode == 0)
            {
                stateQueueMutex.WaitOne();
                messageQueue.Enqueue("Process exited with CODE 0. Output:");

                StreamReader sr = activeCliProcess.StandardOutput;
                while (!sr.EndOfStream)
                {
                    String s = sr.ReadLine();
                    if (s != "")
                    {
                        Trace.WriteLine(s);
                        //messageQueue.Enqueue(s);
                    }
                }

                stateQueue.Enqueue(new Tuple<GameRecord, Task, Status>(record, currentTask, Status.OK));
                returnStatus = Status.OK;

                stateQueueMutex.ReleaseMutex();


                // TODO: Check if it was successfull, //  update light bulb state and 
                if (nextTask != Task.None)
                    taskQueue.Enqueue(new Tuple<GameRecord, Task>(record, nextTask));
            }
            else
            {
                stateQueueMutex.WaitOne();
                messageQueue.Enqueue("Process exited with CODE 1. Output:");

                
                StreamReader sro = activeCliProcess.StandardOutput;
                while (!sro.EndOfStream)
                {
                    String s = sro.ReadLine();
                    if (s != "")
                    {
                        Trace.WriteLine(s);
                        //messageQueue.Enqueue(s);
                    }
                }
                StreamReader sre = activeCliProcess.StandardError;
                while (!sre.EndOfStream)
                {
                    String s = sre.ReadLine();
                    if (s != "")
                    {
                        Trace.WriteLine(s);
                        //messageQueue.Enqueue(s);
                    }
                }

                stateQueue.Enqueue(new Tuple<GameRecord, Task, Status>(record, currentTask, Status.Fail));
                returnStatus = Status.Fail;

                stateQueueMutex.ReleaseMutex();

                if (addRetry)
                    taskQueue.Enqueue(new Tuple<GameRecord, Task>(record, currentTask));
            }
        }
        else
        {
            activeCliProcess.Kill();
            stateQueueMutex.WaitOne();
            messageQueue.Enqueue("Process killed");
            stateQueue.Enqueue(new Tuple<GameRecord, Task, Status>(record, currentTask, Status.OK));
            returnStatus = Status.OK;
            stateQueueMutex.ReleaseMutex();

            // TODO: Check if it was successfull, //  update light bulb state and 
            if (nextTask != Task.None)
                taskQueue.Enqueue(new Tuple<GameRecord, Task>(record, nextTask));
        }
        activeCliProcess = null;

        return returnStatus;
    }
#endregion

    List<GameRecord> GetHardCodedListJust1()
    {
        // Jos monta oppilasta tekee samaa peliä, käytä nimenä molempia "Jaakko&Jussi"
        //  Ei välejä
        return new List<GameRecord>(){
            new GameRecord(){
                Author="Alex&JoonaR",
                GameName="Zombie Swing",
                SVNRepo="https://github.com/magishark/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"Rope Swing"
                },
                Solution=@"Rope Swing\Rope Swing.sln"},};
    }

    List<GameRecord> GetHardCodedList()
    {
        // Jos monta oppilasta tekee samaa peliä, käytä nimenä molempia "Jaakko&Jussi"
        //  Ei välejä
        return new List<GameRecord>(){
            new GameRecord(){
                Author="Alex&JoonaR",
                GameName="Zombie Swing",
                SVNRepo="https://github.com/magishark/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"Rope Swing"
                },
                Solution=@"Rope Swing\Rope Swing.sln"},

            /*new GameRecord(){
                Author="Antti-Jussi",
                GameName="?",
                SVNRepo=@"https://github.com/aj-pelikurssi2014/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"Tasohyppelypeli1"
                },
                Solution=@"Tasohyppelypeli1\Tasohyppelypeli1.sln"},*/

            new GameRecord(){
                Author="Atte",
                GameName="Crazy Greg",
                SVNRepo=@"https://github.com/JeesMies00/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"GrazyGreg.sln",
                    @"GrazyGreg"
                },
                Solution=@"GrazyGreg.sln"},

            new GameRecord(){
                Author="Dani",
                GameName="bojoing",
                SVNRepo=@"https://github.com/daiseri45/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"bojoing",
                },
                Solution=@"bojoing\bojoing.sln"},

            new GameRecord(){
                Author="Emil-Aleksi",
                GameName="Rainbow Fly",
                SVNRepo=@"https://github.com/EA99/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"RainbowFly",
                },
                Solution=@"RainbowFly\RainbowFly.sln"},

            new GameRecord(){
                Author="Jere",
                GameName="Suklaakakku",
                SVNRepo=@"https://github.com/jerekop/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"FysiikkaPeli1",
                    @"FysiikkaPeli1.sln",
                },
                Solution=@"FysiikkaPeli1.sln"},

            new GameRecord(){
                Author="Joel",
                GameName="Urhea Sotilas",
                SVNRepo=@"https://github.com/JopezSuomi/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"UrheaSotilas",
                },
                Solution=@"UrheaSotilas\UrheaSotilas.sln"},

            new GameRecord(){
                Author="JoonaK",
                GameName="_insert name here_",
                SVNRepo=@"https://github.com/kytari/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"_Insert name here_",
                    @"_Insert name here_.sln",
                },
                Solution=@"_Insert name here_.sln"},


            new GameRecord(){
                Author="Saku&Joeli",
                GameName="Flappy derp",
                SVNRepo=@"https://github.com/EXIBEL/sejypeli.git/trunk",
                ToFetch=new List<string>(){
                    @"Falppy derp Saku",
                },
                Solution=@"Falppy derp Saku\Falppy derp Saku.sln"},
        };
    }
}
