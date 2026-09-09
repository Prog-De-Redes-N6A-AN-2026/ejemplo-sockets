using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Cliente
{
    internal class Cliente
    {
        private static readonly SettingsManager settingsManager = new SettingsManager();
        
        static void Main(string[] args)
        {
            Console.WriteLine("Empezando cliente!");

            Socket socketCliente = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            
            IPAddress ipCliente = IPAddress.Parse(settingsManager.LeerConfig(ClienteConfig.ClaveIpCliente));
            int puertoCliente = int.Parse(settingsManager.LeerConfig(ClienteConfig.ClavePuertoCliente));
            
            IPEndPoint endpointLocal = new IPEndPoint(ipCliente, puertoCliente);
            socketCliente.Bind(endpointLocal);
            
            IPAddress ipServidor = IPAddress.Parse(settingsManager.LeerConfig(ServidorConfig.ClaveIpServidor));
            int puertoServidor = int.Parse(settingsManager.LeerConfig(ServidorConfig.ClavePuertoServidor));
            
            IPEndPoint endpointServidor = new IPEndPoint(ipServidor, puertoServidor);
            socketCliente.Connect(endpointServidor);
            Console.WriteLine("Conectado al servidor!");
            
            NetworkDataHelper ndh = new NetworkDataHelper(socketCliente);

            bool salir = false;
            while (!salir)
            {
                Console.Write("Ingresar ruta del archivo: ");
                string ruta = Console.ReadLine();
                
                FileInfo info = new FileInfo(ruta);
                string nombreArchivo = info.Name;
                byte[] bufferNombreArchivo = Encoding.UTF8.GetBytes(nombreArchivo);
                int largoNombreArchivo = bufferNombreArchivo.Length;
                byte[] bufferLargoNombreArchivo = BitConverter.GetBytes(largoNombreArchivo);
                ndh.Send(bufferLargoNombreArchivo);
                ndh.Send(bufferNombreArchivo);

                long largoArchivo = info.Length;
                byte[] bufferLargoArchivo = BitConverter.GetBytes(largoArchivo);
                long numPartes = Protocolo.CalcularCantidadDePartes(largoArchivo);
                long desplazamiento = 0;
                long parteActual = 1;
                
                ndh.Send(bufferLargoArchivo);

                using (FileStreamHelper fsh = new FileStreamHelper(ruta, FileMode.Open, FileAccess.Read))
                {
                    while (desplazamiento < largoArchivo)
                    {
                        int largoParte = parteActual == numPartes
                            ? (int)(largoArchivo - desplazamiento)
                            : Protocolo.MaxLargoParteArchivo;
                        Console.WriteLine($"Enviando segmento #{parteActual}/{numPartes} de largo {largoParte}");
                        byte[] buffer = fsh.Leer(largoParte);
                        ndh.Send(buffer);
                        
                        desplazamiento += largoParte;
                        parteActual++;
                    }
                }
            }
            Console.WriteLine("Se cierra la conexion...");
            ndh.Disconnect();
        }
    }
}
