using ByteDev.Sonos;
using System.Net;

namespace Sonos_autoPause_alpha
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool ProvideForFocusSession;
            List<string> SonosesIPs = [];

            if (args.Length == 0)
            {
                ProvideForFocusSession = true;
                StopDelayTime = 750;
                PlayDelayTime = 3000;
                SonosesIPs.Add("169.254.212.11");

            }
            else
            {
                if (!bool.TryParse(args[0], out ProvideForFocusSession))
                {
                    Console.WriteLine("Wrong arg. of provideing for focus session.");
                    ProvideForFocusSession = true;
                }

                if (!int.TryParse(args[1], out StopDelayTime))
                {
                    Console.WriteLine("Wrong arg. of delay stop.");
                    StopDelayTime = 750;
                }

                if (!int.TryParse(args[2], out StopDelayTime))
                {
                    Console.WriteLine("Wrong arg. of delay play.");
                    PlayDelayTime = 3000;
                }

                for (int i = 3; i != args.Length; ++i)
                {
                    if (IPAddress.TryParse(args[i], out _))
                        SonosesIPs.Add(args[i]);
                    else
                        Console.WriteLine("Wrong arg. of sonos ip - arg no " + i);
                }
            }

            List<SonosController> ConnectedSonosControllersTemp = [];
            PausedHereSonosControllers = [];
            SonosControllerFactory sonosControllerFactory = new();

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

        static IReadOnlyList<SonosController> ConnectedSonosControllers = [];
        static List<SonosController> PausedHereSonosControllers = [];
        static int PlayDelayTime;
        static int StopDelayTime;

        private static void PlayPauseControllers(bool PauseNow)
        {
            if (PauseNow)
            {
                if (PausedHereSonosControllers.Count != 0)
                    return;

                foreach (SonosController controller in ConnectedSonosControllers)
                {
                    try
                    {
                        if (controller.GetIsPlayingAsync().Result)
                        {
                            controller.PauseAsync();
                            PausedHereSonosControllers.Add(controller);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Some problem in time of controler starting " + e.ToString());
                    }
                }

            }
            else
            {
                foreach (SonosController controller in PausedHereSonosControllers)
                {
                    try
                    {
                        controller.PlayAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Some problem in time of controler stoppting " + e.ToString());
                    }
                }

                PausedHereSonosControllers = [];
            }
        }
    }
}