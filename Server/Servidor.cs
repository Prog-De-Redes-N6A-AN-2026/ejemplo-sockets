using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Server
{
    internal class Servidor
    {
        private static readonly SettingsManager settingsManager = new SettingsManager();
        
        static void Main(string[] args)
        {
            Console.WriteLine("Empezando servidor!");

            Socket socketServer = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            IPAddress ipServidor = IPAddress.Parse(settingsManager.LeerConfig(ServidorConfig.ClaveIpServidor));
            int puertoServidor = int.Parse(settingsManager.LeerConfig(ServidorConfig.ClavePuertoServidor));
            
            IPEndPoint endpointLocal = new IPEndPoint(ipServidor, puertoServidor); 
            
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
            NetworkDataHelper ndh = new NetworkDataHelper(socketCliente);
            bool clienteConectado = true;
            do
            {
                try
                {
                    byte[] bufferLargoNombreArchivo = ndh.Receive(Protocolo.LargoDeLargoNombreArchivo);
                    int largoNombreArchivo = BitConverter.ToInt32(bufferLargoNombreArchivo);

                    byte[] bufferNombreArchivo = ndh.Receive(largoNombreArchivo);
                    string nombreArchivo = Encoding.UTF8.GetString(bufferNombreArchivo);

                    byte[] bufferLargoArchivo = ndh.Receive(Protocolo.LargoDeLargoArchivo);
                    long largoArchivo =  BitConverter.ToInt64(bufferLargoArchivo);
                    
                    long desplazamiento = 0;
                    long numPartes = Protocolo.CalcularCantidadDePartes(largoArchivo);
                    long parteActual = 1;

                    try
                    {
                        using (FileStreamHelper fsh = new FileStreamHelper(
                                   nombreArchivo,
                                   FileMode.Create,
                                   FileAccess.Write))
                        {
                            while (desplazamiento < largoArchivo)
                            {
                                int largoParte = parteActual == numPartes
                                    ? (int)(largoArchivo - desplazamiento)
                                    : Protocolo.MaxLargoParteArchivo;

                                Console.WriteLine(
                                    $"Recibiendo segmento #{parteActual}/{numPartes} de largo {largoParte}");

                                byte[] buffer = ndh.Receive(largoParte);
                                fsh.Escribir(buffer);

                                desplazamiento += largoParte;
                                parteActual++;
                            }
                        }
                    } 
                    catch (Exception)
                    {
                        File.Delete(nombreArchivo);
                    }

                    Console.WriteLine("Se recibió el archivo");
                }
                catch (SocketException)
                {
                    clienteConectado = false;
                    continue;
                }
            } while (clienteConectado);
            Console.WriteLine($"Se cerro la conexion del lado del cliente #{clienteId}");
            ndh.Disconnect();
        }
    }
}
