using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Rockwell.Components.AddOns;
using Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;
using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tasks;


namespace Fls.AcesysConversion.PLC.Rockwell;

public class UpgradeEngineFactory
{
    private L5XCollection Current { get; set; }
    private L5XCollection Original { get; set; }
    private RockwellL5XProject Project { get; set; }
    private AcesysVersions From { get; set; }
    private AcesysVersions To { get; set; }
    private PlcManufacturer PLC { get; set; }
    private Type? EntityType { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public UpgradeEngineFactory()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {

    }

    public UpgradeEngineFactory SetCollections(L5XCollection current, L5XCollection original, RockwellL5XProject project)
    {
        Current = current;
        Original = original;
        Project = project;
        return this;
    }

    public UpgradeEngineFactory SetUpgradeProperties(AcesysVersions from, AcesysVersions to, PlcManufacturer plc, Type type)
    {
        From = from;
        To = to;
        PLC = plc;
        EntityType = type;
        return this;
    }

    public UpgradeEngine? Create()
    {
        UpgradeEngine? upgradeProcessor = null;

        switch (PLC)
        {
            case PlcManufacturer.Rockwell:

                switch (From)
                {
                    case AcesysVersions.V77:

                        if (To == AcesysVersions.V80)
                        {
                            switch (EntityType?.Name)
                            {
                                case nameof(L5XDataTypes):
                                    upgradeProcessor = new V7ToV8DataTypesUpgradeEngine(Current, Original, Project);
                                    break;
                                case nameof(L5XAddOnInstructionDefinitions):
                                    upgradeProcessor = new V7ToV8AddOnUpgradeEngine(Current, Original, Project);
                                    break;
                                case nameof(L5XTags):
                                    upgradeProcessor = new V7ToV8TagsUpgradeEngine(Current, Original, Project);
                                    break;
                                case nameof(L5XPrograms):
                                    upgradeProcessor = new V7ToV8ProgramsUpgradeEngine(Current, Original, Project);
                                    break;
                                case nameof(L5XTasks):
                                    upgradeProcessor = new V7ToV8TasksUpgradeEngine(Current, Original, Project);
                                    break;
                            }

                        }
                        else if (To == AcesysVersions.V90)
                        {
                            //implement as needed
                        }
                        break;
                    case AcesysVersions.Unknown: //implement as needed
                        break;
                    case AcesysVersions.V80: //implement as needed
                        break;
                    case AcesysVersions.V90: //implement as needed
                        break;
                }
                break;
            case PlcManufacturer.Unknown: //implement as needed
                break;
            case PlcManufacturer.Siemens: //implement as needed
                break;
            case PlcManufacturer.Schnieder: //implement as needed
                break;
        }

        return upgradeProcessor;
    }
}

