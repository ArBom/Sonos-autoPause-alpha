using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.UI.Shell;
using static Sonos_autoPause_alpha.SystemMediaObserver;

namespace Sonos_autoPause_alpha
{
    internal class SystemMediaObserver
    {
        public delegate void FadeMusicOut(bool PauseNow);
        public event FadeMusicOut fadeMusicOut;

        public SystemMediaObserver(bool ProvideForFocusSession) 
        {
            globalSystemMediaTransportControlsSessionManager = GetSystemMediaTransportControlsSessionManager().Result;
            globalSystemMediaTransportControlsSessionManager.SessionsChanged += SessionChanged;
            ProvidingForFocusSession = ProvideForFocusSession;

            try
            {
                focusSessionManager = FocusSessionManager.GetDefault();
            }
            catch
            {
                focusSessionManager = null;
            }
        }

        private GlobalSystemMediaTransportControlsSessionManager globalSystemMediaTransportControlsSessionManager;
        IReadOnlyList<GlobalSystemMediaTransportControlsSession> current;
        private bool PreviesWasActivePlayer;
        private FocusSessionManager? focusSessionManager;
        public bool ProvidingForFocusSession;

        private void SessionChanged(GlobalSystemMediaTransportControlsSessionManager GlSyMeTrCoSeMa, SessionsChangedEventArgs SeCcEvAr)
        {
            if (current is not null)
                foreach (var s in current)
                    s.PlaybackInfoChanged -= PlaybackInfoChanged;

            var t = GlSyMeTrCoSeMa.GetSessions();
            if (t.Count == 0)
                SendOrder();
            else
                foreach (var s in t)
                    s.PlaybackInfoChanged += PlaybackInfoChanged;

            current = t;
            SendOrder();
        }

        private void SendOrder()
        {
            bool IsActivePlayer2 = IsActivePlayer();

            if (IsActivePlayer2 == PreviesWasActivePlayer)
                return;

            PreviesWasActivePlayer = IsActivePlayer2;

            if (IsActivePlayer2)
            {
                Console.WriteLine("Stop");
                fadeMusicOut.Invoke(true);
                return;
            }

            if (!ProvidingForFocusSession)
            {
                Console.WriteLine("Play");
                fadeMusicOut.Invoke(false);
                return;
            }

            if (focusSessionManager is not null) //Windows 11
                if (focusSessionManager.IsFocusActive)
                    return;
                else //windows 10
                {
                    int HourOfDay = DateTime.Now.TimeOfDay.Hours;
                    if (HourOfDay >= 22 || HourOfDay < 7)
                        return;
                }

            Console.WriteLine("Play");
            fadeMusicOut.Invoke(false);
        }

        private bool IsActivePlayer()
        {
            IReadOnlyList<GlobalSystemMediaTransportControlsSession> current;
            try
            {
                current = globalSystemMediaTransportControlsSessionManager.GetSessions();
            }
            catch
            {
                return false;
            }

            foreach (var s in current)
                if (s.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    return true;

            return false;
        }

        private void PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession GlSyMeTrCoSe, PlaybackInfoChangedEventArgs playbackInfoChangedEventArgs) =>
            SendOrder();

        private async Task<GlobalSystemMediaTransportControlsSessionManager> GetSystemMediaTransportControlsSessionManager() =>
            await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
    }
}
