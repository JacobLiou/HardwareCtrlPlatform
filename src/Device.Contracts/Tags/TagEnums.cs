namespace Device.Contracts.Tags;

public enum TagQuality
{
    Unknown = 0,
    Good = 1,
    Bad = 2,
    Stale = 3
}

public enum TagAccess
{
    Read = 0,
    Write = 1,
    ReadWrite = 2
}
