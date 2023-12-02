using ByteDev.Sonos;
using ByteDev.Sonos.Device;
using ByteDev.Sonos.Models;
using Sonos_autoPause_alpha;
using System.Net;

public class Program
{
    public static async Task Main(string[] args)
    {
        bool ProvideForFocusSession;
        List<string> SonosesIPs = new List<string>();

        if (args.Count() == 0)
        {
            ProvideForFocusSession = true;
            SonosesIPs.Add("169.254.212.11");

        }
        else
        {
            if (!Boolean.TryParse(args[0], out ProvideForFocusSession))
            {
                Console.WriteLine("Wrong arg. of provideing for focus session.");
                ProvideForFocusSession = true;
            }

            for (int i = 1; i!= args.Length; ++i)
            {
                if (IPAddress.TryParse(args[i], out _))
                    SonosesIPs.Add(args[i]);
                else
                    Console.WriteLine("Wrong arg. of sonos ip - arg no " + i);
            }
        }

        SonosController controller = new SonosControllerFactory().Create("169.254.212.11");

        ConnectedSonosControllers = new List<SonosController>();
        PausedHereSonosControllers = new List<SonosController>();

        ConnectedSonosControllers.Add(controller);

        SystemMediaObserver systemMediaObserver = new(true);
        systemMediaObserver.fadeMusicOut += PlayPauseControllers;

        Console.WriteLine("Programm is running, press any key to quit..");
        Console.ReadKey(true);
    }

    static List<SonosController> ConnectedSonosControllers = new List<SonosController>();
    static List<SonosController> PausedHereSonosControllers = new List<SonosController>();

    private static void PlayPauseControllers(bool PauseNow)
    {
        if (PauseNow)
        {
            PausedHereSonosControllers = new List<SonosController>();
            foreach (SonosController controller in ConnectedSonosControllers)
            {
                if (controller.GetIsPlayingAsync().Result)
                {
                    controller.PauseAsync();
                    PausedHereSonosControllers.Add(controller);
                }
            }
        }
        else
        {
            foreach (SonosController controller in PausedHereSonosControllers)
            {
                controller.PlayAsync();
            }
            PausedHereSonosControllers = new List<SonosController>();
        }
    }
}
