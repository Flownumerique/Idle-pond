# Préséance des documents

Écrit le 2026-09-08. Cette page existe parce que trois sessions ont re-dérivé
la même question et ont conclu différemment.

| Domaine | Document directif |
|---|---|
| Mécanique, économie, courbe, arbre technique, état | `docs/idlepond-noyau-v1_0.md` |
| **Fiction, monde, arc du héros, lexique** | **`docs/CODEX.md`** |
| Chiffres — amende le noyau sur ses cinq findings | `docs/RESULTATS.md` |
| Systèmes conçus mais jamais construits | `docs/GDD.md` v2.4 |
| `θ`, loi de contenance, là où il ne contredit pas le noyau | `docs/amendement-v1.1.md` |
| **Périmé** | `docs/jalon-v0.1.md` §5.1, qui donnait la préséance au prompt de lancement |

**Règle de lecture.** Quand le GDD v2.4 décrit une mécanique que le noyau v1.0
a tuée, le noyau gagne. Quand le GDD et le Codex se contredisent sur un nom,
un mot ou un point de fiction, **le Codex gagne**. Quand le noyau v1.0 est muet
sur un système que seul le GDD décrit, le GDD tient — c'est sa fonction
restante. Quand `RESULTATS.md` mesure un chiffre que le noyau v1.0 avait posé
en graine, la mesure gagne.

**Sections du GDD dépassées par le noyau v1.0** : §3 la captation, §7 le
vivant, §10 la ponte, §16 l'équilibrage. Le reste du GDD tient.

**Ajouté le 2026-09-17.** GDD §4.2 (« la Foi n'achète que des miracles ») est
dépassé par le noyau v1.0 §4 : la Foi achète des **bénédictions**, permanentes,
qui montent la production. Les miracles restent gelés (`[P26]`). Le noyau v1.0
§1.2 compte désormais **quatre** achats — voir `docs/amendement-v1.1.md`, v1.4.

**Ajouté le 2026-09-18 — le Codex prend la fiction et le lexique.**
`docs/CODEX.md` v1.0 absorbe `idlepond-histoire-v0.1.md` (archivé dans
`docs/archive/`) et reprend au GDD l'autorité sur la fiction, le monde, l'arc
et **le vocabulaire** — à l'écran comme dans les identifiants. Le GDD garde ce
qu'il est seul à porter : les systèmes conçus et jamais construits (temples,
alliés, portails, miracles, défiscalisation), qui sont la matière de
`docs/ROADMAP.md`.

Trois renommages de fond en découlent, et ils touchent les identifiants et les
sauvegardes : la Foi devient **le Souffle** (une énergie distincte du mana, non
une émotion du peuple), les bénédictions deviennent **les insufflations**, et
la ponte devient **la renaissance**. Le §10 du GDD, déjà dépassé sur la
mécanique, l'est désormais aussi sur le nom. Le chantier de renommage est le
n°1 de la roadmap ; tant qu'il n'est pas fait, le code porte l'ancien
vocabulaire et le Codex fait foi sur le nouveau.

**Contradiction interne au noyau v1.0, tranchée.** Sa §2 place
`mult_technique` dans la production totale et sa §6.2 donne au nœud
« Réputation » un `+5 % de production`, contre sa propre §6.3 — « la technique
baisse les coûts, la bénédiction monte la production, aucun nœud ne franchit
cette ligne ». La règle dure gagne : `mult_technique` ne figure pas dans la
production, et « Réputation » est à réécrire en réduction de coût.

La justification longue est dans
`docs/superpowers/specs/2026-09-08-roadmap-noyau-v1-design.md`.
