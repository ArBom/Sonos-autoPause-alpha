using ByteDev.Sonos;
using ByteDev.Sonos.Models;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Sonos_autoPause_alpha
{
    public class Program
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public struct PausedSonosControler
        {
            public readonly SonosController sonosController;
            public readonly SonosVolume? previousVolume;

            public PausedSonosControler(SonosController sonosController, SonosVolume? previousVolume = null)
            {
                this.sonosController = sonosController;
                this.previousVolume = previousVolume;
            }
        }

        static IReadOnlyList<SonosController> ConnectedSonosControllers = [];
        static List<PausedSonosControler> PausedHereSonosControllers = [];
        static bool MuteInsteadStop;
        static int PlayDelayTime;
        static int StopDelayTime;

        static CancellationTokenSource StopMakeMuted;
        static List<Task> MuteInRunTasks;

        public static void Main(string[] args)
        {
            bool ProvideForFocusSession;
            bool NoWindow;
            List<string> SonosesIPs = [];
            List<SonosController> ConnectedSonosControllersTemp = [];
            PausedHereSonosControllers = [];
            SonosControllerFactory sonosControllerFactory = new();
            StopMakeMuted = default;

            #region args
            if (args.Length == 0)
            {
                NoWindow = true;
                ProvideForFocusSession = true;
                MuteInsteadStop = true;
                StopDelayTime = 400;
                PlayDelayTime = 500;
                SonosesIPs.Add("192.168.0.100");

            }
            else
            {
                if (!bool.TryParse(args[0], out NoWindow))
                {
                    Console.WriteLine("Wrong arg. of hiding window.");
                    NoWindow = false;
                }

                if (!bool.TryParse(args[1], out ProvideForFocusSession))
                {
                    Console.WriteLine("Wrong arg. of provideing for focus session.");
                    ProvideForFocusSession = true;
                }

                if (!bool.TryParse(args[2], out MuteInsteadStop))
                {
                    Console.WriteLine("Wrong arg. of action type (Stop or mute).");
                    ProvideForFocusSession = false;
                }

                if (!int.TryParse(args[3], out StopDelayTime))
                {
                    Console.WriteLine("Wrong arg. of delay stop.");
                    StopDelayTime = 150;
                }

                if (StopDelayTime < 0)
                    StopDelayTime = -StopDelayTime;

                if (!int.TryParse(args[4], out PlayDelayTime))
                {
                    Console.WriteLine("Wrong arg. of delay play.");
                    PlayDelayTime = 1500;
                }

                if (PlayDelayTime < 0)
                    PlayDelayTime = -PlayDelayTime;

                for (int i = 5; i != args.Length; ++i)
                {
                    if (IPAddress.TryParse(args[i], out _))
                        SonosesIPs.Add(args[i]);
                    else
                        Console.WriteLine("Wrong arg. of sonos ip - arg no " + i);
                }
            }
            #endregion

            if (NoWindow)
            {
                IntPtr WinH = Process.GetCurrentProcess().MainWindowHandle;
                ShowWindow(WinH, 0);
            }

            foreach (string IPaddr in SonosesIPs)
            {
                ConnectedSonosControllersTemp.Add(sonosControllerFactory.Create(IPaddr));
            }

            ConnectedSonosControllers = ConnectedSonosControllersTemp;

            SystemMediaObserver.FadeMusicOut += PlayPauseControllers;
            SystemMediaObserver systemMediaObserver = new(false, StopDelayTime, PlayDelayTime);

            Console.WriteLine("Programm is running, press any key to quit..");
            Console.ReadKey(true);
        }

        private static void MakeSoftPause(PausedSonosControler controler, CancellationToken cancellationToken)
        {
            int diff = 1;
            int ActControlVolume = controler.sonosController.GetVolumeAsync().Result.Value;
            SonosVolume NextControlerVolume = new SonosVolume();

            while (!cancellationToken.IsCancellationRequested && ActControlVolume != SonosVolume.MinVolume) 
            {
                if (ActControlVolume - diff < SonosVolume.MinVolume)
                    ActControlVolume = SonosVolume.MinVolume;
                else
                    ActControlVolume = ActControlVolume - diff;

                NextControlerVolume = new(ActControlVolume);
                Task t = controler.sonosController.SetVolumeAsync(NextControlerVolume);
                Task.WaitAny(t);
                diff++;

                Thread.Sleep(95);
            }
        }

        private static void PauseControllers()
        {
            StopMakeMuted = new CancellationTokenSource();
            MuteInRunTasks = new List<Task>();

            foreach (SonosController controller in ConnectedSonosControllers)
            {
                try
                {
                    if (controller.GetIsPlayingAsync().Result == false)
                        continue;

                    SonosVolume controlleVolume = controller.GetVolumeAsync().Result;

                    if (MuteInsteadStop)
                    {                       
                        PausedSonosControler pausedSonosControler = new PausedSonosControler(controller, controlleVolume);
                        Task t = new Task(() => { MakeSoftPause(pausedSonosControler, StopMakeMuted.Token); });
                        t.Start();
                        MuteInRunTasks.Add(t);
                        PausedHereSonosControllers.Add(pausedSonosControler);
                    }
                    else
                    {
                        PausedSonosControler pausedSonosControler = new PausedSonosControler(controller, controlleVolume);
                        Task t = new Task(delegate { MakeSoftPause(pausedSonosControler, StopMakeMuted.Token); });
                        t.Start();
                            t.ContinueWith(delegate { controller.PauseAsync(); }, StopMakeMuted.Token).
                            ContinueWith(delegate { controller.SetVolumeAsync(controlleVolume); }, StopMakeMuted.Token);
                        MuteInRunTasks.Add(t);
                        PausedHereSonosControllers.Add(pausedSonosControler);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Some problem in time of controler make silence.");
                    Console.ResetColor();
                }
            }
        }

        private static void PlayControllers()
        {
            StopMakeMuted.Cancel();
            Task.WaitAll(MuteInRunTasks.ToArray());

            foreach (PausedSonosControler pausedSonosControler in PausedHereSonosControllers)
            {
                try
                {
                    pausedSonosControler.sonosController.SetVolumeAsync(pausedSonosControler.previousVolume);

                    if (!MuteInsteadStop)
                        pausedSonosControler.sonosController.PlayAsync();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Some problem in time of controler reactivate.");
                    Console.ResetColor();
                }
            }
        }

        private static void PlayPauseControllers(bool PauseNow)
        {
            if (PauseNow)
            {
                if (PausedHereSonosControllers.Count != 0)
                    return;

                PauseControllers();
            }
            else
            {
                PlayControllers();
                PausedHereSonosControllers = [];
            }
        }
    }
}