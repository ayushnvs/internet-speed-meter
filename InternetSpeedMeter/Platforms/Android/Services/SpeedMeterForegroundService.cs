using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using Paint = Android.Graphics.Paint;
using Color = Android.Graphics.Color;

namespace InternetSpeedMeter.Platforms.Android.Services
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
    public class SpeedMeterForegroundService : Service
    {
        private const int NotificationId = 92901;
        private const string ChannelId = "SPEED_METER_CHANNEL";
        private bool _isRunning;
        private CancellationTokenSource _cts;

        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();

            CreateNotificationChannel();
            
            // Build initial fallback notification to start the foreground context
            var initialNotification = BuildDynamicNotification("0");
            StartForeground(NotificationId, initialNotification, global::Android.Content.PM.ForegroundService.TypeDataSync);

            // Execute the high-precision Linux kernel TrafficStats monitoring loop
            Task.Run(async () => await MonitoringLoopAsync(_cts.Token));

            return StartCommandResult.Sticky;
        }

        private async Task MonitoringLoopAsync(CancellationToken token)
        {
            long lastRxBytes = TrafficStats.TotalRxBytes;

            while (_isRunning && !token.IsCancellationRequested)
            {
                await Task.Delay(1500, token);

                long currentRxBytes = TrafficStats.TotalRxBytes;
                long deltaBytes = currentRxBytes - lastRxBytes;
                lastRxBytes = currentRxBytes;

                // Simple whole-number parsing strategy for clean status bar rendering
                double speedKb = deltaBytes / 1024.0;
                string speedDisplay = "0";

                if (speedKb >= 1024)
                {
                    speedDisplay = Math.Floor(speedKb / 1024.0).ToString(); // MB values
                }
                else if (speedKb > 0)
                {
                    speedDisplay = Math.Floor(speedKb).ToString(); // KB values
                }

                // Push messaging metrics directly back to UI layer via MAUI MessagingCenter or community patterns
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MessagingCenter.Send(this, "SpeedUpdate", speedKb);
                });

                // Dynamically refresh status bar asset layer configurations
                var manager = GetSystemService(NotificationService) as NotificationManager;
                manager?.Notify(NotificationId, BuildDynamicNotification(speedDisplay));
            }
        }

        private Notification BuildDynamicNotification(string speedText)
        {
            // Allocate a 48x48 pixel bounding block
            Bitmap bitmap = Bitmap.CreateBitmap(48, 48, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bitmap);

            Paint paint = new Paint
            {
                AntiAlias = true,
                Color = Color.White,
                FakeBoldText = true
            };
            paint.TextAlign = Paint.Align.Center;

            // Responsive font resizing depending on string length metrics
            if (speedText.Length > 2)
            {
                paint.TextSize = 34f;
                canvas.DrawText(speedText, 24f, 36f, paint);
            }
            else if (speedText.Length == 2)
            {
                paint.TextSize = 38f;
                canvas.DrawText(speedText, 24f, 38f, paint);
            }
            else
            {
                paint.TextSize = 44f;
                canvas.DrawText(speedText, 24f, 40f, paint);
            }

            var iconCompat = IconCompat.CreateWithBitmap(bitmap);

            return new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Speed Meter Active")
                .SetContentText($"Live Speed: {speedText}")
                .SetSmallIcon(iconCompat)
                .SetOngoing(true)
                .SetPriority(NotificationCompat.PriorityMin)
                .Build();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "Internet Speed Meter Monitoring", NotificationImportance.Low);
                var manager = GetSystemService(NotificationService) as NotificationManager;
                manager?.CreateNotificationChannel(channel);
            }
        }

        public override void OnDestroy()
        {
            _isRunning = false;
            _cts?.Cancel();
            StopForeground(StopForegroundFlags.Remove);
            base.OnDestroy();
        }
    }
}