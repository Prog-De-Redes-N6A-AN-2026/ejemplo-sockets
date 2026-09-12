using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Cliente
{
    internal class Cliente
    {
        private static readonly SettingsManager settingsManager = new SettingsManager();

        static void ImprimirMenu()
        {
            Console.WriteLine("MENÚ DE OPCIONES");
            Console.WriteLine("1. Registrarse");
            Console.WriteLine("2. Iniciar sesión");
            Console.WriteLine("3. Enviar archivo");
            Console.WriteLine("4. Salir");
            Console.Write("Seleccione una opción: ");
        }
        
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

            try
            {
                socketCliente.Connect(endpointServidor);
            }
            catch (Exception)
            {
                Console.WriteLine("No se pudo conectar al servidor");
                return;
            }

            Console.WriteLine("Conectado al servidor!");
            
            NetworkDataHelper ndh = new NetworkDataHelper(socketCliente);

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
                    ndh.Send(tipoMensaje);
                    ndh.Send(bufferComando);
                }
                catch (SocketException)
                {
                    salir = true;
                    break;
                }

                switch (comando)
                {
                    case Comando.Registrar:
                        salir = !Registrar(ndh);
                        break;
                    case Comando.IniciarSesion:
                        salir = !IniciarSesion(ndh);
                        break;
                    case Comando.EnviarArchivo:
                        salir = !EnviarArchivo(ndh);
                        break;
                    default:
                        Console.WriteLine("Opción inválida");
                        break;
                }
            }

            Console.WriteLine("Se cierra la conexion...");
            ndh.Disconnect();
        }

        internal static bool Registrar(NetworkDataHelper ndh)
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
                ndh.Send(bufferLargoDatosComando);
                ndh.Send(datosComando);

                byte[] resultado = ndh.Receive(1);
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

        internal static bool IniciarSesion(NetworkDataHelper ndh)
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
                ndh.Send(bufferLargoDatosComando);
                ndh.Send(datosComando);

                byte[] resultado = ndh.Receive(1);
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

        internal static bool EnviarArchivo(NetworkDataHelper ndh)
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

                Console.WriteLine("Se envió el archivo...");
                return true;
            }
            catch (SocketException)
            {
                Console.WriteLine("Conexión interrumpida");
                return false;
            }
        }
    }
}
