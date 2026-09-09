using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Simulators;

public sealed class SimulatedOpticalSwitch : SimulatedDeviceBase, IOpticalSwitch
{
    private readonly object _gate = new();
    private int _inputPort = 1;
    private int _outputPort = 1;

    public SimulatedOpticalSwitch(DeviceIdentity identity)
        : base(identity)
    {
    }

    public (int Input, int Output) CurrentPath
    {
        get
        {
            lock (_gate)
            {
                return (_inputPort, _outputPort);
            }
        }
    }

    public async Task<DeviceResult> SwitchToAsync(int inputPort, int outputPort, CancellationToken cancellationToken)
    {
        var early = await BeginOperationAsync(cancellationToken).ConfigureAwait(false);
        if (early is not null)
        {
            return early;
        }

        if (inputPort <= 0 || outputPort <= 0)
        {
            return DeviceResult.Fail(
                DeviceErrorCode.InvalidArgument,
                "Input and output ports must be positive.",
                Identity.DeviceId);
        }

        lock (_gate)
        {
            _inputPort = inputPort;
            _outputPort = outputPort;
        }

        return DeviceResult.Ok(Identity.DeviceId);
    }
}
