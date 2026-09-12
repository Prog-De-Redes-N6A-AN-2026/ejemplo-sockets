namespace Server.Domain;

public class Usuario
{
    public string nombreUsuario;
    public string contrasena;

    public Usuario(string nombreUsuario, string contrasena)
    {
        this.nombreUsuario = nombreUsuario;
        this.contrasena = contrasena;
    }
}
