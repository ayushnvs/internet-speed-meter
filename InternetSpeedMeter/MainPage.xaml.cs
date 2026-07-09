using InternetSpeedMeter.Services;

namespace InternetSpeedMeter
{
    public partial class MainPage : ContentPage
    {
        private bool _isMonitoring;
        private readonly ISpeedService _speedService;

        public MainPage()
        {
            InitializeComponent();
            
            // Resolve platform instance mapping definitions safely
            _speedService = DependencyService.Get<ISpeedService>();

            // Subscribe to real-time events coming from our background thread loop
            MessagingCenter.Subscribe<object, double>(this, "SpeedUpdate", (sender, speedKb) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (speedKb >= 1024)
                    {
                        double mb = speedKb / 1024.0;
                        LblSpeedValue.Text = mb.ToString("F1");
                        LblSpeedUnit.Text = "M/s";
                        LblStatDl.Text = $"{mb:F1} M/s";
                    }
                    else
                    {
                        LblSpeedValue.Text = speedKb.ToString("F1");
                        LblSpeedUnit.Text = "K/s";
                        LblStatDl.Text = $"{speedKb:F1} K/s";
                    }
                });
            });
        }

        private async void OnMonitorClicked(object sender, EventArgs e)
        {
            if (_isMonitoring)
            {
                _speedService.StopService();
                _isMonitoring = false;
                BtnMonitor.Text = "Start Monitoring";
                BtnMonitor.BackgroundColor = Color.FromArgb("#2563EB");
                
                LblSpeedValue.Text = "0.0";
                LblSpeedUnit.Text = "K/s";
                LblStatDl.Text = "0.0 K/s";
            }
            else
            {
                // Request runtime notification tracking clearances on modern variants safely
                var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Notification permissions are required to provide a system tray speed counter.", "OK");
                    return;
                }

                _speedService.StartService();
                _isMonitoring = true;
                BtnMonitor.Text = "Stop Monitoring";
                BtnMonitor.BackgroundColor = Color.FromArgb("#DC2626");
            }
        }
    }
}