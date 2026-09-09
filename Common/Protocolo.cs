namespace Common;

public class Protocolo
{
    public const int MaxLargoParteArchivo = 1024; // 1K
    public const int LargoDeLargoNombreArchivo = sizeof(int);
    public const int LargoDeLargoArchivo = sizeof(long);

    public static long CalcularCantidadDePartes(long largoArchivo)
    {
        long numPartes = largoArchivo / MaxLargoParteArchivo;
        return numPartes * MaxLargoParteArchivo == largoArchivo ? numPartes : numPartes + 1;
    }
}