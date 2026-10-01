namespace Common;
using System.Net.Sockets;

public class NetworkDataHelper
{
    private readonly TcpClient _cliente;
    private readonly NetworkStream _stream;

    public NetworkDataHelper(TcpClient cliente)
    {
        _cliente = cliente;
        _stream = cliente.GetStream();
    }

    public async Task<byte[]> ReceiveAsync(int largo)
    {
        byte[] buffer = new byte[largo];
        int desplazamiento = 0;

        while (desplazamiento < largo)
        {
            int recibidos = await _stream.ReadAsync(
                buffer,
                desplazamiento,
                largo - desplazamiento
            );

            if (recibidos == 0)
            {
                throw new SocketException();
            }

            desplazamiento += recibidos;
        }

        return buffer;
    }

    public async Task SendAsync(byte[] buffer)
    {
        await _stream.WriteAsync(buffer, 0, buffer.Length);
    }

    public void Disconnect()
    {
        _stream.Close();
        _cliente.Close();
    }
}
