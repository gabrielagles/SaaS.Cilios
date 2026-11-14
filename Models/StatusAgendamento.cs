namespace SaaS.Cilios.Models;

public enum StatusAgendamento
{
    Confirmado = 0,
    Cancelado = 1
}

public enum OrigemAgendamento
{
    Admin = 0,   // criado pela Rafaela no painel
    Online = 1   // criado pela cliente na página pública (próxima etapa)
}
