// ============================================================
// Cílios Agenda — fila offline de agendamentos
//
// Quando o formulário de "Novo Agendamento" é enviado sem internet,
// em vez de dar erro, ele guarda os dados no IndexedDB do próprio
// celular. Quando a internet volta (evento 'online') ou quando o app
// é reaberto, tenta reenviar tudo que estiver pendente pra
// /api/agendamentos.
//
// Importante: isso só sincroniza enquanto o app está ABERTO. O iOS
// Safari não permite sincronização em segundo plano com o app fechado.
// ============================================================

const DB_NAME = "ciliosOffline";
const STORE_NAME = "agendamentosPendentes";

function abrirBanco() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, 1);
        req.onupgradeneeded = () => {
            const db = req.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME, { keyPath: "chaveLocal", autoIncrement: true });
            }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

async function salvarPendente(dados) {
    const db = await abrirBanco();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        tx.objectStore(STORE_NAME).add(dados);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function listarPendentes() {
    const db = await abrirBanco();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readonly");
        const req = tx.objectStore(STORE_NAME).getAll();
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

async function removerPendente(chaveLocal) {
    const db = await abrirBanco();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        tx.objectStore(STORE_NAME).delete(chaveLocal);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function sincronizarPendentes() {
    if (!navigator.onLine) return;

    const pendentes = await listarPendentes();
    if (pendentes.length === 0) return;

    atualizarBadgePendentes(pendentes.length, true);

    for (const item of pendentes) {
        try {
            const resp = await fetch("/api/agendamentos", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "same-origin",
                body: JSON.stringify(item.dados)
            });

            if (resp.ok) {
                await removerPendente(item.chaveLocal);
            }
        } catch (e) {
            // sem rede ainda de verdade, ou servidor fora do ar — tenta de novo depois
            break;
        }
    }

    const restantes = await listarPendentes();
    atualizarBadgePendentes(restantes.length, false);
}

function atualizarBadgePendentes(qtd, sincronizando) {
    const banner = document.getElementById("offlineBanner");
    if (!banner) return;

    if (qtd > 0) {
        banner.classList.add("show");
        banner.textContent = sincronizando
            ? `Sincronizando ${qtd} agendamento(s) pendente(s)...`
            : `${qtd} agendamento(s) aguardando internet pra sincronizar`;
    } else if (!navigator.onLine) {
        banner.classList.add("show");
        banner.textContent = "Você está offline — as telas abertas continuam funcionando";
    } else {
        banner.classList.remove("show");
    }
}

// Intercepta o formulário de criação de agendamento (se existir na página)
function interceptarFormularioAgendamento() {
    const form = document.getElementById("formNovoAgendamento");
    if (!form) return;

    form.addEventListener("submit", async (event) => {
        if (navigator.onLine) return; // com internet, segue o fluxo normal (POST pro MVC)

        event.preventDefault();

        const dados = {
            clienteNome: form.querySelector("[name='ClienteNome']").value,
            clienteTelefone: form.querySelector("[name='ClienteTelefone']").value,
            servicoId: parseInt(form.querySelector("[name='ServicoId']").value, 10),
            dataHora: form.querySelector("[name='DataHora']").value,
            valor: parseFloat(form.querySelector("[name='Valor']").value.replace(",", ".")) || 0,
            concluido: form.querySelector("[name='Concluido']")?.checked || false
        };

        await salvarPendente({ dados });
        alert("Sem internet agora — o agendamento foi salvo no celular e será enviado automaticamente assim que a conexão voltar.");
        window.location.href = "/Agendamento";
    });
}

window.addEventListener("online", sincronizarPendentes);
document.addEventListener("DOMContentLoaded", () => {
    interceptarFormularioAgendamento();
    sincronizarPendentes();
    listarPendentes().then((p) => atualizarBadgePendentes(p.length, false));
});
