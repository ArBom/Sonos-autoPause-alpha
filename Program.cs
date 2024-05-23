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
            public SonosController sonosController;
            public SonosVolume? previousVolume;

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

        public static void Main(string[] args)
        {
            bool ProvideForFocusSession;
            bool NoWindow;
            List<string> SonosesIPs = [];
            List<SonosController> ConnectedSonosControllersTemp = [];
            PausedHereSonosControllers = [];
            SonosControllerFactory sonosControllerFactory = new();

            #region args
            if (args.Length == 0)
            {
                NoWindow = false;
                ProvideForFocusSession = true;
                MuteInsteadStop = true;
                StopDelayTime = 750;
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
                    StopDelayTime = 400;
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

        private static void PauseControllers()
        {
            foreach (SonosController controller in ConnectedSonosControllers)
            {
                try
                {
                    if (controller.GetIsPlayingAsync().Result == false)
                        continue;

                    if (MuteInsteadStop)
                    {
                        SonosVolume controlleVolume = controller.GetVolumeAsync().Result;
                        PausedSonosControler pausedSonosControler = new PausedSonosControler(controller, controlleVolume);
                        controller.SetVolumeAsync(new SonosVolume(0));
                        PausedHereSonosControllers.Add(pausedSonosControler);
                    }
                    else
                    {
                        PausedSonosControler pausedSonosControler = new PausedSonosControler(controller, null);
                        controller.PauseAsync();
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
            foreach (PausedSonosControler pausedSonosControler in PausedHereSonosControllers)
            {
                try
                {
                    if (MuteInsteadStop)
                        pausedSonosControler.sonosController.SetVolumeAsync(pausedSonosControler.previousVolume);
                    else
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