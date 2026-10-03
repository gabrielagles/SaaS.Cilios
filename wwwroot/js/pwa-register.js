if ("serviceWorker" in navigator) {
    window.addEventListener("load", () => {
        navigator.serviceWorker.register("/sw.js").catch(() => {
            // navegador não suporta ou falhou — o site continua funcionando normal, só sem cache offline
        });
    });
}
