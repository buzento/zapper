using Zapper.PaymentsApi.Workers.PaymentSubmission;

namespace Zapper.PaymentsApi;

public static class WorkerRegistration
{
    // longer than the 35-second call a piece of work makes, plus its save, BackendCode.md section 23
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(45);

    public static IServiceCollection AddWorkers(this IServiceCollection services)
    {
        services.Configure<HostOptions>(SetShutdownTimeout);

        services.AddOptions<PaymentSubmissionWorkerOptions>()
                .BindConfiguration("Zapper:PaymentSubmissionWorker")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddHostedService<PaymentSubmissionWorker>();

        return services;
    }

    private static void SetShutdownTimeout(HostOptions host) => host.ShutdownTimeout = ShutdownTimeout;
}
