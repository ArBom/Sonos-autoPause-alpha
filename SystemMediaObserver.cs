using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Control;
using Windows.Media.Playback;
using Windows.UI.Shell;
//using static Sonos_autoPause_alpha.SystemMediaObserver;

namespace Sonos_autoPause_alpha
{
    internal class SystemMediaObserver
    {
        public delegate void fadeMusicOut(bool PauseNow);
        public static event fadeMusicOut FadeMusicOut;

        public SystemMediaObserver(bool ProvideForFocusSession, int StopDelayTime, int StartDelayTime)
        {
            globalSystemMediaTransportControlsSessionManager = GetSystemMediaTransportControlsSessionManager().Result;
            globalSystemMediaTransportControlsSessionManager.SessionsChanged += SessionChanged;
            SessionChanged(globalSystemMediaTransportControlsSessionManager);
            ProvidingForFocusSession = ProvideForFocusSession;
            this.StartDelayTime = StartDelayTime;
            this.StopDelayTime = StopDelayTime;

            try
            {
                focusSessionManager = FocusSessionManager.GetDefault();
            }
            catch
            {
                focusSessionManager = null;
            }
        }

        private readonly int StartDelayTime;
        private readonly int StopDelayTime;
        private readonly bool ProvidingForFocusSession;

        private bool PreviesWasActivePlayer;
        private bool LastSendingOrder = false;

        private readonly GlobalSystemMediaTransportControlsSessionManager globalSystemMediaTransportControlsSessionManager;
        IReadOnlyList<GlobalSystemMediaTransportControlsSession> current;       
        private readonly FocusSessionManager? focusSessionManager;
        private CancellationTokenSource tokenSourceForSwitchDeley = new();

        private void SessionChanged(GlobalSystemMediaTransportControlsSessionManager GlSyMeTrCoSeMa, SessionsChangedEventArgs SeCcEvAr = null)
        {
            if (current is not null)
                foreach (var s in current)
                {
                    s.PlaybackInfoChanged -= PlaybackInfoChanged;
                }

            var t = GlSyMeTrCoSeMa?.GetSessions();
            if (t is null)
                return;

            foreach (var s in t)
            {
                s.PlaybackInfoChanged += PlaybackInfoChanged;
            }

            current = t;
            PlaybackInfoChanged();
        }


        private async Task SendOrder()
        {
            bool IsActivePlayer2 = IsActivePlayer();

            if (IsActivePlayer2 == PreviesWasActivePlayer)
                return;

            tokenSourceForSwitchDeley.Cancel();
            tokenSourceForSwitchDeley = new CancellationTokenSource();
            PreviesWasActivePlayer = IsActivePlayer2;

            if (IsActivePlayer2)
            {
                try
                {
                    await Task.Delay(StopDelayTime, tokenSourceForSwitchDeley.Token);
                }
                catch
                {
                    return;
                }

                if (!LastSendingOrder)
                {
                        Console.WriteLine("Stop");
                        LastSendingOrder = true;
                        FadeMusicOut?.Invoke(true);
                }

                return;

            }

            if (!ProvidingForFocusSession)
            {
                try
                {
                    await Task.Delay(StartDelayTime, tokenSourceForSwitchDeley.Token);
                }
                catch
                {
                    return;
                }

                if (LastSendingOrder)
                {
                    Console.WriteLine("Play");
                    LastSendingOrder = false;
                    FadeMusicOut?.Invoke(false);
                }
                
                return;
            }

            if (focusSessionManager is not null) //Windows 11
            { 
               if (focusSessionManager.IsFocusActive)
                   return;
            }
            else //windows 10*/
            {
                int HourOfDay = DateTime.Now.TimeOfDay.Hours;
                if (HourOfDay >= 22 || HourOfDay < 7)
                    return;
            }

            try
            {
                await Task.Delay(StartDelayTime, tokenSourceForSwitchDeley.Token);
            }
            catch
            {
                return;
            }

            if (LastSendingOrder)
            {
                PreviesWasActivePlayer = false;
                Console.WriteLine("Play");
                LastSendingOrder = false;
                FadeMusicOut?.Invoke(false);
            }
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

        private async void PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession GlSyMeTrCoSe = null, PlaybackInfoChangedEventArgs playbackInfoChangedEventArgs = null) =>
            await SendOrder();

        private static async Task<GlobalSystemMediaTransportControlsSessionManager> GetSystemMediaTransportControlsSessionManager() =>
            await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
    }
}
