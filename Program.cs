using ByteDev.Sonos;
using ByteDev.Sonos.Device;
using ByteDev.Sonos.Models;
using Sonos_autoPause_alpha;

public class Program
{
    public static async Task Main(string[] args)
    {
        SonosController controller = new SonosControllerFactory().Create("192.168.1.100");

        List<SonosController> ConnectedSonosControllers = new List<SonosController>();
        List<SonosController> PausedHereSonosControllers = new List<SonosController>();

        ConnectedSonosControllers.Add(controller);

        SystemMediaObserver systemMediaObserver = new(true);
        systemMediaObserver.fadeMusicOut += PlayPauseControllers;

        Console.WriteLine("Press any key to quit..");
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
