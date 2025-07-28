
namespace Computer;
public class Input
{
    private Queue<byte> keyBuffer = new Queue<byte>();
    private readonly object lockObject = new object();

    private static readonly Dictionary<Keys, byte> KeyMappings = new Dictionary<Keys, byte>
    {
        { Keys.A, 0x41 }, { Keys.B, 0x42 }, { Keys.C, 0x43 }, { Keys.D, 0x44 },
        { Keys.E, 0x45 }, { Keys.F, 0x46 }, { Keys.G, 0x47 }, { Keys.H, 0x48 },
        { Keys.I, 0x49 }, { Keys.J, 0x4A }, { Keys.K, 0x4B }, { Keys.L, 0x4C },
        { Keys.M, 0x4D }, { Keys.N, 0x4E }, { Keys.O, 0x4F }, { Keys.P, 0x50 },
        { Keys.Q, 0x51 }, { Keys.R, 0x52 }, { Keys.S, 0x53 }, { Keys.T, 0x54 },
        { Keys.U, 0x55 }, { Keys.V, 0x56 }, { Keys.W, 0x57 }, { Keys.X, 0x58 },
        { Keys.Y, 0x59 }, { Keys.Z, 0x5A },

        { Keys.D0, 0x30 }, { Keys.D1, 0x31 }, { Keys.D2, 0x32 }, { Keys.D3, 0x33 },
        { Keys.D4, 0x34 }, { Keys.D5, 0x35 }, { Keys.D6, 0x36 }, { Keys.D7, 0x37 },
        { Keys.D8, 0x38 }, { Keys.D9, 0x39 },

        { Keys.Space, 0x20 }, { Keys.Enter, 0x0D }, { Keys.Escape, 0x1B },
        { Keys.Up, 0x80 }, { Keys.Down, 0x81 }, { Keys.Left, 0x82 }, { Keys.Right, 0x83 }
    };

    public void OnKeyPressed(Keys key)
    {
        if (KeyMappings.TryGetValue(key, out byte keyCode))
        {
            lock (lockObject)
            {
                keyBuffer.Enqueue(keyCode);
            }
        }
    }

    public bool HasKey()
    {
        lock (lockObject)
        {
            return keyBuffer.Count > 0;
        }
    }

    public byte GetKey()
    {
        lock (lockObject)
        {
            Console.WriteLine($"Keybuffer: {string.Join(", ", keyBuffer)}");
            return keyBuffer.Count > 0 ? keyBuffer.Dequeue() : (byte)0;
        }
    }

    public byte PeekKey()
    {
        lock (lockObject)
        {
            Console.WriteLine($"Keybuffer: {string.Join(", ", keyBuffer)}");
            return keyBuffer.Count > 0 ? keyBuffer.Peek() : (byte)0;
        }
    }
}
