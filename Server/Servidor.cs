using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server
{
    internal class Servidor
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Empezando servidor!");

            Socket socketServer = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            
            IPEndPoint endpointLocal = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000); 
            
            socketServer.Bind(endpointLocal);

            socketServer.Listen(10); // escuchamos conexiones

            Console.WriteLine("Esperando a que se conecten clientes...");
            int clientesConectados = 0;
            while (true)
            {
                Socket socketCliente = socketServer.Accept(); // bloqueante
                clientesConectados++;
                int clienteId = clientesConectados;
                Thread t = new Thread(() => ManejarCliente(socketCliente, clienteId)); 
                t.Start();
            }

            Console.ReadLine();
        }

        static void ManejarCliente(Socket socketCliente, int clienteId)
        {
            Console.WriteLine($"Se conectó el cliente #{clienteId}!");
            bool clienteConectado = true;
            do
            {
                int recibidos;
                byte[] bufferLargoMensaje = new byte[sizeof(int)];
                recibidos = socketCliente.Receive(bufferLargoMensaje); // bloqueante
                if (recibidos == 0)
                {
                    clienteConectado = false;
                    continue;
                }
                int largoMensaje = BitConverter.ToInt32(bufferLargoMensaje);
                byte[] buffer = new byte[largoMensaje];
                recibidos = socketCliente.Receive(buffer); // bloqueante
                if (recibidos == 0)
                {
                    clienteConectado = false;
                    continue;
                }
                string mensaje = Encoding.UTF8.GetString(buffer);
                Console.WriteLine($"El cliente #{clienteId} envió: {mensaje}");
            } while (clienteConectado);
            Console.WriteLine($"Se cerro la conexion del lado del cliente #{clienteId}");
        }
    }
}
