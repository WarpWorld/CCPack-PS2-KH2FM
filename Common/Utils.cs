using ConnectorLib;
using Timer = System.Timers.Timer;
namespace CrowdControl.Games.Packs.KH2FM;

public static class Utils {
    public static bool CheckTPose(IPS2Connector connector)
    {
        if (connector == null) return false;

        connector.Read64LE(0x20341708, out ulong animationStateOffset);
        connector.Read32LE(animationStateOffset + 0x2000014C, out uint animationState);

        return animationState == 0;
    }

    public static void FixTPose(IPS2Connector connector)
    {
        if (connector == null) return;

        connector.Read64LE(0x20341708, out ulong animationStateOffset);

        connector.Read8(0x2033CC38, out byte cameraLock);

        if (cameraLock == 0)
        {
            connector.Read16LE(animationStateOffset + 0x2000000C, out ushort animationState);

            // 0x8001 is Idle state
            if (animationState != 0x8001)
            {
                connector.Write16LE(animationStateOffset + 0x2000000C, 0x40);
            }
        }
    }

    public static void TriggerReaction(IPS2Connector connector) {
        Timer timer = new()
        {
            AutoReset = true,
            Enabled = true,
            Interval = 10
        };

        timer.Elapsed += (obj, ev) =>
        {
            connector.Read16LE(DriveAddresses.ReactionEnable, out ushort value);

            if (value == 5 || DateTime.Compare(DateTime.Now, ev.SignalTime.AddSeconds(30)) > 0) timer.Stop();

            connector.Write8(ButtonAddresses.ButtonsPressed, (byte)ButtonValues.Triangle);
            connector.Write8(ButtonAddresses.ButtonMask, 0x10);
            connector.Write8(ButtonAddresses.ButtonsDown, 0xEF);
            connector.Write8(ButtonAddresses.TriangleDown, 0xFF); // Triangle button down
            connector.Write8(ButtonAddresses.TrianglePressed, 0xFF); // Triangle button pressed
        };
        timer.Start();
    }
}