﻿using System.IO.Ports;
using System.Reflection.Emit;
using System.Text;
using lab1;

namespace lab1 {
    public class Port : IDisposable 
    {
        protected SerialPort serialPort;

        public Port(string serialPortName, int speed = 19200) 
        {
            serialPort = new SerialPort(serialPortName, speed, Parity.None, 8, StopBits.One);
            serialPort.Open();

            serialPort.ReadTimeout = 200;
            serialPort.WriteTimeout = 500;
        }

        public void Dispose() 
        {
            serialPort.Dispose();
        }

        public virtual void Do() { }
    }
    public class Consumer : Port 
    {
        public Consumer(string serialPortName, int speed) : base(serialPortName, speed) 
        {
            serialPort.DataReceived += new SerialDataReceivedEventHandler(OutputData);
        }

        public override void Do() 
        {
            Console.WriteLine($"Consumer on port {serialPort.PortName}");
            while (true);
        }

        private void OutputData(object sender, SerialDataReceivedEventArgs e) 
        {
            var packageSize = 24;
            var buffer = new byte[packageSize];
    
            var packages = new List<Package>();

            var hammingsCode = new HammingCode();
            var bitStaffing = new BitStaffing();

            try
            {
                while(serialPort.Read(buffer, 0, packageSize) != 0)
                {
                    var recievePackage = new Package().Deserialize(buffer);
                    packages.Add(recievePackage);
                }
            }
            catch (TimeoutException)
            {
                List<byte> recievedDataBytes = new List<byte> { };
                for(int i = 0; i < packages.Count; i++)
                {
                    recievedDataBytes.AddRange(packages[i].data);
                }

                var bytesArray = recievedDataBytes.ToArray();

                var decodeStaffing = bitStaffing.DecodeData(bytesArray);
                var decodeHamming = hammingsCode.Decode(decodeStaffing, packages[0].FCS);

                Console.WriteLine($"Message = {decodeHamming}");
            }
        }
    }
    public class Producer : Port 
    {
        public Producer(string serialPortName, int speed) : base(serialPortName, speed) { }

        public override void Do() 
        {
            Console.WriteLine($"Producer on port {serialPort.PortName}");

            while (true) 
            {
                Console.Write("\n\nWrite message: ");
                var data = Console.ReadLine();

                var bitStaffing = new BitStaffing();
                var hammingsCode = new HammingCode();

                var hammingBytes = hammingsCode.Encode(Encoding.ASCII.GetBytes(data));
                var staffingBytes = bitStaffing.EncodeData(hammingBytes);

                var package = new Package();

                int dataLength = package.length;
                var countOfPackage = Math.Ceiling((double)data.Length / (double)dataLength);

                Console.WriteLine($"\n\nPackages count: {countOfPackage}");

                for (int i = 0; i < countOfPackage; i++)
                {
                    var partOfData = staffingBytes.Skip(i * dataLength).Take(dataLength).ToArray();

                    package = new Package();

                    package.data = partOfData;
                    package.length = package.data.Length;
                    package.FCS = hammingsCode.GetFCS(hammingBytes);

                    var packageBytes = package.Serialize();

                    Console.WriteLine("\nI send " + Encoding.ASCII.GetString(package.data));

                    serialPort.Write(packageBytes, 0, packageBytes.Length);
                }
            }
        }
    }
}
