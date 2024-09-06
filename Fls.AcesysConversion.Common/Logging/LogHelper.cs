using Microsoft.Extensions.Configuration;
using Serilog;

namespace Fls.AcesysConversion.Common.Logging;

public class LogHelper
{
    private const string logConfigFileName = "serilogconfig.json";
    private static LogHelper? instance;
    public readonly ILogger Logger;

    private LogHelper()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(logConfigFileName).Build();

        Serilog.Core.Logger logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Logger = logger;
    }
    public static LogHelper Instance
    {
        get
        {
            instance ??= new LogHelper();
            return instance;
        }
    }

}
