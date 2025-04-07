namespace CrowdControl.Games.Packs.KH2FM;

public static class WeaponValues {
    public static uint TinyWeapon = BitConverter.ToUInt32(BitConverter.GetBytes(-0.25f));
    public static uint NormalWeapon = BitConverter.ToUInt32(BitConverter.GetBytes(-1f));
    public static uint BigWeapon = BitConverter.ToUInt32(BitConverter.GetBytes(-4f));
}