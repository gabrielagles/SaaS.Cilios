// Service Worker — Cílios Agenda
const CACHE_VERSION = "cilios-v2"; // <- suba esse número toda vez que quiser forçar atualização
const APP_SHELL = [
    "/css/beauty-theme.css",
    "/css/site.css",
    "/manifest.json",
    "/offline.html",
    "/icons/icon-192.png",
    "/icons/icon-512.png",
    "/js/offline-sync.js"
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(CACHE_VERSION).then((cache) => cache.addAll(APP_SHELL))
    );
    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys.filter((k) => k !== CACHE_VERSION).map((k) => caches.delete(k)))
        )
    );
    self.clients.claim();
});

self.addEventListener("fetch", (event) => {
    const req = event.request;
    if (req.method !== "GET") return;

    // Rede primeiro (pega sempre a versão mais nova). Só usa cache se estiver offline.
    event.respondWith(
        fetch(req)
            .then((res) => {
                const clone = res.clone();
                caches.open(CACHE_VERSION).then((cache) => cache.put(req, clone));
                return res;
            })
            .catch(() =>
                caches.match(req).then((cached) => {
                    if (cached) return cached;
                    if (req.mode === "navigate") return caches.match("/offline.html");
                })
            )
    );
});