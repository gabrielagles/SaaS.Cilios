namespace SaaS.Cilios.Models;

public static class DuracaoHelper
{
    // 40 -> "40 min", 60 -> "1h", 90 -> "1h30", 125 -> "2h05"
    public static string Texto(int minutos)
    {
        if (minutos <= 0) return "—";
        if (minutos < 60) return $"{minutos} min";

        var horas = minutos / 60;
        var resto = minutos % 60;
        return resto == 0 ? $"{horas}h" : $"{horas}h{resto:00}";
    }
}
