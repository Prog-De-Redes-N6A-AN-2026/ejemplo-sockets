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

        static async Task Main(string[] args)
        {
            Console.WriteLine("Empezando servidor!");

            IPAddress ipServidor = IPAddress.Parse(settingsManager.LeerConfig(ServidorConfig.ClaveIpServidor));
            int puertoServidor = int.Parse(settingsManager.LeerConfig(ServidorConfig.ClavePuertoServidor));

            TcpListener listener = new TcpListener(ipServidor, puertoServidor);
            listener.Start(10); // escuchamos conexiones

            Console.WriteLine("Esperando a que se conecten clientes...");

            while (clientesActuales < maxClientesPermitidos)
            {
                TcpClient clienteTcp = await listener.AcceptTcpClientAsync();

                lock (lockHilosClientes)
                {
                    clientesActuales++;
                }

                int clienteId = clientesActuales;
                _ = ManejarClienteAsync(clienteTcp, clienteId);
            }

            listener.Stop();
        }

        static async Task ManejarClienteAsync(TcpClient clienteTcp, int clienteId)
        {
            Console.WriteLine($"Se conectó el cliente #{clienteId}!");
            NetworkDataHelper ndh = new NetworkDataHelper(clienteTcp);
            bool clienteConectado = true;

            do
            {
                try
                {
                    byte[] bufferTipoMensaje = await ndh.ReceiveAsync(Protocolo.LargoTipoMensaje);

                    byte[] bufferComando = await ndh.ReceiveAsync(Protocolo.LargoComando);
                    string comandoString = Encoding.UTF8.GetString(bufferComando);
                    Comando comando = (Comando)int.Parse(comandoString);

                    byte[] bufferLargoDatosComando = await ndh.ReceiveAsync(sizeof(int));
                    int largoDatosComando = BitConverter.ToInt32(bufferLargoDatosComando);
                    byte[] datosComando = await ndh.ReceiveAsync(largoDatosComando);

                    switch (comando)
                    {
                        case Comando.Registrar:
                            await RegistrarAsync(ndh, datosComando);
                            break;
                        case Comando.IniciarSesion:
                            await IniciarSesionAsync(ndh, datosComando);
                            break;
                        case Comando.EnviarArchivo:
                            await RecibirArchivoAsync(ndh, datosComando);
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

        static async Task RegistrarAsync(NetworkDataHelper ndh, byte[] datos)
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
            await ndh.SendAsync(bufferResultado);
        }

        static async Task IniciarSesionAsync(NetworkDataHelper ndh, byte[] datos)
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
            await ndh.SendAsync(bufferResultado);
        }

        static async Task RecibirArchivoAsync(NetworkDataHelper ndh, byte[] datosNombreArchivo)
        {
            string nombreArchivo = Encoding.UTF8.GetString(datosNombreArchivo);

            byte[] bufferLargoArchivo = await ndh.ReceiveAsync(Protocolo.LargoDeLargoArchivo);
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

                        byte[] buffer = await ndh.ReceiveAsync(largoParte);
                        await fsh.EscribirAsync(buffer);

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
