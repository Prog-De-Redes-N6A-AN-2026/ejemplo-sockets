using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Cliente
{
    internal class Cliente
    {
        private static readonly SettingsManager settingsManager = new SettingsManager();
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();

        static void ImprimirMenu()
        {
            Console.WriteLine("MENÚ DE OPCIONES");
            Console.WriteLine("1. Registrarse");
            Console.WriteLine("2. Iniciar sesión");
            Console.WriteLine("3. Enviar archivo");
            Console.WriteLine("4. Salir");
            Console.Write("Seleccione una opción: ");
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Empezando cliente!");

            IPAddress ipCliente = IPAddress.Parse(settingsManager.LeerConfig(ClienteConfig.ClaveIpCliente));
            int puertoCliente = int.Parse(settingsManager.LeerConfig(ClienteConfig.ClavePuertoCliente));

            IPEndPoint endpointLocal = new IPEndPoint(ipCliente, puertoCliente);
            TcpClient clienteTcp = new TcpClient(endpointLocal);

            string hostServidor = settingsManager.LeerConfig(ServidorConfig.ClaveHostServidor);
            int puertoServidor = int.Parse(settingsManager.LeerConfig(ServidorConfig.ClavePuertoServidor));

            Console.CancelKeyPress += ManejarCancelacion;

            try
            {
                Task tareaConexion = clienteTcp.ConnectAsync(hostServidor, puertoServidor);
                await tareaConexion.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Se canceló la conexión");
                return;
            }
            catch (Exception)
            {
                Console.WriteLine("No se pudo conectar al servidor");
                return;
            }

            Console.CancelKeyPress -= ManejarCancelacion;

            Console.WriteLine("Conectado al servidor!");

            NetworkDataHelper ndh = new NetworkDataHelper(clienteTcp);

            bool salir = false;
            while (!salir)
            {
                ImprimirMenu();

                string? comandoString = Console.ReadLine();
                int numeroComando;

                try
                {
                    numeroComando = int.Parse(comandoString!) - 1;
                }
                catch (Exception)
                {
                    Console.WriteLine("Formato de comando inválido");
                    continue;
                }

                Comando comando = (Comando)numeroComando;
                if (comando == Comando.Salir)
                {
                    break;
                }

                byte[] tipoMensaje = Encoding.UTF8.GetBytes(Protocolo.Solicitud);
                string comandoStringDatos = numeroComando.ToString("D2");
                byte[] bufferComando = Encoding.UTF8.GetBytes(comandoStringDatos);

                try
                {
                    await ndh.SendAsync(tipoMensaje);
                    await ndh.SendAsync(bufferComando);
                }
                catch (SocketException)
                {
                    salir = true;
                    break;
                }

                switch (comando)
                {
                    case Comando.Registrar:
                        salir = !await RegistrarAsync(ndh);
                        break;
                    case Comando.IniciarSesion:
                        salir = !await IniciarSesionAsync(ndh);
                        break;
                    case Comando.EnviarArchivo:
                        salir = !await EnviarArchivoAsync(ndh);
                        break;
                    default:
                        Console.WriteLine("Opción inválida");
                        break;
                }
            }

            Console.WriteLine("Se cierra la conexion...");
            ndh.Disconnect();
        }

        internal static async Task<bool> RegistrarAsync(NetworkDataHelper ndh)
        {
            Console.Write("Nombre de usuario: ");
            string? nombreUsuario = Console.ReadLine();
            if (nombreUsuario == null)
            {
                Console.WriteLine("El nombre de usuario no puede ser null");
                return true;
            }

            Console.Write("Contraseña: ");
            string? contrasena = Console.ReadLine();
            if (contrasena == null)
            {
                Console.WriteLine("La contraseña no puede ser null");
                return true;
            }

            string usuarioYContrasena = nombreUsuario + "|" + contrasena;
            byte[] datosComando = Encoding.UTF8.GetBytes(usuarioYContrasena);
            int largoDatosComando = datosComando.Length;
            byte[] bufferLargoDatosComando = BitConverter.GetBytes(largoDatosComando);

            try
            {
                await ndh.SendAsync(bufferLargoDatosComando);
                await ndh.SendAsync(datosComando);

                byte[] resultado = await ndh.ReceiveAsync(1);
                bool registrado = BitConverter.ToBoolean(resultado);

                if (registrado)
                {
                    Console.WriteLine("Usuario registrado correctamente...");
                }
                else
                {
                    Console.WriteLine("No se pudo registrar, el nombre de usuario ya existe...");
                }

                return true;
            }
            catch (SocketException)
            {
                Console.WriteLine("Conexión interrumpida");
                return false;
            }
        }

        internal static async Task<bool> IniciarSesionAsync(NetworkDataHelper ndh)
        {
            Console.Write("Nombre de usuario: ");
            string? nombreUsuario = Console.ReadLine();
            if (nombreUsuario == null)
            {
                Console.WriteLine("El nombre de usuario no puede ser null");
                return true;
            }

            Console.Write("Contraseña: ");
            string? contrasena = Console.ReadLine();
            if (contrasena == null)
            {
                Console.WriteLine("La contraseña no puede ser null");
                return true;
            }

            string usuarioYContrasena = nombreUsuario + "|" + contrasena;
            byte[] datosComando = Encoding.UTF8.GetBytes(usuarioYContrasena);
            int largoDatosComando = datosComando.Length;
            byte[] bufferLargoDatosComando = BitConverter.GetBytes(largoDatosComando);

            try
            {
                await ndh.SendAsync(bufferLargoDatosComando);
                await ndh.SendAsync(datosComando);

                byte[] resultado = await ndh.ReceiveAsync(1);
                bool sesionIniciada = BitConverter.ToBoolean(resultado);

                if (sesionIniciada)
                {
                    Console.WriteLine("Sesión iniciada...");
                }
                else
                {
                    Console.WriteLine("No se pudo iniciar sesión...");
                }

                return true;
            }
            catch (SocketException)
            {
                Console.WriteLine("Conexión interrumpida");
                return false;
            }
        }

        internal static async Task<bool> EnviarArchivoAsync(NetworkDataHelper ndh)
        {
            try
            {
                Console.Write("Ingresar ruta del archivo: ");
                string? ruta = Console.ReadLine();
                if (ruta == null)
                {
                    Console.WriteLine("La ruta no puede ser null");
                    return true;
                }

                ruta = ruta.Trim().Trim('"');

                FileInfo info = new FileInfo(ruta);
                if (!info.Exists)
                {
                    Console.WriteLine("El archivo no existe");
                    return true;
                }

                string nombreArchivo = info.Name;
                byte[] bufferNombreArchivo = Encoding.UTF8.GetBytes(nombreArchivo);
                int largoNombreArchivo = bufferNombreArchivo.Length;
                byte[] bufferLargoNombreArchivo = BitConverter.GetBytes(largoNombreArchivo);

                await ndh.SendAsync(bufferLargoNombreArchivo);
                await ndh.SendAsync(bufferNombreArchivo);

                long largoArchivo = info.Length;
                byte[] bufferLargoArchivo = BitConverter.GetBytes(largoArchivo);
                long numPartes = Protocolo.CalcularCantidadDePartes(largoArchivo);
                long desplazamiento = 0;
                long parteActual = 1;

                await ndh.SendAsync(bufferLargoArchivo);

                using (FileStreamHelper fsh = new FileStreamHelper(ruta, FileMode.Open, FileAccess.Read))
                {
                    while (desplazamiento < largoArchivo)
                    {
                        int largoParte = parteActual == numPartes
                            ? (int)(largoArchivo - desplazamiento)
                            : Protocolo.MaxLargoParteArchivo;

                        Console.WriteLine($"Enviando segmento #{parteActual}/{numPartes} de largo {largoParte}");

                        byte[] buffer = await fsh.LeerAsync(largoParte);
                        await ndh.SendAsync(buffer);

                        desplazamiento += largoParte;
                        parteActual++;
                    }
                }

                Console.WriteLine("Se envió el archivo...");
                return true;
            }
            catch (SocketException)
            {
                Console.WriteLine("Conexión interrumpida");
                return false;
            }
        }

        internal static void ManejarCancelacion(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Console.WriteLine("Cancelación solicitada...");
            cts.Cancel();
        }
    }
}
