using System.IO.Ports;
using System.Text;

class Program
{
    static void SendSlow(SerialPort sp, byte[] data)
    {
        sp.Write(data, 0, data.Length);

        sp.BaseStream.Flush();
    }

    static void SendCommand(SerialPort sp, params ushort[] values)
    {
        byte cksum;
        string s;
        List<ushort> vals = new List<ushort>(values);

        if (vals.Count > 8)
            throw new Exception("Invalid command");

        if (vals.Count < 8)
            vals.AddRange(new ushort[8 - vals.Count]);

        s = $"PVCI,{vals.Select(a => a.ToString()).Aggregate((a, b) => a + "," + b)}";

        cksum = s.Select(a => (byte)a).Aggregate((a, b) => (byte)(a ^ b));

        s += $"*{cksum:X}\r";

        SendSlow(sp, Encoding.ASCII.GetBytes("$" + s));
    }

    static void SendAndWaitForStatus(SerialPort sp, ushort status, params ushort[] values)
    {
        ushort rstatus = 0;
        string s;
        DateTime dt = DateTime.Now;

        do
        {
            if ((DateTime.Now - dt).TotalMilliseconds > 100)
            {
                SendCommand(sp, values);

                dt = DateTime.Now;
            }

            s = "";

            while (!s.EndsWith('\r'))
                s += (char)sp.ReadByte();

            if (!s.StartsWith('$'))
                continue;

            var ss = s.Split(',');

            rstatus = ushort.Parse(ss[16]);

        } while (rstatus != status);
    }

    static void Main()
    {
        SerialPort sp = new SerialPort("COM2", 19200);

        sp.Open();

        sp.DiscardInBuffer();

        Console.WriteLine("Running autocalibration");

        SendAndWaitForStatus(sp, 33, 0, 11);

        Console.WriteLine("Cycle controls and hit any key to accept results");
        Console.ReadKey(true);

        SendAndWaitForStatus(sp, 0, 0, 22);

        sp.Close();
    }
}
