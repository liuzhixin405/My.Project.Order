using Microsoft.AspNetCore.Builder;
using Ordering.BackgroundTasks;
using Ordering.BackgroundTasks.Extensions;
using Ordering.BackgroundTasks.Services;

var configuration = GetConfiguration();

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureServices(serv =>
{
    serv.Configure<BackgroundTaskSettings>(configuration)
                .AddOptions()
                .AddHostedService<GracePeriodManagerService>()
                .AddEventBus(configuration);
});

var app = builder.Build();
app.UseRouting();

app.Run();


static IConfiguration GetConfiguration()
{
    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", false, true)
        .AddEnvironmentVariables();
    return builder.Build();
}
public partial class Program
{
    public static string Namespace = typeof(BackgroundTaskSettings).Namespace;
    public static string AppName = Namespace.Substring(Namespace.LastIndexOf(".", Namespace.LastIndexOf(".") - 1) + 1);
}