namespace Common;

public class FileStreamHelper : IDisposable
{
    private readonly FileStream _stream;

    public FileStreamHelper(string ruta, FileMode modo, FileAccess acceso)
    {
        _stream = new FileStream(ruta, modo, acceso);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async Task<byte[]> LeerAsync(int largo)
    {
        byte[] buffer = new byte[largo];
        int totalLeidos = 0;

        while (totalLeidos < largo)
        {
            int leidos = await _stream.ReadAsync(buffer, totalLeidos, largo - totalLeidos);
            if (leidos == 0)
            {
                throw new Exception("No se pudo leer el archivo");
            }

            totalLeidos += leidos;
        }

        return buffer;
    }

    public async Task EscribirAsync(byte[] buffer)
    {
        await _stream.WriteAsync(buffer, 0, buffer.Length);
    }
}
