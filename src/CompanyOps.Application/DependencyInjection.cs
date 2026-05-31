using CompanyOps.Application.Auditing.ListAuditLogs;
using CompanyOps.Application.Requests.ApproveRequest;
using CompanyOps.Application.Requests.Comments.AddComment;
using CompanyOps.Application.Requests.Comments.ListComments;
using CompanyOps.Application.Requests.CreateRequest;
using CompanyOps.Application.Requests.FulfillRequest;
using CompanyOps.Application.Requests.GetRequest;
using CompanyOps.Application.Requests.ListRequests;
using CompanyOps.Application.Requests.RejectRequest;
using CompanyOps.Application.Requests.SubmitRequest;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyOps.Application;

/// <summary>
/// Registers Application use-case handlers. Referencing
/// Microsoft.Extensions.DependencyInjection.Abstractions is allowed here — it is a
/// composition contract, not infrastructure. Handlers are registered explicitly for
/// now; this is the seam where a mediator would later auto-register them.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateRequestHandler>();
        services.AddScoped<GetRequestByIdHandler>();
        services.AddScoped<ListRequestsHandler>();
        services.AddScoped<SubmitRequestHandler>();
        services.AddScoped<ApproveRequestHandler>();
        services.AddScoped<RejectRequestHandler>();
        services.AddScoped<FulfillRequestHandler>();
        services.AddScoped<AddCommentHandler>();
        services.AddScoped<ListCommentsHandler>();
        services.AddScoped<ListAuditLogsHandler>();
        return services;
    }
}
