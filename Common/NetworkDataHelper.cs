namespace Common;
using System.Net.Sockets;

public class NetworkDataHelper
{
    private readonly Socket _socket;

    public NetworkDataHelper(Socket socket)
    {
        _socket = socket;
    }
    
    public byte[] Receive(int largo)
    {
        byte[] buffer = new byte[largo];
        int desplazamiento = 0;
        
        while (desplazamiento < largo)
        {
            int recibidos = _socket.Receive(buffer, desplazamiento, largo - desplazamiento, SocketFlags.None);
            if (recibidos == 0)
            {
                throw new SocketException();
            }
            desplazamiento += recibidos;
        }
        
        return buffer;
    }

    public void Send(byte[] buffer)
    {
        int largo = buffer.Length;
        int desplazamiento = 0;
        
        while (desplazamiento < largo)
        {
            int enviados = _socket.Send(buffer, desplazamiento, largo - desplazamiento, SocketFlags.None);
            if (enviados == 0)
            {
                throw new SocketException();
            }
            desplazamiento += enviados;
        }
    }
    
    public void Disconnect()
    {
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
    }
}