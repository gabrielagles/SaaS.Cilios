namespace SaaS.Cilios.Models;

public static class WhatsAppHelper
{
    // Monta um link wa.me com a mensagem pronta. Devolve null se não houver telefone válido.
    public static string? Link(string? telefone, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(telefone)) return null;

        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (digitos.Length < 10) return null;
        if (digitos.Length <= 11) digitos = "55" + digitos;   // acrescenta o código do Brasil

        return $"https://wa.me/{digitos}?text={Uri.EscapeDataString(mensagem)}";
    }
}
