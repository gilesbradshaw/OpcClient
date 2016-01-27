using OpcLabs.BaseLib.ComInterop;
using OpcLabs.EasyOpc;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using OpcLabs.EasyOpc.UA;
using OpcLabs.EasyOpc.UA.OperationModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlsSampleOpcClient
{
    class Program
    {

        static ConsoleKeyInfo UaOrDa()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine();
            Console.WriteLine("ua = u, da = d, any other key to quit ...");
            Console.ResetColor();
            return Console.ReadKey();
        }

        static void Writing(char value)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine(string.Format("writing {0}", value));
            Console.ResetColor();
        }

        static void Read(object value)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine();
            Console.WriteLine(string.Format("READ: {0}", value));
            Console.ResetColor();
        }
        
        static void Main(string[] args)
        {
            var key = UaOrDa();

            while (key.KeyChar == 'd' || key.KeyChar == 'u')
            {
                Console.WriteLine();
                if(key.KeyChar=='d')
                {
                    Da();
                }
                else
                {
                    Ua();
                }
                key = UaOrDa();
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("Thnx cu!");
            Console.ResetColor();


        }


        static void Ua()
        {





            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("UA! Press any number to write a value any other key to exit");
            Console.ResetColor();


            var uaClient = new EasyUAClient();

            uaClient.MonitoredItemChanged += (sender, e) =>
                 Console.WriteLine(
                     string.Format("{0} {1} {2}",
                     e.Arguments.State,
                     e.AttributeData.StatusCode,
                     e.AttributeData.Value
                     ));


            uaClient.SubscribeMonitoredItem(new EasyUAMonitoredItemArguments(
                 "uaState",
                 "opc.tcp://127.0.0.1:49320/",
                 "ns=2;s=Channel1.Device1.Tag1",
                 1000)
             );

            UAAttributeData attributeData = uaClient.Read(
                "opc.tcp://127.0.0.1:49320/",
                "ns=2;s=Channel1.Device1.Tag1");


            Read(attributeData.DisplayValue());

            var key = Console.ReadKey();

            while (key.KeyChar >= '0' && key.KeyChar <= '9')
            {
                Writing(key.KeyChar);
                uaClient.WriteValue(
                    new UAWriteValueArguments(
                        "opc.tcp://127.0.0.1:49320/",
                       "ns=2;s=Channel1.Device1.Tag1",
                        int.Parse(key.KeyChar.ToString())
                    )
                );
                key = Console.ReadKey();
            }

            uaClient.UnsubscribeAllMonitoredItems();




        }


        static void Da()
        {


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("DA! Press any number to write a value any other key to exit");
            Console.ResetColor();

            var daClient = new OpcLabs.EasyOpc.DataAccess.EasyDAClient();

            daClient.ItemChanged += (sender, e) =>
                Console.WriteLine(string.Format("da {0} {1} {2}",
                    e.Arguments.State,
                    e.Vtq.Quality,
                    e.Vtq.Value));

            daClient.SubscribeItem(
                "localhost",
                "Kepware.KEPServerEX.V5",
                "Channel1.Device1.Tag1",
                VarTypes.Int,
                1000,
                "daState"
            );

            var item = daClient.ReadItem(
                new ServerDescriptor("localhost", "Kepware.KEPServerEX.V5"),
                new DAItemDescriptor("Channel1.Device1.Tag1")
            );

            Read(item.DisplayValue());

 
            var key = Console.ReadKey();

            while (key.KeyChar >= '0' && key.KeyChar <= '9')
            {
                Writing(key.KeyChar);

                daClient.WriteItemValue(
                    new ServerDescriptor("localhost", "Kepware.KEPServerEX.V5"),
                    new DAItemDescriptor("Channel1.Device1.Tag1"),
                    int.Parse(key.KeyChar.ToString())
                );
                key = Console.ReadKey();
            }

            
            daClient.UnsubscribeAllItems();

        }
    }
}
