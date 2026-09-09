using System.Text;
using Device.Drivers.Udl;

namespace Device.Client.Tests.Demos;

public class UdlMessageDecoderTests
{
    static UdlMessageDecoderTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void Decode_Uses_Gb2312_When_Udl_Error_Text_Is_Chinese()
    {
        var expected = "打开文件失败";
        var bytes = Encoding.GetEncoding("GB2312").GetBytes(expected + "\0");

        var actual = UdlErrorMessageDecoder.Decode(bytes);

        Assert.Equal(expected, actual);
    }
}
