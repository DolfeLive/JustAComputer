namespace Computer;
public class RAM
{
    public byte[] ram;

    public RAM(int size)
    {
        ram = new byte[size];
    }

    public byte this[int index]
    {
        get => ram[index];
        set => ram[index] = value;
    }
}
