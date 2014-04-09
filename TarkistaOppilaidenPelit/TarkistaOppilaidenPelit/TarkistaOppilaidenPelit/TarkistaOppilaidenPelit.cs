using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;
using System.Linq;
using System.IO;
using System.Diagnostics;

// Oppilas, PelinNimi, Repo, Lista checkoutattavista tiedostoista/kansioista, Solutiontiedosto
class GameRecord 
{ 
  public string Author;
  public string GameName;
  public string SVNRepo;
  public List<string> ToFetch;
  public string Solution;
}


/*
* For checking out we use
* > svn checkout <repo> <author_folder> --depth empty
* > cd <author_folder>/trunk
* > svn up <files/folders_you_want>
*/
public class TarkistaOppilaidenPelit : Game
{
    string SVN_CLI_EXE = @"C:\Program Files\TortoiseSVN\bin\svn.exe";
    string MSBUILD_EXE = @"C:\Program Files (x86)\MSBuild\12.0\Bin\MsBuild.exe";
    double TASK_COMPLETION_POLL_INTERVAL = 2.0;
    double PROCESS_CHECK_INTERVAL = 3.0;

    Queue<Tuple<GameRecord, Task, string>> taskQueue; // string is free paramter used when processing updates
    List<GameRecord> listOfGames;
    Process activeCliProcess;
    bool processing = false;

    enum Task
    {
        Checkout,
        UpdateListed,
        Compile,
        RunGame,
        None
    }
   
    

    public override void Begin()
    {
        SetWindowSize(800, 600);
        IsMouseVisible = true;
        // Kirjoita ohjelmakoodisi tähän

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        listOfGames = GetHardCodedList();
        List<string> authors = listOfGames.Select(ot => ot.Author).Distinct().ToList();

        if (listOfGames.Count != authors.Count)
            throw new ArgumentException("Tekijöiden pitää olla yksilöllisiä");

        taskQueue = new Queue<Tuple<GameRecord, Task, string>>();
        foreach (var record in listOfGames)
        {
            taskQueue.Enqueue( new Tuple<GameRecord, Task, string>(record, Task.Checkout, "") );
        }

        Timer processTimer = new Timer();
        processTimer.Interval = PROCESS_CHECK_INTERVAL;
        processTimer.Timeout += ProcessTaskList;
        processTimer.Start();
    }

    void ProcessTaskList()
    {
        if (processing)
            return;

        processing = true;
        var task = taskQueue.Dequeue();
        switch (task.Item2)
        {   
            case Task.Checkout:
                ProcessCheckoutRepo(task.Item1);
                break;
            case Task.UpdateListed:
                ProcessUpdateListed(task.Item1, task.Item3);
                break;
            case Task.Compile:
                ProcessCompile(task.Item1);
                break;
            case Task.RunGame:
                ProcessRunGame(task.Item1);
                break;
            default:
                break;
        }
    }

    void ProcessCheckoutRepo(GameRecord record)
    {
        // Directory existance implies existing checkout
        if (Directory.Exists(record.Author))
        {
            taskQueue.Enqueue(new Tuple<GameRecord, Task, string>(record, Task.UpdateListed, ""));
            processing = false;
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


    void ProcessUpdateListed(GameRecord record, string fetch)
    {
        if (fetch == "")
        {
            // Manipulate task list by adding new _real_ tasks for the update
            //  (it will take some time to actually do it, but it should not be problem)
            foreach (var toUpdate in record.ToFetch)
            {
                taskQueue.Enqueue(new Tuple<GameRecord, Task, string>(record, Task.UpdateListed, toUpdate));
            }
            processing = false;
        }
        else
        {
            Task currentTask = Task.UpdateListed;
            Task nextTask = Task.None;
            bool addRetry = false;
            // The success of the last update will determine if to try again or if to try and compile.
            if (fetch == record.ToFetch.Last())
            {
                nextTask = Task.Compile;
                addRetry = true;    
            }
            string command = String.Format("\"{0}\" up \"{1}\"", SVN_CLI_EXE, Path.Combine(Directory.GetCurrentDirectory(), record.Author, fetch));
            GenericProcessor(record, currentTask, nextTask, command, addRetry);
        }
    }

    void ProcessCompile(GameRecord record)
    {
        Task currentTask = Task.Compile;
        Task nextTask = Task.RunGame;
        string command = String.Format("\"{0}\" /nologo \"{1}\"", MSBUILD_EXE, record.Solution);
        GenericProcessor(record, currentTask, nextTask, command);
    }

    void ProcessRunGame(GameRecord record)
    {
        string gameExeName = "";
        foreach (string file in Directory.EnumerateFiles(
            record.Author, "*.exe", SearchOption.AllDirectories))
        {
            if (gameExeName == "")
                gameExeName = file;
            else
                processing = false;
                throw new IOException("Multiple exe files for the game");
        }

        Task currentTask = Task.RunGame;
        Task nextTask = Task.UpdateListed;
        string command = String.Format("\"{0}\"", gameExeName);
        GenericProcessor(record, currentTask, nextTask, command);
    }

    private void GenericProcessor(GameRecord record, Task currentTask, Task nextTask, string command, bool addRetry = true)
    {
        if (activeCliProcess == null)
        {
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

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;

            startInfo.Arguments = command;
            activeCliProcess.StartInfo = startInfo;
            activeCliProcess.Start();

            MessageDisplay.Add("Running command " + command);

            Action retry = () => GenericProcessor(record, currentTask, nextTask, command);
            Timer.SingleShot(TASK_COMPLETION_POLL_INTERVAL, retry);
        }
        else
        {
            if (!activeCliProcess.HasExited)
            {
                Action retry = () => GenericProcessor(record, currentTask, nextTask, command);
                Timer.SingleShot(TASK_COMPLETION_POLL_INTERVAL, retry);
            }
            else
            {
                // THIS is probably CMD.exe exitcode
                if (activeCliProcess.ExitCode==0)
                {
                    MessageDisplay.Add("Process exited with CODE 0. Output:");
                    StreamReader sr = activeCliProcess.StandardOutput;
                    while (!sr.EndOfStream)
                    {
                        String s = sr.ReadLine();
                        if (s != "")
                            MessageDisplay.Add(s);
                    }

                    // TODO: Check if it was successfull, //  update light bulb state and 
                    if (nextTask!=Task.None)
                        taskQueue.Enqueue(new Tuple<GameRecord, Task, string>(record, nextTask, ""));
                }
                else
                {
                    MessageDisplay.Add("Process exited with CODE 1. Output:");
                    StreamReader sr = activeCliProcess.StandardError;
                    while (!sr.EndOfStream)
                    {
                        String s = sr.ReadLine();
                        if (s != "")
                            MessageDisplay.Add(s);
                    }

                    if (addRetry)
                        taskQueue.Enqueue(new Tuple<GameRecord, Task, string>(record, currentTask, ""));
                }
                activeCliProcess = null;
                processing = false;
            }
        }
    }

    List<GameRecord> GetHardCodedList()
    {
        return new List<GameRecord>(){
            new GameRecord(){
                Author="Atte",
                GameName="Crazy Greg",
                SVNRepo=@"https://github.com/JeesMies00/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\GrazyGreg.sln",
                    @"trunk\GrazyGreg"
                },
                Solution=@"trunk\GrazyGreg.sln"},
        };

        // Jos monta oppilasta tekee samaa peliä, käytä nimenä molempia "Jaakko&Jussi"
        //  Ei välejä
        return new List<GameRecord>(){
            new GameRecord(){
                Author="Alex&JoonaR",
                GameName="Zombie Swing",
                SVNRepo="https://github.com/magishark/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\Rope Swing"
                },
                Solution=@"trunk\Rope Swing\Rope Swing.sln"},

            new GameRecord(){
                Author="Antti-Jussi",
                GameName="?",
                SVNRepo=@"https://github.com/aj-pelikurssi2014/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\Tasohyppelypeli1"
                },
                Solution=@"trunk\Tasohyppelypeli1\Tasohyppelypeli1.sln"},

            new GameRecord(){
                Author="Atte",
                GameName="Crazy Greg",
                SVNRepo=@"https://github.com/JeesMies00/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\GrazyGreg.sln",
                    @"trunk\GrazyGreg"
                },
                Solution=@"trunk\GrazyGreg.sln"},

            new GameRecord(){
                Author="Dani",
                GameName="bojoing",
                SVNRepo=@"https://github.com/daiseri45/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\bojoing",
                },
                Solution=@"trunk\bojoing\bojoing.sln"},

            new GameRecord(){
                Author="Emil-Aleksi",
                GameName="Rainbow Fly",
                SVNRepo=@"https://github.com/EA99/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\RainbowFly",
                },
                Solution=@"trunk\RainbowFly\RainbowFly.sln"},

            new GameRecord(){
                Author="Jere",
                GameName="?",
                SVNRepo=@"https://github.com/jerekop/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\FysiikkaPeli1",
                    @"trunk\FysiikkaPeli1.sln",
                },
                Solution=@"trunk\FysiikkaPeli1.sln"},

            new GameRecord(){
                Author="Joel",
                GameName="Urhea Sotilas",
                SVNRepo=@"https://github.com/JopezSuomi/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\UrheaSotilas",
                },
                Solution=@"trunk\UrheaSotilas\UrheaSotilas.sln"},

            new GameRecord(){
                Author="JoonaK",
                GameName="_insert name here_",
                SVNRepo=@"https://github.com/kytari/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\_Insert name here_",
                    @"trunk\_Insert name here_.sln",
                },
                Solution=@"trunk\_Insert name here_.sln"},


            new GameRecord(){
                Author="Saku&Joeli",
                GameName="Flappy derp",
                SVNRepo=@"https://github.com/EXIBEL/sejypeli.git",
                ToFetch=new List<string>(){
                    @"trunk\Falppy derp Saku",
                },
                Solution=@"trunk\Falppy derp Saku\Falppy derp Saku.sln"},
        };
    }
}
