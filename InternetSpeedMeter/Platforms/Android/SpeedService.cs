using Android.Content;
using InternetSpeedMeter.Services;
using InternetSpeedMeter.Platforms.Android.Services;

[assembly: Dependency(typeof(InternetSpeedMeter.Platforms.Android.SpeedService))]
namespace InternetSpeedMeter.Platforms.Android;

public class SpeedService : ISpeedService
{
    public void StartService()
    {
        var intent = new Intent(Platform.AppContext, typeof(SpeedMeterForegroundService));
        Platform.AppContext.StartForegroundService(intent);
    }

    public void StopService()
    {
        var intent = new Intent(Platform.AppContext, typeof(SpeedMeterForegroundService));
        Platform.AppContext.StopService(intent);
    }
}