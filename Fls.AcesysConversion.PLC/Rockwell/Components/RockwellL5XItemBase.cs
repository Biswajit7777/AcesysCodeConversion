using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Logging;
using Serilog;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components;

public abstract class RockwellL5XItemBase : XmlElement, IMessageBoardReferenceable, IMessageBoardSubscriber
{
    private readonly ILogger Logger;

    public RockwellL5XItemBase(string? prefix, string localName, string? namespaceURI, XmlDocument doc) : base(prefix, localName, namespaceURI, doc)
    {
        Logger = LogHelper.Instance.Logger;
    }

    public int MessageboardReference { get; set; }

    public virtual void UpgradeVersion(L5XCollection original, RockwellUpgradeOptions upgradeOptions, IProgress<string> progress)
    {
        return;
    }
    public virtual void UpgradeVersion(RockwellL5XProject originalProject, RockwellUpgradeOptions upgradeOptions, IProgress<string> progress)
    {
        return;
    }
    //public abs void UpgradeVersion(L5XCollection original, , RockwellUpgradeOptions upgradeOptions);

    public void LogMessage(string message, Serilog.Events.LogEventLevel level)  //limit to levels
    {
        switch (level)
        {
            case Serilog.Events.LogEventLevel.Information:
                if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Information))
                {
                    Logger.Information(message, level);
                }
                break;
            case Serilog.Events.LogEventLevel.Warning:
                if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Warning))
                {
                    Logger.Warning(message, level);
                }
                break;
            case Serilog.Events.LogEventLevel.Error:
                if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Error))
                {
                    Logger.Error(message, level);
                }
                break;
            case Serilog.Events.LogEventLevel.Fatal:
                if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Fatal))
                {
                    Logger.Fatal(message, level);
                }
                break;
            case Serilog.Events.LogEventLevel.Debug:
                if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                {
                    Logger.Debug(message, level);
                }
                break;
            case Serilog.Events.LogEventLevel.Verbose:
                if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
                {
                    Logger.Verbose(message, level);
                }
                break;

        }
    }

    public void AnnounceNewUserMessage(UserMessage userMessage)
    {
        throw new NotImplementedException();
    }
}

