using System;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace GeohashTest
{
    class TestProgram
    {
        static void Main(string[] args)
        {
            var h = new[]
            {
                85, 84, 81, 80, 69, 68, 65, 64,
                21, 20, 17, 16, 5, 4, 1, 0,
                85, 84, 81, 80, 69, 68, 65, 64,
                21, 20, 17, 16, 5, 4, 1, 0
            };
            var hh = h.Reverse().ToArray();
            for (var index = 0; index < hh.Length; index++)
            {
                if (index % 8 ==0)
                    Console.WriteLine();
                var i = hh[index];
                Console.Out.Write("0b");
                Console.Out.Write(Convert.ToString(i, 2).PadLeft(8, '0'));
                Console.Out.Write(",");
            }
            Console.Out.WriteLine();

            for (byte index = 0; index < 16; index++)
            {
                int i = (int) Geohash.Spread(index);
                Console.Out.Write("0b");
                Console.Out.Write(Convert.ToString(i, 2).PadLeft(8, '0'));
                Console.Out.Write(",");
            }
            Console.Out.WriteLine();
            for (byte index = 0; index < 16; index++)
            {
                int i = (int) Geohash.Spread(index);
                Console.Out.Write(i);
                Console.Out.Write(",");
            }
            Console.Out.WriteLine();

            int co = 0b111;
            const int count = 1000000;
            var r = new Random();
            for (var i = 0; i < count; i++)
            {
                var lat = (r.NextDouble()-0.5 ) *180;
                var lng = (r.NextDouble()-0.5 ) *360;
                var code1 = Geohash.EncodeInt(lat, lng);
                var code2 = Geohash.EncodeIntPDEP(lat, lng);
                if (code1 != code2)
                    throw new Exception();
            }
        }
    }
}