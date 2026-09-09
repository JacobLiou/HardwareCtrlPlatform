using Device.Contracts.Common;

namespace Device.Server.Tests;

public class DeviceResultTests
{
    [Fact]
    public void Ok_SetsSuccessAndNoneError()
    {
        var result = DeviceResult.Ok("PM-01", "req-1");

        Assert.True(result.Success);
        Assert.Equal(DeviceErrorCode.None, result.ErrorCode);
        Assert.Equal("PM-01", result.DeviceId);
        Assert.Equal("req-1", result.RequestId);
    }

    [Fact]
    public void Fail_SetsErrorCodeAndMessage()
    {
        var result = DeviceResult<string>.Fail(DeviceErrorCode.Timeout, "timed out", "PM-01");

        Assert.False(result.Success);
        Assert.Equal(DeviceErrorCode.Timeout, result.ErrorCode);
        Assert.Equal("timed out", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public void TypedOk_CarriesData()
    {
        var result = DeviceResult<int>.Ok(42, "PM-01");

        Assert.True(result.Success);
        Assert.Equal(42, result.Data);
    }
}