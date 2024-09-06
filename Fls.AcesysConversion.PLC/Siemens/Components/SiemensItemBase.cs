using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Logging;
using Serilog;

namespace Fls.AcesysConversion.PLC.Siemens.Components
{
    public abstract class SiemensItemBase : IMessageBoardReferenceable, IMessageBoardSubscriber
    {
        private readonly ILogger Logger;
        protected string AwlContent;  // Content of the AWL file as a string
        protected string Prefix;      // Additional property for the prefix
        protected string LocalName;   // Additional property for the local name
        protected string NsURI;       // Additional property for the namespace URI

        // Sequence number for message board reference
        public int MessageboardReference { get; set; }

        public SiemensItemBase(string prefix, string localName, string nsURI, string awlContent, int seq)
        {
            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            LocalName = localName ?? throw new ArgumentNullException(nameof(localName));
            NsURI = nsURI ?? throw new ArgumentNullException(nameof(nsURI));
            AwlContent = awlContent ?? throw new ArgumentNullException(nameof(awlContent));

            MessageboardReference = seq;
            Logger = LogHelper.Instance.Logger;
        }

        protected SiemensItemBase(string prefix, string localName, string nsURI)
        {
            Prefix = prefix;
            LocalName = localName;
            NsURI = nsURI;
            Logger = LogHelper.Instance.Logger;
        }

        public virtual void UpgradeVersion(SiemensProject originalProject, SiemensUpgradeOptions upgradeOptions, IProgress<string> progress)
        {
            // Implement the logic to upgrade the AWL file based on the versioning needs
            LogMessage("Upgrading Siemens AWL content to the new version.", Serilog.Events.LogEventLevel.Information);
            progress?.Report("Upgrading AWL file...");
            // Actual upgrade logic here
        }

        public virtual void ParseAwlContent()
        {
            // Implement logic to parse and manipulate the AWL content
            LogMessage("Parsing AWL content.", Serilog.Events.LogEventLevel.Information);
            // Parsing logic goes here
        }

        public void LogMessage(string message, Serilog.Events.LogEventLevel level)
        {
            switch (level)
            {
                case Serilog.Events.LogEventLevel.Information:
                    if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Information))
                    {
                        Logger.Information(message);
                    }
                    break;
                case Serilog.Events.LogEventLevel.Warning:
                    if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Warning))
                    {
                        Logger.Warning(message);
                    }
                    break;
                case Serilog.Events.LogEventLevel.Error:
                    if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Error))
                    {
                        Logger.Error(message);
                    }
                    break;
                case Serilog.Events.LogEventLevel.Fatal:
                    if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Fatal))
                    {
                        Logger.Fatal(message);
                    }
                    break;
                case Serilog.Events.LogEventLevel.Debug:
                    if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                    {
                        Logger.Debug(message);
                    }
                    break;
                case Serilog.Events.LogEventLevel.Verbose:
                    if (Logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
                    {
                        Logger.Verbose(message);
                    }
                    break;
            }
        }

        public void AnnounceNewUserMessage(UserMessage userMessage)
        {
            throw new NotImplementedException();
        }

        // Abstract method to be implemented by derived classes
        public abstract string GenerateAwl();
    }
}