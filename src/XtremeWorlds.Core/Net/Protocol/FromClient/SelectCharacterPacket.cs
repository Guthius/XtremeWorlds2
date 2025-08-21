namespace Core.Net.Protocol.FromClient;

public sealed record SelectCharacterPacket(int Slot) : IPacket<SelectCharacterPacket>
{
    public static SelectCharacterPacket Deserialize(PacketReader reader)
    {
        return new SelectCharacterPacket(reader.ReadByte());
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteEnum(Packets.ClientPackets.CUseChar);
        writer.WriteByte((byte) Slot);
    }
};