using CompanyOps.Application.Requests.CreateRequest;
using CompanyOps.Application.Requests.GetRequest;
using CompanyOps.Application.Requests.ListRequests;
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
        return services;
    }
}
