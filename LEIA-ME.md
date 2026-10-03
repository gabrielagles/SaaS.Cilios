# Cílios Agenda — Pacote completo

Este zip substitui o projeto inteiro. Extraia e copie **o conteúdo da pasta**
(não a pasta em si) para dentro de `C:\Users\Guest\Downloads\SaaS.Cilios\SaaS.Cilios`
— pode sobrescrever tudo que já existir lá, exceto o arquivo `cilios.db`
se você já tiver cadastrado serviços/agendamentos que queira manter (se
não ligar de perder os dados de teste, pode apagar ele também, o sistema
recria na hora).

## 1. Rodar

```
cd C:\Users\Guest\Downloads\SaaS.Cilios\SaaS.Cilios
dotnet restore
dotnet ef migrations add AutenticacaoECampoOffline
dotnet run
```

> Se dotnet-ef reclamar que já existe uma migration com esse nome, troca
> por outro nome, tipo `dotnet ef migrations add V2`.

Acesse `http://localhost:5000` — vai cair direto na tela de login.

**Login padrão** (definido em `appsettings.json`, seção `AppLogin`):
- Usuário: `rafaela`
- Senha: `troque-esta-senha` — troque isso no arquivo antes de usar de verdade.

## 2. O que mudou nesse pacote

- **Login funcional** com cookie de autenticação — todas as telas do
  sistema agora exigem login (`[Authorize]` no Home, Servico e Agendamento).
- **Tema rosé aplicado no app inteiro** — não só nas telas novas, mas
  também no CRUD de Serviços/Agendamentos, navbar, botões, etc. Mesma
  paleta e fontes (Playfair Display + Alex Brush + Poppins).
- **Tela nova "Agenda do Dia"** (`/Agendamento/AgendaDia?data=2026-01-16`),
  igual ao print que você mandou: cards do dia com horário, cliente,
  serviço e valor, e lista dos outros dias do mês embaixo. Ela é
  acessada clicando em qualquer dia do calendário (`/Agendamento/Calendario`).
- **PWA instalável no iPhone** (veja o passo a passo abaixo).
- **Fila offline** para criar agendamentos sem internet, com
  sincronização automática quando a conexão volta.

## 3. Instalando no iPhone (adicionar à tela de início)

Isso só funciona com o site publicado em HTTPS de verdade (não em
`localhost`) — então esse passo é pra quando você já tiver feito o
deploy (Azure, Railway, uma VPS com domínio, etc.). Localmente dá pra
testar a interface, mas o "instalar como app" só funciona em produção.

1. Abra o site no **Safari** do iPhone (tem que ser Safari, não Chrome)
2. Toque no ícone de compartilhar (quadrado com seta pra cima)
3. Toque em **"Adicionar à Tela de Início"**
4. Pronto — vai aparecer um ícone com a coroa rosé, e abrindo por ele o
   site roda em tela cheia, sem a barra de endereço do navegador

## 4. Como funciona o modo offline (leia com atenção)

**O que funciona offline:**
- Todas as páginas que ela já abriu pelo menos uma vez com internet
  ficam salvas no celular e continuam abrindo mesmo sem sinal.
- O formulário de **Novo Agendamento** funciona offline: se não tiver
  internet na hora de salvar, o agendamento fica guardado no próprio
  celular (não se perde) e aparece um aviso rosé no topo avisando que
  está pendente de sincronizar.
- Assim que a internet volta (ou quando ela reabre o app), a
  sincronização acontece sozinha, sem precisar fazer nada.

**O que NÃO dá pra fazer (limitação real do iPhone/Safari, não é falha
do sistema):**
- O iOS não permite sincronizar em segundo plano com o app fechado —
  então a sincronização só roda enquanto o app está aberto na tela.
  Na prática isso quase não importa, porque ela vai abrir o app pra
  ver a agenda de qualquer forma.
- Editar ou excluir um agendamento offline ainda não está implementado
  (só criar um novo). Se quiser, a gente evolui isso depois.
- Páginas que ela nunca abriu (tipo se você adicionar uma tela nova no
  futuro) não vão estar no cache até serem abertas pelo menos uma vez
  com internet.

## 5. Onde mexer se quiser ajustar o visual

- `wwwroot/css/beauty-theme.css` → cores, fontes e todo o estilo rosé.
  As variáveis de cor estão no topo do arquivo (`--rose-500`, etc.) —
  mudando ali, muda em todo o app de uma vez.
- `Views/Agendamento/AgendaDia.cshtml` → a tela dos cards do dia
- `Views/Agendamento/Calendario.cshtml` → a tela do calendário mensal
- `Views/Account/Login.cshtml` → a tela de login

## 6. Avatar da Rafaela

Coloquei um círculo rosé com a letra "R" no lugar da foto (não gero foto
de pessoa real). Pra trocar por uma foto de verdade: coloque o arquivo
em `wwwroot/img/rafaela.jpg` e troque, nos arquivos `Calendario.cshtml`
e `AgendaDia.cshtml`, a linha:

```html
<div class="avatar-circle">R</div>
```

por:

```html
<img src="~/img/rafaela.jpg" class="avatar-circle" style="object-fit:cover;" />
```

## 7. Segurança (importante)

O login usa usuário/senha fixos em texto puro no `appsettings.json`. Pra
uso privado entre vocês duas tá tranquilo. Se algum dia for publicar
esse sistema num endereço público na internet, me chama antes — vale a
pena trocarmos por senha com hash (BCrypt) e HTTPS obrigatório.
