using Common;
using MySqlConnector;

namespace Server.Datos;

public class RepositorioUsuarios
{
    private readonly string connectionString;

    public RepositorioUsuarios(SettingsManager settings)
    {
        MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
        {
            Server = settings.LeerConfig(BaseDeDatosConfig.ClaveHost),
            Port = uint.Parse(settings.LeerConfig(BaseDeDatosConfig.ClavePuerto)),
            Database = settings.LeerConfig(BaseDeDatosConfig.ClaveNombre),
            UserID = settings.LeerConfig(BaseDeDatosConfig.ClaveUsuario),
            Password = settings.LeerConfig(BaseDeDatosConfig.ClaveContrasena),
        };
        connectionString = builder.ConnectionString;
    }

    public async Task<bool> RegistrarAsync(string usuario, string contrasena)
    {
        await using MySqlConnection conexion = new MySqlConnection(connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = conexion.CreateCommand();
        comando.CommandText =
            "INSERT INTO usuarios (nombre_usuario, contrasena) VALUES (@nombreUsuario, @contrasena)";
        comando.Parameters.AddWithValue("@nombreUsuario", usuario);
        comando.Parameters.AddWithValue("@contrasena", contrasena);

        try
        {
            return await comando.ExecuteNonQueryAsync() == 1;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return false;
        }
    }
    
    public async Task<bool> LoginAsync(string usuario, string contrasena)
    {
        await using MySqlConnection conexion = new MySqlConnection(connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = conexion.CreateCommand();
        comando.CommandText =
            "SELECT EXISTS(SELECT 1 FROM usuarios WHERE nombre_usuario = @nombreUsuario AND contrasena = @contrasena)";
        comando.Parameters.AddWithValue("@nombreUsuario", usuario);
        comando.Parameters.AddWithValue("@contrasena", contrasena);

        object? resultado = await comando.ExecuteScalarAsync();
        return Convert.ToInt32(resultado) == 1;
    }
}