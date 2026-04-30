using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SistemaF.Application.Common.Behaviors;

namespace SistemaF.Application;

// ═══════════════════════════════════════════════════════════════════════════════
//  APPLICATION DEPENDENCY INJECTION
//
//  Utilizzo in Program.cs / App.xaml.cs:
//    services.AddApplication();
// ═══════════════════════════════════════════════════════════════════════════════

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationDependencyInjection).Assembly;

        // MediatR — gestisce Command/Query/Event dispatch
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                            typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                            typeof(LoggingBehavior<,>));
        });

        // FluentValidation — registra tutti i validator del progetto
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
