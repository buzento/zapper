using Zapper.ApiCommon.Managers.ErrorMessage;
using Zapper.Domain.Entities.Error;
using Shouldly;
using Xunit;

namespace Zapper.UnitTests.Managers.ErrorMessage;

public sealed class ErrorMessageManagerTests
{
    [Fact]
    public void GetStatusByStatusCode_StatusPopulated_ReturnsStatus()
    {
        var status = new ErrorStatusEntity(404, "NotFound", "Not Found", "That record could not be found.");
        var manager = new ErrorMessageManager();
        manager.Populate([status], []);

        var result = manager.GetStatusByStatusCode(404);

        result.ShouldBeSameAs(status);
    }

    [Fact]
    public void GetStatusByStatusCode_StatusNotPopulated_ThrowsInvalidOperationException()
    {
        var manager = new ErrorMessageManager();
        manager.Populate([], []);

        var lookup = () => manager.GetStatusByStatusCode(404);

        lookup.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void GetMessageByCode_MessagePopulated_ReturnsMessage()
    {
        var message = new ErrorMessageEntity("NotFound", "That record could not be found.");
        var manager = new ErrorMessageManager();
        manager.Populate([], [message]);

        var result = manager.GetMessageByCode("NotFound");

        result.ShouldBeSameAs(message);
    }

    [Fact]
    public void GetMessageByCode_MessageNotPopulated_ReturnsNull()
    {
        var manager = new ErrorMessageManager();
        manager.Populate([], []);

        var result = manager.GetMessageByCode("NotFound");

        result.ShouldBeNull();
    }
}
