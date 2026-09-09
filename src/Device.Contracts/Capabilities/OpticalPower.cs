namespace Device.Contracts.Capabilities;

public sealed record OpticalPower(
    double Value,
    string Unit,
    double? WavelengthNm = null);