"""Convierte una ejecución de GitHub Actions en una imagen para el informe.

Uso:  python tests/herramientas/figura_ci.py <id_de_ejecucion> <nombre_de_archivo_sin_extension>
Requiere la CLI de GitHub (gh) autenticada.
"""
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from generar_figuras import OUT, texto_a_imagen  # noqa: E402

run_id, nombre = sys.argv[1], sys.argv[2]
datos = json.loads(subprocess.run(
    ["gh", "run", "view", run_id, "--json", "jobs,displayTitle,headBranch,event,conclusion,createdAt"],
    capture_output=True, text=True, encoding="utf-8", check=True).stdout)

marca = {"success": "√", "failure": "x", "skipped": "-", "cancelled": "-"}
lineas = [f"$ gh run view {run_id}", "",
          f"CI · {datos['headBranch']} · {datos['event']}", f"Resultado: {datos['conclusion']}", ""]
for job in datos["jobs"]:
    lineas.append(f"{marca.get(job['conclusion'], '?')} {job['name']}  [{job['conclusion']}]")
    for paso in job["steps"]:
        if paso["name"].startswith(("Set up", "Post ", "Complete", "Run actions/")):
            continue
        lineas.append(f"    {marca.get(paso['conclusion'], '?')} {paso['name']}")
texto_a_imagen(lineas, OUT / f"{nombre}.png", f"GitHub Actions - ejecución {run_id}", ancho=1100)
