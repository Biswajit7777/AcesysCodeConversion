using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.PLC.Rockwell;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Rockwell.Components.AddOns;
using Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;
using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tasks;
using Fls.AcesysConversion.PLC.Siemens.Components;
using Fls.AcesysConversion.PLC.Siemens.Components.DataBlock;
using Fls.AcesysConversion.PLC.Siemens.Components.FunctionBlocks_FB;
using Fls.AcesysConversion.PLC.Siemens.Components.SystemFunctionBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens
{
    public class UpgradeEngineFactorySiemens
    {
        private string Current { get; set; }
        private string Original { get; set; }
        private SiemensProject Project { get; set; }
        private AcesysVersions From { get; set; }
        private AcesysVersions To { get; set; }
        private PlcManufacturer PLC { get; set; }
        private Type? EntityType { get; set; }

        public UpgradeEngineFactorySiemens()
        {
                
        }

        public UpgradeEngineFactorySiemens SetCollectionsSiemens(string current, string original, SiemensProject project)
        {
            Current = current;
            Original = original;
            Project = project;
            return this;
        }

        public UpgradeEngineFactorySiemens SetUpgradeProperties(AcesysVersions from, AcesysVersions to, PlcManufacturer plc, Type type)
        {
            From = from;
            To = to;
            PLC = plc;
            EntityType = type;
            return this;
        }

        public UpgradeEngineSiemens? Create()
        {
            UpgradeEngineSiemens? upgradeProcessor = null;

            switch (PLC)
            {
                case PlcManufacturer.Siemens:

                    switch (From)
                    {
                        case AcesysVersions.V77:

                            if (To == AcesysVersions.V80)
                            {
                                switch (EntityType?.Name)
                                {
                                    case nameof(SiemensDataTypes):
                                        upgradeProcessor = new V7ToV8DataTypeUpgradeEngine(Current, Original, Project);
                                        break;
                                    case nameof(SiemensFBs):
                                        upgradeProcessor = new V7ToV8FBUpgradeEngine(Current, Original, Project);
                                        break;
                                    case nameof(SiemensSFBs):
                                        upgradeProcessor = new V7ToV8SFBUpgradeEngine(Current, Original, Project);
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
                case PlcManufacturer.Schnieder: //implement as needed
                    break;
            }

            return upgradeProcessor;
        }
    }
}
