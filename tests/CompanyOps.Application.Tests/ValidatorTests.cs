using CompanyOps.Application.Assets;
using CompanyOps.Application.Requests.CreateRequest;
using CompanyOps.Domain.Assets;
using CompanyOps.Domain.Requests;
using Xunit;

namespace CompanyOps.Application.Tests;

/// <summary>
/// Application-boundary input validation (non-negotiable #2). These cover the gaps the
/// scan surfaced — chiefly a <em>missing</em> required enum that the non-nullable type would
/// otherwise have defaulted silently — plus the required/length/enum-range rules.
/// </summary>
public class ValidatorTests
{
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Dept = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static CreateRequestCommand CreateCmd(
        string title = "Laptop", RequestType? type = RequestType.Procurement,
        RequestPriority? priority = RequestPriority.Medium, RequestCategory? category = null) =>
        new(title, null, type, priority, category, Actor, Dept);

    private static RegisterAssetCommand AssetCmd(
        string tag = "AST-1", string name = "MacBook Pro", AssetType? type = AssetType.Laptop) =>
        new(tag, name, type, Actor);

    [Fact]
    public void CreateRequest_ValidCommand_Passes()
        => Assert.True(new CreateRequestValidator().Validate(CreateCmd()).IsValid);

    [Fact]
    public void CreateRequest_MissingType_Fails()
    {
        var result = new CreateRequestValidator().Validate(CreateCmd(type: null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateRequestCommand.Type));
    }

    [Fact]
    public void CreateRequest_EmptyTitle_Fails()
        => Assert.False(new CreateRequestValidator().Validate(CreateCmd(title: "   ")).IsValid);

    [Fact]
    public void CreateRequest_TitleTooLong_Fails()
        => Assert.False(new CreateRequestValidator().Validate(CreateCmd(title: new string('x', Request.TitleMaxLength + 1))).IsValid);

    [Fact]
    public void CreateRequest_OutOfRangeEnum_Fails()
        => Assert.False(new CreateRequestValidator().Validate(CreateCmd(priority: (RequestPriority)99)).IsValid);

    [Fact]
    public void RegisterAsset_ValidCommand_Passes()
        => Assert.True(new RegisterAssetValidator().Validate(AssetCmd()).IsValid);

    [Fact]
    public void RegisterAsset_MissingType_Fails()
    {
        var result = new RegisterAssetValidator().Validate(AssetCmd(type: null));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterAssetCommand.Type));
    }

    [Fact]
    public void RegisterAsset_EmptyTag_Fails()
        => Assert.False(new RegisterAssetValidator().Validate(AssetCmd(tag: "")).IsValid);

    [Fact]
    public void RegisterAsset_OutOfRangeType_Fails()
        => Assert.False(new RegisterAssetValidator().Validate(AssetCmd(type: (AssetType)99)).IsValid);
}
