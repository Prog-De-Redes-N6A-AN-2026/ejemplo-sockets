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

    public byte[] Leer(int largo)
    {
        byte[] buffer = new byte[largo];
        int totalLeidos = 0;

        while (totalLeidos < largo)
        {
            int leidos = _stream.Read(buffer, totalLeidos, largo - totalLeidos);
            if (leidos == 0)
            {
                throw new Exception("No se pudo leer el archivo");
            }
            totalLeidos += leidos;
        }
        return buffer;
    }

    public void Escribir(byte[] buffer)
    {
        _stream.Write(buffer, 0, buffer.Length);
    }
}