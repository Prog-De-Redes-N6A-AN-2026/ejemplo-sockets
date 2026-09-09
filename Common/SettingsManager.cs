using System.Configuration;

namespace Common;

public class SettingsManager
{
    public string LeerConfig(string clave)
    {
        try
        {
            var appSetting = ConfigurationManager.AppSettings;
            return appSetting[clave] ?? String.Empty;
        }
        catch (Exception)
        {
            Console.WriteLine($"No se encontro un valor para la clave {clave}");
            return String.Empty;
        }
    }
}