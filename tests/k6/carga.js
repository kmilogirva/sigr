// Prueba de rendimiento de SIGR con k6: humo (1 usuario) y carga (hasta 20 usuarios virtuales).
// Uso: docker compose --profile performance run --rm k6
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  noCookiesReset: true,   // k6 borra las cookies entre iteraciones por defecto: aquí cada usuario virtual conserva su sesión
  scenarios: {
    humo: { executor: 'constant-vus', vus: 1, duration: '10s', exec: 'navegar' },
    carga: {
      executor: 'ramping-vus', startVUs: 0, exec: 'navegar', startTime: '12s',
      stages: [{ duration: '15s', target: 20 }, { duration: '20s', target: 20 }, { duration: '5s', target: 0 }],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],                          // menos de 1 % de errores
    'http_req_duration{tipo:lectura}': ['p(95)<500'],        // lecturas públicas: p95 menor a 500 ms
    'http_req_duration{tipo:login}': ['p(95)<1500'],         // el login es costoso a propósito (hash de la clave)
    checks: ['rate>0.99'],
  },
};

const jsonHeaders = { headers: { 'Content-Type': 'application/json' } };

export function navegar() {
  // Cada usuario virtual conserva su cookie entre iteraciones: se autentica solo la primera vez.
  if (__ITER === 0) {
    const r = http.post(`${BASE}/api/auth/login`,
      JSON.stringify({ email: 'admin@sigr.test', password: 'Sigr2026!' }), { ...jsonHeaders, tags: { tipo: 'login' } });
    check(r, { 'login 200': (x) => x.status === 200 });
  }

  const lista = http.get(`${BASE}/api/platos`, { tags: { tipo: 'lectura' } });
  check(lista, { 'platos 200': (x) => x.status === 200, 'hay platos': (x) => x.json().length > 0 });

  const detalle = http.get(`${BASE}/api/platos/${lista.json()[0].platoId}`, { tags: { tipo: 'lectura' } });
  check(detalle, { 'detalle 200': (x) => x.status === 200 });

  const reporte = http.get(`${BASE}/api/reportes/ventas?dias=7`, { tags: { tipo: 'lectura' } });
  check(reporte, { 'reporte 200': (x) => x.status === 200 });

  const menu = http.get(`${BASE}/menu`, { tags: { tipo: 'lectura' } });
  check(menu, { 'pagina menu 200': (x) => x.status === 200 });
  sleep(1);
}
