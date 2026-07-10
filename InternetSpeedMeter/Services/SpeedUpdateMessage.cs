using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace InternetSpeedMeter.Services;

public class SpeedUpdateMessage(double value) : ValueChangedMessage<double>(value);