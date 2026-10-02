#!/usr/bin/env bash
# IdlePond — Unity en batch, sans ouvrir l'éditeur.
#
#   outils/unity.sh tests [EditMode|PlayMode] [filtre]
#   outils/unity.sh methode Espace.Classe.Methode
#   outils/unity.sh captures
#
# Unity refuse d'ouvrir en batch un projet déjà ouvert : fermer l'éditeur avant.
set -uo pipefail

UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe}"
RACINE="$(cd "$(dirname "$0")/.." && pwd)"
PROJET="$(cd "$RACINE" && pwd -W 2>/dev/null || echo "$RACINE")"
SORTIE="$RACINE/Logs/batch"
mkdir -p "$SORTIE"
JOURNAL="$SORTIE/unity.log"

erreurs_de_compilation() {
  if grep -q "another Unity instance is running\|It looks like another Unity instance" "$JOURNAL" 2>/dev/null; then
    echo "L'éditeur Unity a ce projet ouvert : ferme-le puis relance." >&2
  fi
  grep -E "error CS[0-9]+" "$JOURNAL" 2>/dev/null | sort -u >&2
}

case "${1:-}" in
  tests)
    PLATEFORME="${2:-EditMode}"
    FILTRE="${3:-}"
    RESULTATS="$SORTIE/resultats-$PLATEFORME.xml"
    rm -f "$RESULTATS"
    ARGS=(-batchmode -nographics -projectPath "$PROJET" -runTests -testPlatform "$PLATEFORME"
          -testResults "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/resultats-$PLATEFORME.xml"
          -logFile "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/unity.log")
    [ -n "$FILTRE" ] && ARGS+=(-testFilter "$FILTRE")
    "$UNITY" "${ARGS[@]}"
    if [ ! -f "$RESULTATS" ]; then
      echo "Aucun résultat : la compilation a probablement échoué." >&2
      erreurs_de_compilation
      exit 1
    fi
    python - "$RESULTATS" <<'PY'
import sys, xml.etree.ElementTree as ET
racine = ET.parse(sys.argv[1]).getroot()
print(f"total {racine.get('total')} · réussis {racine.get('passed')} · échoués {racine.get('failed')} · ignorés {racine.get('skipped')}")
echecs = 0
for cas in racine.iter('test-case'):
    if cas.get('result') == 'Failed':
        echecs += 1
        message = (cas.findtext('failure/message') or '').strip()
        print(f"ÉCHEC {cas.get('fullname')}\n    {message[:2000]}")
sys.exit(1 if echecs or racine.get('result', '').startswith('Failed') else 0)
PY
    ;;
  methode)
    "$UNITY" -batchmode -nographics -quit -projectPath "$PROJET" -executeMethod "$2" \
      -logFile "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/unity.log"
    CODE=$?
    [ $CODE -ne 0 ] && erreurs_de_compilation
    exit $CODE
    ;;
  captures)
    # Les captures de contrôle (spec DA §7) : PlayMode AVEC affichage, pas de -nographics.
    RESULTATS="$SORTIE/resultats-captures.xml"
    rm -f "$RESULTATS"
    IDLEPOND_CAPTURES=1 "$UNITY" -batchmode -projectPath "$PROJET" -runTests -testPlatform PlayMode \
      -testFilter CapturesDeControle \
      -testResults "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/resultats-captures.xml" \
      -logFile "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/unity.log"
    if [ ! -f "$RESULTATS" ]; then
      echo "Aucun résultat : la compilation a probablement échoué." >&2
      erreurs_de_compilation
      exit 1
    fi
    ls -1 "$RACINE/Logs/captures"
    ;;
  *)
    echo "usage : outils/unity.sh tests [EditMode|PlayMode] [filtre] | methode Espace.Classe.Methode | captures" >&2
    exit 2
    ;;
esac
