using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Cliente
{
    internal class Cliente
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Empezando cliente!");

            Socket socketCliente = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            
            IPEndPoint endpointLocal = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            socketCliente.Bind(endpointLocal);
            
            IPEndPoint endpointServidor = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            socketCliente.Connect(endpointServidor);
            Console.WriteLine("Conectado al servidor!");
            
            NetworkDataHelper ndh = new NetworkDataHelper(socketCliente);

            bool salir = false;
            while (!salir)
            {
                Console.Write("Ingresar un mensaje para el servidor: ");
                string mensaje = Console.ReadLine();
                if (mensaje == "salir")
                {
                    salir = true;
                    continue;
                }
                byte[] bufferMensaje = Encoding.UTF8.GetBytes(mensaje);
                int largoMensaje = bufferMensaje.Length;
                byte[] bufferLargoMensaje = BitConverter.GetBytes(largoMensaje);
                ndh.Send(bufferLargoMensaje);
                ndh.Send(bufferMensaje);
                Console.WriteLine($"Mensaje enviado: \"{mensaje}\"");
            }
            Console.WriteLine("Se cierra la conexion...");
            ndh.Disconnect();
        }
    }
}
