using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;
using Server.Domain;

namespace Server
{
    internal class Servidor
    {
        private static readonly int maxClientesPermitidos = 3;
        private static readonly object lockHilosClientes = new object();
        private static int clientesActuales = 0;
        private static readonly SettingsManager settingsManager = new SettingsManager();
        private static List<Usuario> usuarios = new List<Usuario>();
        private static readonly object lockUsuarios = new object();
        
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

            while (clientesActuales < maxClientesPermitidos)
            {
                Socket socketCliente = socketServer.Accept(); // bloqueante

                lock (lockHilosClientes)
                {
                    clientesActuales++;
                }

                int clienteId = clientesActuales;
                Thread t = new Thread(() => ManejarCliente(socketCliente, clienteId)); 
                t.Start();
            }

            socketServer.Close();
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
                    byte[] bufferTipoMensaje = ndh.Receive(Protocolo.LargoTipoMensaje);

                    byte[] bufferComando = ndh.Receive(Protocolo.LargoComando);
                    string comandoString = Encoding.UTF8.GetString(bufferComando);
                    Comando comando = (Comando)int.Parse(comandoString);

                    byte[] bufferLargoDatosComando = ndh.Receive(sizeof(int));
                    int largoDatosComando = BitConverter.ToInt32(bufferLargoDatosComando);
                    byte[] datosComando = ndh.Receive(largoDatosComando);

                    switch (comando)
                    {
                        case Comando.Registrar:
                            Registrar(ndh, datosComando);
                            break;
                        case Comando.IniciarSesion:
                            IniciarSesion(ndh, datosComando);
                            break;
                        case Comando.EnviarArchivo:
                            RecibirArchivo(ndh, datosComando);
                            break;
                        default:
                            break;
                    }
                }
                catch (SocketException)
                {
                    clienteConectado = false;
                }
                catch (Exception)
                {
                    Console.WriteLine($"El cliente #{clienteId} envió datos malformados");
                    clienteConectado = false;
                }
            } while (clienteConectado);

            Console.WriteLine($"Se cerro la conexion del lado del cliente #{clienteId}");
            ndh.Disconnect();

            lock (lockHilosClientes)
            {
                clientesActuales--;
            }
        }

        static void Registrar(NetworkDataHelper ndh, byte[] datos)
        {
            string datosString = Encoding.UTF8.GetString(datos);
            string[] usuarioYContrasena = datosString.Split('|');
            string nombreUsuario = usuarioYContrasena[0].Trim();
            string contrasena = usuarioYContrasena[1];

            bool agregado = false;

            lock (lockUsuarios)
            {
                bool existe = usuarios.Any(u => string.Equals(u.nombreUsuario, nombreUsuario));
                if (!existe)
                {
                    usuarios.Add(new Usuario(nombreUsuario, contrasena));
                    agregado = true;
                }
            }

            byte[] bufferResultado = BitConverter.GetBytes(agregado);
            ndh.Send(bufferResultado);
        }

        static void IniciarSesion(NetworkDataHelper ndh, byte[] datos)
        {
            string datosString = Encoding.UTF8.GetString(datos);
            string[] usuarioYContrasena = datosString.Split('|');
            string nombreUsuario = usuarioYContrasena[0].Trim();
            string contrasena = usuarioYContrasena[1];

            bool exitoso = false;

            lock (lockUsuarios)
            {
                Usuario? usuario = usuarios.FirstOrDefault(u => string.Equals(u.nombreUsuario, nombreUsuario));
                if (usuario != null)
                {
                    if (usuario.contrasena == contrasena)
                    {
                        exitoso = true;
                    }
                }
            }

            byte[] bufferResultado = BitConverter.GetBytes(exitoso);
            ndh.Send(bufferResultado);
        }

        static void RecibirArchivo(NetworkDataHelper ndh, byte[] datosNombreArchivo)
        {
            string nombreArchivo = Encoding.UTF8.GetString(datosNombreArchivo);

            byte[] bufferLargoArchivo = ndh.Receive(Protocolo.LargoDeLargoArchivo);
            long largoArchivo = BitConverter.ToInt64(bufferLargoArchivo);
            
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

                Console.WriteLine($"Se recibió el archivo {nombreArchivo}");
            }
            catch (SocketException)
            {
                File.Delete(nombreArchivo);
                throw;
            }
        }
    }
}
