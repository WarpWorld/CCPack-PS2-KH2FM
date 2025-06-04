using ConnectorLib;
using CrowdControl.Common;
using CrowdControl.Games.SmartEffects;
using System;
using System.Collections.Generic;

namespace CrowdControl.Games.Packs.KH2FM;

public partial class KH2FM {
    [EffectHandler("zero_drive", "unlimited_drive")]
    public class SoraDrive : BaseEffect
    {
        public SoraDrive(KH2FM pack) : base(pack) { }

        public override EffectHandlerType Type => EffectHandlerType.Durational;

        public override IList<String> Codes { get; } = new[] { EffectIds.ZeroDrive, EffectIds.UnlimitedDrive };

        public override Mutex Mutexes { get; } = new[] {
            EffectIds.ZeroDrive,
            EffectIds.UnlimitedDrive,
            EffectIds.ProCodes,
            EffectIds.EzCodes
        };
        
        private bool isZeroDrive => Lookup(0, 1) == 0;
        private bool isUnlimitedDrive => Lookup(0, 1) == 1;

        public override bool StartAction()
        {
            if (isZeroDrive)
            {
                return Connector.Write8(DriveAddresses.DriveFill, 0)
                    && Connector.Write8(DriveAddresses.Drive, 0);
            }
            else
            {
                return Connector.Write8(DriveAddresses.DriveFill, 255)
                    && Connector.Write8(DriveAddresses.Drive, 7);
            }
        }

        public override bool RefreshAction() {
            if (isZeroDrive) {
                return Connector.Write8(DriveAddresses.DriveFill, 0)
                    && Connector.Write8(DriveAddresses.Drive, 0);
            } else {
                return Connector.Write8(DriveAddresses.DriveFill, 255)
                    && Connector.Write8(DriveAddresses.Drive, 7);
            }
        }

        public override bool StopAction() {
            return Connector.Write8(DriveAddresses.DriveFill, 255)
                    && Connector.Write8(DriveAddresses.Drive, 1);
        }
    }
}