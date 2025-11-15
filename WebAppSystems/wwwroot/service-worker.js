const CACHE_NAME = "meu-app-cache-v1";
const URLS_TO_CACHE = [
    "/",              // página inicial
    "/index.html",    // seu HTML principal
    "/css/site.css",  // exemplo de CSS
    "/js/site.js",    // exemplo de JS
    "/icon-192.png",  // ícone
    "/icon-512.png"   // ícone maior
];

// Instala e adiciona arquivos ao cache
self.addEventListener("install", (event) => {
    console.log("[ServiceWorker] Instalando...");
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            console.log("[ServiceWorker] Arquivos em cache");
            return cache.addAll(URLS_TO_CACHE);
        })
    );
});

// Ativa e limpa caches antigos
self.addEventListener("activate", (event) => {
    console.log("[ServiceWorker] Ativado");
    event.waitUntil(
        caches.keys().then((cacheNames) => {
            return Promise.all(
                cacheNames.map((name) => {
                    if (name !== CACHE_NAME) {
                        console.log("[ServiceWorker] Removendo cache antigo:", name);
                        return caches.delete(name);
                    }
                })
            );
        })
    );
});

// Intercepta requisições
self.addEventListener("fetch", (event) => {
    event.respondWith(
        caches.match(event.request).then((response) => {
            // Se encontrou no cache, retorna
            if (response) {
                console.log("[ServiceWorker] Servindo do cache:", event.request.url);
                return response;
            }
            // Se não encontrou, busca na rede
            console.log("[ServiceWorker] Buscando na rede:", event.request.url);
            return fetch(event.request);
        })
    );
});
