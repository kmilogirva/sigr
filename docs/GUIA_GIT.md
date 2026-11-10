# Guía de trabajo con Git — SIGR (camino a la línea base)

## 1. Modelo de ramas

```
main      ●────────────────────────●───────────────●──── v0.1.0-baseline
           \                      / \             /
develop     ●──●──────●──────●───●───●──●───●────●
               \     /\     /    
feature/*       ●──●  ●──●        (una rama por módulo/entrega)
```

| Rama | Propósito | Quién integra |
|---|---|---|
| `main` | Estable. Solo recibe merges desde `develop` vía PR. Aquí vive la línea base. | Coordinador |
| `develop` | Integración continua del equipo. | Cualquiera vía PR |
| `feature/<modulo>` | Una entrega granular. Sale de `develop`, vuelve a `develop`. | Autor |
| `docs/<tema>` | Documentación | Autor |
| `release/0.1.0` | Congelamiento previo a la línea base (solo correcciones) | Coordinador |

**Convención de commits** (Conventional Commits): `feat:`, `fix:`, `docs:`, `chore:`, `test:`, `refactor:`.
Ejemplo: `feat(menu): agrega CRUD de platos`.

## 2. Plan de entregas (ramas → PR → merge)

| # | Rama | Contenido | Issue |
|---|---|---|---|
| 0 | `main` (inicial) | .gitignore, solución Blazor vacía, README | #1 |
| 1 | `docs/base-de-datos` | `database/01_schema.sql`, `02_seed.sql`, diagrama | #2 |
| 2 | `feature/acceso-datos` | Cadena de conexión, capa de datos (EF Core/Dapper) | #3 |
| 3 | `feature/autenticacion` | Login, roles Cliente/Mesero/Admin | #4 |
| 4 | `feature/menu-digital` | CRUD platos y categorías | #5 |
| 5 | `feature/pedidos` | Registro y seguimiento de pedidos | #6 |
| 6 | `feature/reservas` | Reservas por fecha/hora | #7 |
| 7 | `feature/caja-reportes` | Cierre de caja y ventas diarias | #8 |
| 8 | `docs/linea-base` | CHANGELOG, LICENSE, manual de despliegue | #9 |
| 9 | `release/0.1.0` | Pruebas de integración y tag | #10 |

## 3. Comandos paso a paso

### 3.1 Inicializar y publicar (una sola vez)
```bash
cd "Proyecto Restaurante Blazor"
git init -b main
git config user.name  "Tu Nombre"
git config user.email "tu@correo"
git add .gitignore SIGR.sln src README.md
git commit -m "chore: estructura inicial de solución Blazor"
git remote add origin https://github.com/<usuario>/sigr.git
git push -u origin main            # PUSH #1
git switch -c develop
git push -u origin develop
```

### 3.2 Ciclo de cada entrega (repetir por rama)
```bash
git switch develop
git pull origin develop                       # PULL: actualizarse
git switch -c feature/menu-digital            # nueva rama

# ... trabajar ...
git status
git add src/SIGR.Web/Components/Pages/Platos.razor
git commit -m "feat(menu): listado de platos"
git add .
git commit -m "feat(menu): formulario crear/editar plato"
git commit -am "fix(menu): valida precio no negativo"

git push -u origin feature/menu-digital       # PUSH de la rama
# En GitHub: abrir Pull Request  feature/menu-digital → develop
#   (descripción: "Closes #5"), pedir revisión, aprobar y "Squash/Merge".

git switch develop
git pull origin develop                       # PULL con el merge ya integrado
git branch -d feature/menu-digital            # limpiar local
```

### 3.3 Conflictos (documentar al menos uno para el taller)
```bash
git switch feature/reservas
git fetch origin
git merge origin/develop          # o: git rebase origin/develop
# editar archivos marcados con <<<<<<< ======= >>>>>>>
git add <archivo>
git commit -m "merge: resuelve conflicto en Program.cs"
```

### 3.4 Congelar y establecer la línea base
```bash
git switch develop && git pull
git switch -c release/0.1.0
# solo correcciones menores + actualizar CHANGELOG.md
git commit -am "docs: CHANGELOG de la línea base 0.1.0"
git push -u origin release/0.1.0
# PR release/0.1.0 → main (aprobado por el coordinador)

git switch main && git pull
git tag -a v0.1.0-baseline -m "Línea base SIGR 0.1.0"
git push origin v0.1.0-baseline
git log --oneline -1               # ← este es el hash que citas en 4.3 (en vez de a93b4f1)
git switch develop && git merge main && git push   # sincronizar
```

### 3.5 Comandos útiles para evidenciar en el PDF
```bash
git log --oneline --graph --all --decorate
git branch -a
git shortlog -sn
git diff --stat main..develop
git show v0.1.0-baseline
```
> Toma **capturas de pantalla** de: `git log --graph`, los PR en GitHub, los Issues cerrados y el tag/release.

## 4. Reglas del equipo
- Nada se commitea directo en `main` ni `develop` (proteger en *Settings → Branches*: requerir PR y 1 aprobación).
- Commits pequeños y atómicos; un PR = una funcionalidad.
- Siempre `git pull` antes de crear rama o hacer `push`.
- Cada PR referencia su Issue (`Closes #N`).
- Nunca subir contraseñas reales: usar `dotnet user-secrets` para la cadena de conexión.
