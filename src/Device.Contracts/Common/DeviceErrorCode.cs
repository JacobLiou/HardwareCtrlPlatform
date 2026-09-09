namespace Device.Contracts.Common;

public enum DeviceErrorCode
{
    None = 0,
    NotFound,
    Offline,
    Busy,
    Timeout,
    Cancelled,
    InvalidArgument,
    NotSupported,
    DriverError,
    CommunicationError,
    UnexpectedError
}