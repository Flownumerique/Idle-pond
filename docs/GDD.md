IdlePond — Game Design Document

Version : 2.4 — 2026-08-24 Tier : 2 (local au jeu). Tout élément réutilisable devra être promu en Tier 1 par écrit. Dépendances, vérifiées fichier par fichier au 2026-08-24 : tier-0-immuable.md v0.14, mana-typologie.md v0.9, especes-cadre.md v0.5, dieux-et-benedictions.md v0.10, idlepond-tier-2.md v0.1 — périmé, voir ci-dessous. (Les versions sont désormais inscrites. L'en-tête sans numéros de la v2.1 avait produit exactement le défaut qu'il prétendait éviter : une affirmation d'alignement invérifiable.)

Alignement — correction. La v2.1 déclarait s'aligner sur un idlepond-tier-2.md v0.8 qui n'existe pas au dépôt. La copie réelle est en v0.1 : elle porte encore trois paliers de voix, dépend d'un Tier 0 v0.10 alors qu'il est en v0.14, et classe encore le guide en condensat de lieu alors que dieux-et-benedictions.md v0.6 l'a reclassé en condensat d'intention instinctive. Le §13 du présent document est correct ; c'est le Tier 2 qui doit être mis à jour, et non l'inverse. Tâche ouverte, voir §19.5.

Remplace : GDD_Etang_des_Merveilles_v2.0, ZONES_ET_BIOMES_v4, POISSONS.md, BIOMES.md, AMELIORATIONS.md, et les GDD IdlePond v1.0 / v1.1.

Genre : idle narratif. Plateforme : Web (React 19 + Phaser 3), PWA mobile. Aucun nom propre définitif. [le héros], [la Primordiale], [l'esprit] sont des termes de conception.

Journal

v2.4 — [P4] tranché : six assises, fixées au papier, et la contradiction §6.2 / §16.1 est levée. Introduction de la règle d'engagement par assise : le document décrit six, la production s'engage une assise à la fois, et l'assise VI n'est lancée qu'après mesure. La durée de la courbe ([P20]) reste ouverte et devient une question de calibrage, non de contenu.
v2.3 — Les deux blocages structurels sont levés. [P2] tranché : le canal acclimaté est borné par le plafond de maturation — il dépend de la part du type mûr dans la charge d'un palier, non de la densité absolue. [P3] tranché : la ponte est une évolution subie, et l'indication finale ne rend survivable que le dernier franchissement. [P1] appliqué en conséquence. Reste ouvert : le scope ([P4]/[P20]).
v2.2 — Audit croisé contre le dépôt. Ajout du §14 : les succès deviennent le système de distribution narrative continue et portent des effets. Amendement du coût de redescente (§4.1, §6.4) : aménager reçoit sa formule, la décroissance passe par la technique. Correction de l'en-tête et de l'alignement Tier 2. Application de la purge lexicale annoncée en v2.0 (zone, milieu). Recalcul des franchissements de palier depuis la courbe. Corrections de §12.1, §12.3, §18.2 et de l'Annexe C. Deux blocages structurels ouverts en [P] : la nature de la ponte (§10.5) et la borne du canal acclimaté (§3). Ajout de l'Annexe D, registre unique des questions ouvertes.
v2.1 — Alignement sur idlepond-tier-2.md v0.8. Ajout de la règle « directe et pauvre, pas évasive » (§13.1) et des deux temps de la révélation dans un même échange (§13.2). Ajout du frein structurel sur le nombre d'alliés : chaque temple est une taxe permanente sur le revenu de Foi (§9.3).
v2.0 — Refonte après audit contre le dépôt. Suppression de l'eau pure comme ressource. Foi ramenée aux miracles et à l'effort. Captation en deux canaux additifs. Ajout des portails et des temples élémentaires comme systèmes de plein droit. Fin en quatre temps, par indication et non par prêt. Ajout de la technique comme troisième axe de persistance. Suppression du refuge à achats permanents. Ajout de la défiscalisation.
v1.1 — Correctifs sur l'adressage et la pureté. Périmée : bâtie sur idlepond-tier-2.md v0.4 sans accès au Tier 0 ni au Tier 1.
v1.0 — Première rédaction. Périmée.
Table des matières
Vision et piliers
Le héros, son but, sa jauge
La captation : deux canaux additifs
Les monnaies
Boucles de jeu
Géographie : assises, paliers, et ce qu'il y a dessous
Le vivant : convaincre, peupler, s'acclimater
Les portails : le réseau des bassins
Les temples élémentaires
La ponte
Ce qui persiste, et la technique
La fin, en quatre temps
Le récit et la voix
Les succès et la bibliothèque
Direction artistique
Progression et équilibrage
Monétisation
Architecture technique et migration
Roadmap — Annexe A : glossaire — Annexe B : ce qui change depuis le GDD v1.1, et pourquoi — Annexe C : traçabilité depuis Étang des Merveilles — Annexe D : registre des questions ouvertes
1. Vision et piliers
1.1 Vision

Un alevin mute dans sa ponte et capte le mana de ses frères morts. Une entité très ancienne le remarque et lui indique un trou au fond de sa mare. Il passera ses vies à creuser vers ce qui s'y trouve, en peuplant l'eau au-dessus de lui, parce qu'il n'y a pas d'autre façon de descendre.

Ce n'est pas un jeu d'accumulation de créatures. C'est la chronique de la fondation d'un royaume, où la mécanique et le récit disent la même chose : rien ne se détruit, tout s'alourdit, et ce qui vit dedans change.

En une ligne de système : le mana sert à creuser, creuser augmente le volume habitable, le volume habitable augmente la population, la population augmente le mana. La densité ne redescend jamais.

1.2 Fantasy joueur

Je suis né en prenant ce qui restait des morts. J'ai vu que ceux qui deviennent différents sont chassés. Je passe mes vies à creuser vers quelque chose qui m'appelle, et à construire en chemin le seul endroit du monde où l'on peut devenir différent sans en mourir.

La fantasy n'est pas la puissance, c'est l'accueil. Le joueur ne collectionne pas des créatures rares : il recueille des ratés.

1.3 Piliers
Pilier	Principe	Conséquence mécanique
La physique est le game design	Le mana dépensé retourne à l'ambiant (Tier 0 §5). Le héros n'est pas un puits.	La densité est monotone croissante. Aucune courbe ne peut la faire redescendre, y compris à la ponte. L'escalade n'est pas un choix d'équilibrage.
Ne jamais punir l'absence	Hors ligne, le héros séjourne — et le séjour est le seul moteur connu du plafond (Tier 0 §8).	Gains plafonnés en heures, jamais réduits. L'absence est canoniquement productive.
Ce qu'il est persiste, ce qu'il a bâti régresse	Fixation individuelle (Tier 0 §3).	Trois axes traversent la ponte : son plafond, ce qu'il sait, la densité du bassin. La roche et la population ne la traversent pas. Voir §11.
On ne récompense jamais la mise à mort	Le mana cédé transfère presque tout, le mana pris de force une fraction (Tier 0 §6.2).	Aucun combat, aucune pêche, aucune récolte létale. On convainc.
On dépense, on ne consomme pas	Seule une machine non vivante détruit du mana (Tier 0 §5).	Vocabulaire strict jusque dans le code et l'UI. Aucun libellé « consommation ».
La progression se lit sans l'interface	Un palier change une variable physique visible.	Si un palier n'est lisible qu'en chiffres, ce n'est pas un palier, c'est un multiplicateur : on le retire.
L'UI apprend à parler avec la voix	Le peuple est nommé sans être compris : des gestes qui marchent, aucune théorie juste.	Effets avant chiffres, puis numérisation progressive au rythme des paliers de voix.
Chronique, pas dilemme	Le joueur ne choisit pas s'il accepte, il découvre ce qu'il a déjà accepté.	Chemin unique. Sa seule vraie décision est combien de temps rester.
2. Le héros, son but, sa jauge
2.1 Fiche

Format especes-cadre.md, fiche d'individu.

## [le héros] — individu, non espèce

- Espèce d'origine : poisson de mare, espèce ordinaire
- Écart à l'espèce : faculté de captation apparue par mutation à l'éclosion (Tier 0 §6)
- Trajectoire      : plafond croissant par séjour ; franchissements successifs survécus
- But              : que tout individu qui gagne une conscience sache où se diriger
- Débit            : faible (individuellement négligeable)
- Contenance       : très forte, croissante — axe central du jeu
- Assiette du coût : environnementale (type natal : signature vive de son espèce)
- Registre d'usage : vital dominant, artisanal secondaire
- Captation        : oui, faculté héritable
- Régime           : plafond souple par séjour, dur à court terme
- Doctrine         : recycler — il n'a jamais rien détruit

Faible débit / très forte contenance = espèce réservoir. Le cadre dit de ce quadrant qu'il est voué à diverger avec l'âge, et que saturer lentement est la seule voie qui laisse une chance de franchir sans perdre son corps. Le jeu accélère un destin déjà écrit.

Point structurant : son type natal est celui de son espèce, pas d'un biome. Un puiseur natal de biome est enraciné dans un lieu ; lui est enraciné dans du vivant, et il y a du vivant partout où il en amène. C'est la justification canonique de toute la boucle.

2.2 Son but

Depuis Tier 0 §9, un franchissement sans but produit un monstre, non un dieu. Le but n'est pas un ornement : c'est la condition de son apothéose, et il est fixé dès l'éclosion.

Il voit deux choses à sa mutation : les poissons ordinaires errent sans but et cela leur suffit ; ceux qui ont acquis quelque chose de plus sont chassés. Sa dette est celle d'un témoin, non d'un survivant.

Laisser les poissons ordinaires suivre leur instinct. Et bâtir, pour ceux qui deviennent différents, un endroit où l'être ne coûte pas la vie.

Il ne veut pas gouverner, il veut représenter. Son domaine est la sécurité des éveillés.
Il est l'exemple : ce qu'il devient prouve que la chose est possible, ce qui est une réponse, donc ce qui attire la foi (Tier 0 §1, réciprocité).
Son but final : ajouter à l'instinct de tous les poissons d'étang une ligne — que tout individu qui s'éveille sache où aller. Pas remplacer l'instinct, y ajouter une adresse.
2.3 La jauge
Ligne	Ce que c'est	Comment ça bouge
Niveau	Ce qu'il contient à l'instant	Monte avec la captation, descend avec la dépense
Plafond	Ce qu'il peut contenir sans danger	Ne monte qu'à la ponte, définitivement
Acquis de séjour [P]	Ce que son séjour en mana dense lui a déjà gagné, non encore franchi	Monte passivement, converti au franchissement

[P] La troisième ligne est un raffinement Tier 2, à valider. Elle découle de la lecture conjointe de deux phrases du dépôt : le séjour est le seul moteur du plafond (Tier 0 §8), et chaque ponte est un franchissement survécu (§10.5). Le séjour ne monte donc pas le plafond directement — il construit la capacité à franchir, et le franchissement est la ponte.

Bénéfice de lisibilité : le joueur voit en permanence ce qu'il gagnerait à pondre maintenant, et cette barre grandit tant qu'il reste. C'est le cœur de sa seule décision.

2.4 La saturation

Si le niveau touche le plafond, Tier 0 §2 s'applique : condensation, puis divergence. Ce n'est pas un game over, c'est le prestige.

Alerte à 85 % : l'eau se trouble, la faune s'écarte. Un effet, pas un texte.
Saturation à 100 % : la captation s'arrête. Il dépense encore, il ne gagne plus.
Divergence non choisie après un délai : la ponte se déclenche seule, et fixe moins d'acquis qu'une ponte choisie.

Un joueur qui ignore sa jauge n'est jamais bloqué et ne perd jamais sa partie. C'est la seule pénalité du jeu, et elle est douce.

Note lexicale, tranchée en v2.3. La v2.1 écrivait « divergence forcée ». Le terme est occupé au Tier 0 §2 par le cas inverse : forcé y désigne la tentative délibérée de franchir son plafond, celle qui tue dans la plupart des cas. Le cas décrit ici est passif — le joueur n'a rien tenté, il a laissé la jauge monter. Employer le mot du canon pour son contraire aurait empoisonné toute discussion future avec le Tier 0. Divergence non choisie partout.

3. La captation : deux canaux additifs

Point mécanique essentiel, dérivé du type natal du héros. Deux canaux distincts, à additionner — jamais à multiplier.

Canal	Source	Rendement	Ce qui le fait monter	Ce qui le fait baisser
Natif	Signature vive de son espèce et de ses semblables	100 %, d'emblée	La population vivante présente	La mort ou le départ de cette population
Acclimaté	Type mûr du palier	Très bas, croissant	Le temps passé, et la maturation du palier	Le peuplement du palier, qui dilue son type
captation/s =   débit_natif(population_vivante_présente)
              + débit_acclimaté(part_mûre(palier) × rendement_acclimatation(assise))

part_mûre(palier) = charge du type mûr ÷ charge totale du palier
3.0 Le plafond de maturation : ce qui borne le canal acclimaté

Décision structurelle, tranchée en v2.3. Le canal acclimaté n'est pas indexé sur la densité absolue. Il est indexé sur la part du type mûr dans la charge d'un palier.

Le problème que cela résout : les deux canaux sont additifs, et une somme est dominée par son terme non borné. Le natif est plafonné par la population, elle-même plafonnée par la capacité des paliers ouverts. Si l'acclimaté suivait la densité — qui ne redescend jamais (§1.3) — multipliée par un rendement revenant à 100 % dès que l'assise est retrouvée (§7.3), alors la captation cesserait de dépendre de la population vers la mi-partie, et les trois conséquences du §3 cesseraient d'être vraies.

La borne est canonique, et elle était déjà écrite. mana-typologie.md §1 : un milieu dense en vivant est constamment réensemencé en signature vive, donc il accumule sans mûrir. Une mare peut être immensément ancienne et rester bloquée au degré 2 — riche et jeune en permanence.

Peupler un palier le noie de signature vive, donc dilue son type mûr, donc fait baisser son rendement acclimaté.

Aucun invariant n'est touché. La densité absolue continue de monter et ne redescend jamais : seule la proportion bouge. Le mana dépensé retourne toujours à l'ambiant, rien n'est détruit.

Ce que ça produit

Un arbitrage par palier, posé rarement. C'est le bon format pour ce genre : une décision d'allocation qui s'inscrit dans la durée, pas une action à répéter.

Conduite d'un palier	Ce qu'on y gagne	Ce qu'on y perd
Le peupler	Canal natif fort, immédiat	Le palier reste jeune : rendement acclimaté écrasé, et il faut renouveler la population sans cesse
Le laisser maigre	Le type mûrit, le rendement acclimaté monte et ne redescend pas	Aucun apport natif, et le rendement met du temps à venir

La géographie du jeu enseigne déjà la règle. La mare est dense en vivant, réensemencée, bloquée au degré 2 (§6.2). La mer relique est pauvre en vivant, scellée, mûrie jusqu'à l'élémentaire — et c'est précisément pour ça qu'elle peut porter un élémentaire là où la mare n'y arrivera jamais (§6.5). Le joueur comprend le système en regardant la carte : les deux extrémités du monde sont les deux extrémités de l'arbitrage.

Et la pierre devient l'aboutissement logique plutôt qu'un décor : rien n'y vit, donc rien n'y dilue, donc elle condense indéfiniment (§6.6).

[P29] À calibrer : la vitesse de maturation d'un palier laissé maigre, et la granularité de part_mûre — par palier, ou par assise. Le premier réglage décide si l'arbitrage se joue à l'échelle d'une session ou d'un cycle.

La propriété décisive : la signature vive se fond dans l'ambiant en quelques jours (mana-typologie.md §1). Son seul canal à 100 % dépend donc d'une population qu'il faut renouveler en permanence. Trois conséquences :

Peupler n'est pas produire, c'est reconstituer son propre type natal. Convaincre les espèces est une nécessité physique, pas un multiplicateur.
Son ancrage est du vivant, pas un lieu. C'est ce qui le rend acclimatable à tout, et ce qui lui permet de descendre là où un enraciné de biome ne pourrait pas.
La ponte devient cruelle et juste : perdre la population, c'est perdre l'accès à son propre type natal jusqu'à reconviction.
3.1 La défiscalisation

La Primordiale a coupé la collecte sur ce bassin. La part adressée — infime, mais la plus pure — reste sur place au lieu de partir (Tier 0 §1). Les élévations y sont donc plus probables qu'ailleurs.

C'est la justification interne du rendement anormal de la mare, et le joueur ne l'apprend qu'au palier « directives ». Jusque-là, sa chance ressemble à de la chance.

4. Les monnaies

Deux, et il n'y en aura pas de troisième.

	Mana	La Foi
Perçu	En permanence	Uniquement dans l'œuf
Abondance	Élevée, sale	Minuscule, la plus pure qui existe
Source	Densité locale et population	Ce que le peuple éprouve en son absence
Usage	Creuser, élargir, convaincre, aménager, ouvrir un portail, bâtir un temple	Miracles, et rien d'autre
Persistance	Perdu à la ponte	Permanente
4.1 Mana

Jamais acheté, jamais échangé, jamais vendu : capté. Cinq puits, aucun ne détruit quoi que ce soit.

Puits	Effet	Survit à la ponte
Creuser	Ouvre le palier suivant	Non
Élargir	Augmente la capacité d'accueil d'un palier ouvert	Non
Convaincre	Installe une espèce dans un palier	Non
Aménager	Rend un palier habitable pour du vivant ordinaire. Puits principal de la redescente — voir §6.4	Non
Ouvrir un portail	Liaison vers un autre bassin, coût bilatéral	Non — mais la densité acquise de l'autre côté, oui
Bâtir un temple	Crée une adresse locale pour un allié	Non — mais la charge obtenue, oui
4.2 La Foi

Le héros est localisé, donc adressable. Mais il ne perçoit que pendant sa réincarnation, et la raison est un renoncement, non une concentration.

Le Tier 0 §1 permet à tout destinataire de renoncer à encaisser, et agir comme encaisser sont deux emplois du même être. Il ne peut pas faire les deux : il règne, ou il encaisse. La ponte est le seul moment où il est trésorier plutôt que roi — et il continue de percevoir pendant qu'il est scellé, comme un dieu endormi.

[P] Condition d'épuisement — extension Tier 2 mineure, à porter. La scellure ne remplace pas ce renoncement, elle le rend total au lieu de révocable : il ne décide pas de ne plus prélever, il ne peut plus. Le mécanisme est l'isolement du volume et non une propriété de la coquille — un volume clos n'offre que son propre ambiant, et celui d'un œuf est épuisé presque immédiatement.

Effet à exploiter : il vient d'expirer tout son mana dans l'eau, donc l'eau autour de l'œuf est la plus dense de sa vie, et c'est exactement le moment où il ne peut pas y toucher. Il est scellé au centre du pic qu'il a fabriqué.

Ce qu'elle est, mécaniquement : une réserve d'effort

Fonction première, et c'est le Tier 1 qui la donne : un analgésique, pas un carburant. Tout acte du héros au-delà de l'ordinaire lui coûte un effort (Tier 0 §9), et l'effort se paie de deux façons :

sur sa réserve de Foi — il fait porter le poids par ce qu'il a encaissé, et ne s'use pas ;
sur lui-même, réserve vide — fatigue, puis torpeur.

D'où l'arbitrage permanent du jeu : agir beaucoup et s'user, ou peu et rester vif.

Ce qu'elle achète

Des miracles, c'est-à-dire ce que le creusement ne peut pas acheter :

Miracle	Effet
Réveiller	Rendre à un franchissement interrompu la capacité de reprendre
Guérir	Soigner un égaré que rien d'autre ne sauve
Convaincre ce qui refuse	Installer une espèce dont l'affinité l'exclut du palier
Tenir	Empêcher l'effondrement d'une paroi qui devait céder
Faire survivre	Améliorer l'issue d'un franchissement, sans jamais la garantir

Règles de conception, absolues. La Foi n'est jamais une monnaie de creusement. Elle ne double pas le mana, elle ouvre une catégorie d'actions séparée. Elle n'achète ni rendement, ni multiplicateur, ni plafond d'heures hors ligne, ni bâtiment. Sa quantité est minuscule : tout arbre d'achats en Foi est une erreur de conception.

Et un miracle ne garantit rien (Tier 0 §8) : il déplace des probabilités. Une réussite garantie payée en Foi contredirait le dépôt.

[P] Le terme « Foi » ne nomme que le quart favorable de ce qui est émis — la crainte paie autant que l'amour. Candidats : veille, égard, ferveur, mémoire.

5. Boucles de jeu
5.1 La boucle canonique
mana ambiant capté
   → creusement / élargissement
      → volume habitable
         → population (espèces convaincues, acclimatées)
            → débit total, et canal natif restauré
               → densité ambiante (elle ne redescend jamais)
                  → mana capté, et saturation du héros
                     → ponte
                        → plafond franchi, densité et savoir conservés
                           → redescente plus rapide

Cinq verrous Tier 0 :

Creuser ne détruit rien. Le mana dépensé retourne à l'ambiant.
La vitesse de creusement est plafonnée par la régénération locale, fonction de la biomasse.
La population est un investissement à convaincre, pas une ressource à extraire.
La saturation est probabiliste, jamais un palier garanti.
Le bassin est défiscalisé — voir §3.1.
5.2 La chaîne de mi-partie

La mi-partie a son propre moteur, et il est entièrement canonique. Cette chaîne est la meilleure trouvaille du dépôt et elle doit être lisible en jeu :

portail ouvert  →  migrants  →  parmi eux un éveillé porteur d'une adresse étrangère
                                        ↓
                              temple bâti (une adresse locale)
                                        ↓
                              l'allié répond, et dépense sa substance sur place
                                        ↓
                            le palier se charge de SON type
                                        ↓
                              la faune locale diverge selon ce type → espèces nouvelles

Le héros ne crée aucune espèce. Il choisit de quoi ses paliers sont saturés, et la saturation fait le reste.

5.3 Les quatre échelles
Boucle	Durée	Verbe	Récompense
Courte	2–15 min	Élargir, convaincre	Un seuil de peuplement, une déformation, une espèce qui accepte
Longue	1–4 h	Creuser	Un palier : lumière, pression, chimie qui changent à l'écran
Politique	Plusieurs heures	Relier, bâtir, négocier	Un bassin, un allié, un type de mana, une faune nouvelle
Méta	Plusieurs sessions	Pondre	Une couche, un plafond, un palier de voix
6. Géographie : assises, paliers, et ce qu'il y a dessous
6.1 Lexique verrouillé
Terme	Définition	Test
Assise	Subdivision majeure. Assets complets et type de mana distinct, donc acclimatation propre.	A-t-elle son propre type de mana ?
Palier	Subdivision d'une assise. Modulateurs seulement : lumière, particules, pression, chimie, mutation.	Change-t-elle du visible sans introduire de type de mana ?

« Strate » est réservé au plan des dieux et interdit pour de la roche (Tier 0 §9). Milieu, zone, biome, lit, fond, table sont retirés du vocabulaire de jeu.

Purge appliquée en v2.2. La règle était posée depuis la v2.0 et n'avait jamais été appliquée au document lui-même : zone subsistait au §5.2, au §9, au §14 et jusque dans l'identifiant chargeDeZone. Ce qu'un temple charge est un palier, et la structure de données l'indiquait déjà (Record<PalierId, …>). Renommé partout en chargeDePalier. Restent admis, parce qu'ils ne nomment aucune subdivision du bassin : les zones de l'ancien jeu citées en Annexe C, et l'expression divinités de seconde zone.

Aucun palier n'introduit d'acclimatation. Une acclimatation nouvelle est la signature d'une assise.

6.2 Les six assises
#	Assise	Contrainte	Rôle
I	La mare	Aucune	Départ. Bloquée au degré 2 : dense en vivant, donc réensemencée en permanence, donc riche et jeune.
II	Les galeries noyées	Le courant	Tutoriel de l'acclimatation.
III	Le récif fossile	La chimie	Première faune sans parenté. Premier choc.
IV	La fosse	La pression	Dernière lumière. Les premiers égarés arrivent ici.
V	La faille chaude	Chaleur, toxicité	Taux de mutation maximal. Halocline naissante.
VI	La mer relique, puis la pierre	Salinité, autre monde	Terminus, en deux étages.

Six, fixé — décision v2.4. Six est le minimum pour que la descente ait des chapitres, et le maximum tenable pour un développeur seul. La v2.1 laissait subsister une cible « 5 à 6 » ici et un « 6, fixé » au §16.1 ; la contradiction est levée en faveur de six.

Règle d'engagement par assise. Six au papier n'est pas six en production. Le document décrit la chronique entière — c'est ce qui garantit que les assises tardives sont écrites, et non improvisées quand on y arrivera — mais la production s'engage une assise à la fois, chacune n'étant lancée qu'après mesure de la précédente (§16.3).

Ce qui protège spécifiquement les deux étages de l'assise VI, poste d'assets le plus lourd du projet (§16.4) :

La fin est écrite dès maintenant et ne bouge plus : la pierre est la prémisse causale de tout le jeu (§6.6), la mer relique porte l'halocline, et le retournement du §13.2 en dépend. Couper là reviendrait à retirer la fin pour sauver le milieu.
Si un arbitrage devient nécessaire, il se fait sur la longueur des cycles intermédiaires, jamais sur le nombre d'assises. C'est la variable [P20], et elle se règle après le prototype.

On coupe au milieu, jamais à la fin.

Grille obligatoire — une assise n'est pas canonique sans ses six lignes : type de mana (degré + conditions de formation), lumière, contrainte physique, faune propre, faune mutée, acclimatation.

6.3 Les paliers

Proposition : 6 / 8 / 10 / 12 / 14 / 16 = 66. Croissant, parce que les assises tardives sont retrouvées avec une acclimatation déjà acquise, donc traversées plus vite.

Ne produire que les six paliers de l'assise I avant d'avoir vu la boucle tourner.

6.4 La contrainte de redescente

À chaque ponte les galeries s'effondrent, donc les paliers sont retraversés.

Chiffre corrigé en v2.2. La v2.1 annonçait « environ 900 franchissements », obtenu en multipliant 66 paliers par 13 pontes — ce qui suppose que toute la profondeur est retraversée dès le premier cycle. Faux : la courbe du §16.2 n'atteint l'assise VI qu'au cycle 11. En sommant la profondeur réellement atteinte à la fin de chaque cycle, on obtient ~630 franchissements de redescente, plus les 66 premières ouvertures, soit ~700 au total. Le chiffre reste élevé et le risque tient ; il est simplement exact, et il devra être recalculé si le nombre d'assises change ([P4]).

Le paramètre critique n'est pas le nombre de paliers, c'est la fraction d'un cycle passée à redescendre.

Cible dure : 20 à 25 %.

Ce que coûte une redescente

Le puits de la redescente est aménager : rendre un palier de nouveau habitable pour du vivant ordinaire.

coût_aménagement(palier) = coût_base(palier) × f × réduction_technique
f — fraction fixe du coût d'origine. Paramètre global unique, réglé pour viser les 20–25 %. [P5]
réduction_technique — décroissance gagnée, portée par les acquis de §11.2 (étaiement, plomberie sous contrainte, conviction) et par les succès à effet chiffré (§14.3). Elle ne s'achète pas : elle s'apprend.

Un puits, un levier. L'aménagement est payé par la technique ; la reconviction garde sa formule et reste payée par la densité (§7.1). Aucun coût n'a deux leviers — c'est ce qui rend l'ensemble équilibrable.

Ce n'est pas le Corail de Prestige. La décroissance passe par un axe qui s'acquiert par l'usage, non par un arbre d'achats. Annexe B tient : la technique s'apprend au lieu de s'acheter. Les succès à effet chiffré y gagnent une cible naturelle, au lieu d'être des multiplicateurs flottants.

Ce que le héros ne repaie jamais

Point canonique à ne pas franchir. Tier 0 §3 : un être surévolué conserve ses acquis à vie, en tout lieu. C'est un invariant. Le héros ne repaie donc jamais son acclimatation — §7.3 et §16.1 tiennent, et especes-cadre.md §3 confirme que seule une évolution porte le rendement au maximum d'un type non natal.

Ce qui se repaie est l'habitabilité du palier pour son peuple. Fondement : le même Tier 0 §3, mais son autre moitié — la fixation vaut pour l'individu, la dérive vaut pour la lignée. À chaque ponte il perd sa population. Les poissons qu'il réinstalle dans un palier profond ne sont pas ceux d'avant : ni l'habitude, ni la chimie, ni la pression.

Ce n'est pas lui qui se réacclimate. C'est son peuple qui n'y est jamais allé.

Ce que ce coût ne pilote pas

Le mana se stocke. Un joueur muni de technique et d'une forte densité recapte vite de quoi payer : la dépense est un seuil, pas une durée. Le vrai plancher de durée de la redescente est le repeuplement — §7.2, la population repousse à un taux et ne s'achète pas, et le canal natif dépend de la population vivante présente (§3).

f donne un puits au mana d'après-ponte. C'est k, le taux de repeuplement, qui produit les 20–25 %.

Régler l'un sans l'autre, c'est tourner le mauvais bouton. Les deux sont à calibrer ensemble au prototype (§16.3). [P6]

Aucun multiplicateur n'est ajouté pour atteindre la cible. La redescente est rapide parce que l'acclimatation revient à 100 %, parce que la densité n'est jamais redescendue, et parce que ce qu'il sait ne s'effondre pas (§11). Le confort du prestige est entièrement diégétique.

6.5 La mer relique

Un bassin marin scellé sous la roche, piégé depuis une époque où la mer couvrait la terre. Ce n'est pas l'océan : aucune communication avec la mer actuelle, qui reste ingouvernée et réservée à un autre jeu.

Le second bestiaire y trouve sa source : des êtres déjà là, sans parenté avec la surface.
L'halocline est la meilleure contrainte d'acclimatation du jeu. Rendement quasi nul au départ, croissant avec le séjour, et une raison viscérale de ne pas descendre trop vite.
Elle a mûri, contrairement à la mare. Symétrie exacte du plafond de maturation : peu de vivant, aucun réensemencement, des ères de vieillissement ininterrompu. Elle peut porter un élémentaire là où la mare n'y arrivera jamais — et c'est ce qui fait de sa conquête l'enjeu réel du jeu.
C'est un angle mort de la Primordiale : scellé, pauvre en vivant, presque aucun adressage. Elle voit l'effet, la suintance, et pas la cause.

La suintance est infime et doit le rester : un suintement, pas une source. Elle produit un gradient — salinité et étrangeté croissent continûment vers le bas, donc l'acclimatation est un taux et non un déblocage — et elle produit les égarés.

6.6 Sous la mer : la pierre

Une pierre élémentaire d'eau, reste de l'œuf d'un esprit absent. Elle ne produit aucun mana — seul le vivant en produit — mais elle condense indéfiniment ce que son type appelle. C'est elle qui maintient tout l'étage inférieur en eau.

pierre élémentaire d'eau   (condense sans jamais s'arrêter)
        ↓
mer relique                (scellée, pauvre en vivant, donc mûrie jusqu'à l'élémentaire)
        ↓ suintance infime
assises intermédiaires     (gradient de salinité, égarés remontés)
        ↓
la mare                    (dense en vivant, réensemencée, bloquée au degré 2)

Tout le monde du héros est en aval de la pierre. Ce qu'il ressent depuis l'éclosion n'est ni la mer ni le mana ancien : c'est la source, deux étages plus bas. La prémisse du jeu cesse d'être un postulat et devient une conséquence.

Et la pierre est une adresse : un objet ayant appartenu à une entité, dont l'émotion résout vers son propriétaire. La convoitise suffit, sans aucune foi.

Le héros a passé toutes ses vies à désirer cette pierre sans savoir ce qu'elle était. Il a donc adressé, pendant des générations, l'esprit qui en est sorti.

6.7 Ce qu'il n'y a pas au fond : aucune pureté à fabriquer

Décision, et elle ferme une fausse piste. L'eau de la pierre est déjà pure au registre antique : rien n'y a jamais touché, parce que rien n'y vit. Il n'y a donc aucune purification à produire, aucun débit de pureté à faire monter, aucune barre à remplir.

La raison est canonique et dure : la pureté est une absence de manipulation, jamais une absence d'origine. Toute mécanique de fabrication de pureté est un raffinage, donc un puits, donc une faute. Et l'antique n'est pas raffinable par définition.

Ce qui remplace, et qui est meilleur : la pureté est une propriété du lieu, et le problème est de ne pas la salir. Le terminus n'est pas « produire assez de pureté », c'est arriver assez grand pour s'y transformer sans y toucher. Le gate est la contenance, comme partout ailleurs dans ce jeu.

6.8 Les deux bestiaires, à ne jamais mélanger
	Ce qu'on fabrique	Ce qu'on libère
Origine	Divergences de la faune de la mare	Faune déjà présente dans une assise scellée
Lecture	Familier et déformé. L'animal d'origine se reconnaît.	Sans parenté. Autres symétries, autres membres.
Statut	Ses œuvres. Il en est l'auteur.	Des témoins. Il n'y est pour rien.

Rappel Tier 0 §2 : les monstres ne sont pas des ascensions réussies, ce sont des ascensions ratées qui n'ont pas fini de mourir. Aucune créature hostile du jeu n'est un ennemi ; ce sont des échecs.

Amortissement de l'assise VI, le poste d'assets le plus cher : deux ou trois spécimens de sa faune fuient dès les assises IV–V par la fissure, avec les égarés.

7. Le vivant : convaincre, peupler, s'acclimater

On n'achète plus de poissons. Pas de marché, pas de prix par exemplaire, pas de collection payante.

7.1 Convaincre
coût_conviction(espèce, palier) = coût_base(espèce)
                               ÷ affinité(type_du_palier, espèce)
                               ÷ densité_locale_du_type_supporté

Le second dénominateur est la réponse au problème du prestige : une espèce se laisse reconvaincre d'autant plus facilement que l'eau est déjà chargée du type qu'elle supporte. Ce n'est pas un bonus, c'est une conséquence — la densité conservée est la mémoire du monde, et c'est elle qui paie le retour.

Une espèce que son affinité exclut d'un palier ne se convainc pas au mana : elle se convainc au miracle (§4.2).

7.2 Peupler
population_max(espèce, palier) = capacité(palier) × habitabilité(palier, espèce)
croissance/s                   = k × régénération_locale × (1 − pop/pop_max)

Le joueur achète de la place et de la qualité de place. La population suit. Il n'appuie pas cent fois sur un bouton.

Seuils de peuplement — conversion du système de jalons de l'ancien GDD, sa meilleure mécanique de satisfaction courte :

Seuil	Effet propre	Effet global
10 individus	×2 débit de l'espèce	+x % débit total
25	×4 cumulé	+x %
50	×8 cumulé	+x %
100	×16 cumulé	+x %

Différence essentielle : les seuils s'atteignent par le temps et la place, non par la dépense.

7.3 Acclimatation : le double régime
	Sans ponte	Après ponte
Ce qui se passe	Il atteint ce que son plafond permet — technique, bornée	Il a franchi — évolution, définitive
Rendement de l'assise	Dégressif au départ, croissant avec le temps passé	100 % dès qu'il la retrouve
Verbe joueur	Persister	Revenir
Le joueur n'est jamais bloqué. Une assise trop profonde reste exploitable, mal, et de mieux en mieux. La ponte n'est pas une porte, c'est un taux.
Le héros ne force jamais son évolution en cours de partie. Forcer tue. Le seul franchissement volontaire du jeu est la ponte.
7.4 Les égarés

Reliques vivantes remontées par la fissure, à partir de l'assise IV. Mal adaptées, isolées, sans congénères. Pathétiques plutôt que menaçantes, ce qui va très bien avec un refuge pour les rejetés.

Rôle mécanique : ils ne produisent presque rien, ils coûtent — capacité et entretien. Ils ne rendent qu'une chose : la preuve que l'en-dessous existe.

Rôle narratif, décisif : ils portent l'escalade entre la troisième ponte et le relais. Ils prouvent l'en-dessous sans l'expliquer, parce qu'ils ne peuvent pas l'expliquer non plus.

[P] Le premier égaré guéri par miracle est le premier être d'en-dessous doté d'une parole, et il préfigure le relais.

7.5 Les fidèles arrêtés

Peuple secondaire, gratuit et canonique : des espèces qui ont tenté, qui prient encore, et qui ne montent plus. Pas une punition — de l'aléa que personne ne maîtrise, pas même la Primordiale.

Ils sont les meilleurs candidats au miracle « réveiller », et la preuve vivante que rien ne garantit une élévation.

8. Les portails : le réseau des bassins

Un portail est un tunnel entre deux plans d'eau, qui transporte du vivant et rien d'autre, en dépensant du mana des deux côtés.

Vocabulaire strict : on dépense, on ne consomme pas. Le mana dépensé retourne à l'ambiant des deux bassins.

8.1 Pourquoi c'est possible
Aucune densité ne circule. Le mana n'atteint quelqu'un à distance que par adressage. Un portail déplace de la biomasse, et la densité monte parce que les producteurs ont bougé. Aucun invariant touché.
C'est le seul réseau franchissable sans décompression de type. Deux mares sont du même type au même degré : seule la densité diffère. Là où passer d'une région à une autre exige de survivre à un changement simultané de densité et de type, un archipel de mares est topologiquement mieux relié que deux cités voisines. [P] Personne d'autre n'a jamais pu tenir ce domaine : il faut être né dedans.
Le coût bilatéral est le vrai régulateur. Un bassin pauvre ne peut pas soutenir sa moitié du tunnel. Ouvrir une liaison exige d'avoir d'abord enrichi l'autre côté — ce qui donne au réseau une progression naturelle au lieu d'une liste de déblocages.
8.2 Ce que ça produit
La mare devient la plus dense du réseau parce qu'elle est la plus peuplée. Les éveillés des autres bassins convergent vers le refuge. C'est de là que lui viendra son nom.
Un flux de migrants, pas un multiplicateur. Chaque bassin relié apporte des individus, des espèces, des types natals nouveaux, et des problèmes de cohabitation.
Et l'inverse est vrai. Les bassins reliés reçoivent aussi du monde et de la densité. Ce n'est pas une pompe à sens unique, c'est une mutualisation — exactement le but affiché du héros.
Et surtout : des adresses. Un migrant éveillé apporte une orientation que personne dans la mare ne possédait. C'est l'entrée de la chaîne du §5.2.
8.3 En jeu
Élément	Décision
Structure du réseau	[P] Graphe libre, avec un coût croissant en distance topologique
Coût d'ouverture	Mana, des deux côtés. Le côté distant doit avoir été enrichi au préalable
Enrichir un bassin distant	Y envoyer de la population par la liaison précédente. Le réseau se construit par proche en proche
Survit à la ponte	Non — la liaison s'effondre. Mais la densité acquise de l'autre côté persiste, donc la réouverture est bien moins chère
Cas fondateur	L'anguille électrique, première migrante, venue d'un bassin de côte d'orage
9. Les temples élémentaires

C'est le système qui rend les assises construites plutôt que subies, et il est entièrement canonique.

9.1 La séquence
Rencontre — une créature d'un élément apparaît ou est trouvée.
Contact — et c'est le point contre-intuitif : l'adressage ne route pas par type de mana. Le seuil est l'orientation, et la répartition est émotionnelle. Une bête d'instinct adresse la Primordiale, pas un dieu de la foudre. Le contact exige donc un éveillé qui craigne ou vénère quelque chose. C'est pourquoi il faut des migrants (§8).
Temple — le héros bâtit un collecteur, qui n'est pas une pompe d'importation mais une adresse : un point où une réponse locale devient possible.
Charge — l'entité répond, et un dieu qui agit dépense sa substance et la rend à l'ambiant. Cette substance porte son type. C'est la réponse qui charge le palier, jamais la collecte. Le champ de probabilités opère alors, et la faune diverge selon le type dominant.
9.2 Trois conséquences dures
Un temple sans réponse ne charge rien. Bâtir ne suffit jamais. Un allié endormi, ruiné ou fâché laisse un bâtiment inerte — et rien ne distingue un temple mort d'un temple qui attend.
Charger un palier coûte au créancier sa substance et son effort. Le prix payé par l'allié est énorme, ce qui interdit de traiter les temples comme une liste de déblocages.
Le dosage dépend de ce que l'allié consent à dépenser, non d'un timer. [P] Une réponse trop généreuse sature le voisinage et produit des monstres au lieu d'une espèce. La différence entre une divergence et une horreur est un problème de calibrage, et il n'est pas dans les mains du joueur.
9.3 Le frein : chaque allié est une taxe permanente sur la Foi

Risque de conception identifié. La chaîne du §5.2 fait des adresses étrangères la ressource critique, et les portails en fournissent. Sans frein, la mare devient un panthéon : le royaume se remplit de créanciers dont chacun a son propre but, et il perd sa marge de manœuvre.

Le frein ne doit pas être un plafond. Un nombre maximal d'alliés serait arbitraire, donc contourné le jour où il faudra du contenu. Il en existe un dans le canon, et il est direct.

Un temple installe un destinataire concurrent à l'intérieur de sa propre population. La répartition de l'adressé est émotionnelle, et personne ne contrôle la sienne (Tier 0 §1). Ce que son peuple adresse à cette entité, il ne le lui adresse plus. La Foi étant minuscule et seule monnaie de miracle, trois alliés se voient immédiatement sur son revenu — et la répartition ne se défait pas.

[P7] Deux questions que le frein laisse ouvertes.

Un temple inerte taxe-t-il ? Le raisonnement ci-dessus exige une entité qui réponde et capte l'adressage du peuple ; le journal de la v2.1 dit pourtant « chaque temple ». Les deux ne peuvent pas être vrais.
Le frein a une latence d'un cycle entier. La Foi n'est perçue que dans l'œuf (§4) : le coût d'un temple ne devient donc observable qu'à la ponte suivante, des heures ou des jours plus tard. Combiné au fait que rien ne distingue un temple mort d'un temple qui attend (§9.2), le joueur paie un prix permanent invisible pour un bénéfice peut-être nul, sans jamais pouvoir apprendre.

Sortie recommandée, à coût quasi nul. Tier 0 §1 distingue adresse, perception et encaissement, et précise que renoncer à encaisser ne ferme pas le canal. Afficher en permanence le flux qui lui est adressé — visible, non encaissé — et ne créditer la réserve qu'à la ponte. Bâtir un temple fait alors chuter la ligne à l'écran, immédiatement. Le frein devient lisible à la seconde où il est payé, sans qu'aucune règle ne bouge. Et l'image est juste : il voit toute sa vie ce qu'il ne peut pas prendre, et ne le prend qu'endormi.

Le plafond devient donc émergent : le joueur veut peu d'alliés. Et cela crée la seule vraie décision stratégique d'un jeu par ailleurs défini par « combien de temps rester » :

On échange	Contre
Du revenu de Foi permanent	Une transformation permanente du monde — un type de mana, donc une faune

Le panthéon-comme-partage-de-marché du Tier 0 devient concret à l'échelle de la mare. Son refuge est un marché saturé, lui aussi.

9.4 Deux conséquences qui protègent le système

Une adresse ne suffit pas. Le canon exige que l'entité veuille quelque chose et consente à dépenser substance et effort. La plupart des adresses ne mènent donc nulle part : l'entité dort, elle est ruinée, indifférente, ou son but est incompatible. Et rien ne distingue un temple mort d'un temple qui attend.

Conséquence de conception : le joueur peut bâtir large, peu répondront. Le goulot est la rareté des dieux disposés, pas celle des adresses — donc les portails peuvent rester généreux sans faire gonfler le panthéon. Le §8 et le §9 sont découplés.

Qui accepte, en fait. Ce que la mare offre — une adresse, et des fidèles qui n'étaient pas les siens — n'intéresse aucun dieu riche. Cela intéresse un dieu marginal : obscur, appauvri, réveillé par accident, écarté d'un panthéon saturé.

Ses créanciers sont donc des divinités de seconde zone, ce qui va très bien avec un royaume de rejetés. Même ses dieux sont des ratés. Et cela isole l'esprit par contraste : il ne demande pas de fidèles, il demande une place. Coût nul en Foi, prix élevé en principe.

9.5 En jeu
Élément	Décision
Ce que le joueur choisit	De quoi ses paliers sont saturés. Jamais quelle espèce apparaît
Survit à la ponte	Le bâtiment non. L'alliance oui — de l'information et une dette, pas de la roche. La charge obtenue oui — la densité ne redescend jamais
Conséquence	Un temple est un investissement permanent en effet, temporaire en structure. C'est ce qui empêche la ponte d'être punitive
Coût permanent	Une part du revenu de Foi, définitivement redirigée. Non annulable
Nombre d'alliés	Émergent, pas plafonné. Cible observée attendue : 3 à 4 sur une partie complète
Ce que réclame un allié	[P] Une contrainte permanente sur la conduite du royaume, non un paiement ponctuel — maintenir une population, refuser un type rival, laisser ses fidèles prêcher. Les contraintes s'empilent en gêne
10. La ponte
10.1 Ce que ça fait

Le héros s'enferme dans un œuf et expire dans l'eau tout le mana accumulé. Ce n'est pas une défaite : c'est le seul geste volontaire de toute sa vie.

	Effet
Au monde	Libération massive en un point. Pic de densité, bascule du champ de probabilités : nouvelles adaptations, faune permanente plus étrange
Au chantier	Les parois se referment, les galeries s'effondrent, les paliers profonds redeviennent inaccessibles
Au peuple	Ils ne se souviennent pas, ou pas de la même façon. Il faut reconvaincre
Au héros	Il franchit, et ressort avec un plafond élevé, définitivement
À lui, pendant	Il encaisse la Foi

Le monde se souvient de la densité. La roche ne se souvient pas des galeries.

10.2 Nombre de pontes

Cible : 12 à 15 pour une partie complète. Une par assise au début, deux à trois vers la fin. Au-delà de 30 la ponte devient une routine et perd son poids narratif, ce qui est incompatible avec une chronique.

10.3 Les couches du corps

Six couches pour six assises. Le corps affiche la profondeur fixée, jamais le nombre de vies.

Déclencheur : la première ponte suivant une acclimatation complète dans cette assise. La descente gagne la couche, la ponte la fixe. Le corps ne montre que du définitif.

Deux variantes par couche, selon l'état de l'acclimatation à la fixation : complète (il a séjourné, l'assise l'a fini) ou interrompue (il est parti trop tôt, la marque en garde la trace).

Douze assets pour 64 silhouettes finales. Deux joueurs arrivés à la fin n'ont pas le même corps, et la différence dit comment ils sont descendus — la conséquence visible de la seule décision du jeu.

Contrainte de production : le corps de base doit accepter six paires de points d'ancrage, chacune tolérant deux finitions. À arbitrer avant le premier sprite définitif ; seul poste où une erreur se paie en refonte complète.

10.4 Ce que montrent les autres pontes

12 à 15 pontes pour 6 couches. Les autres s'inscrivent ailleurs :

Le plafond monte d'un cran visible.
L'eau devient plus étrange : la faune permanente change après chaque pic.
Le réseau et les alliances s'étendent, et eux ne régressent pas en effet.

Le corps dit la profondeur, la jauge dit le nombre de vies, l'eau et le réseau disent les deux.

10.5 Trajectoire longue

La ponte est une évolution subie. Décision structurelle, tranchée en v2.3.

Tier 0 §2 distingue deux modes d'évolution : subie — exposition lente et passive à un lieu dense, peu risquée — et forcée — tentative délibérée de franchir son plafond, mortelle dans la plupart des cas. La v1 reprenait de idlepond-tier-2.md la formule « chaque ponte est une évolution forcée survécue », rendue survivable par l'indication. Cette formule est abandonnée, pour une raison qui n'était pas rattrapable : la v2.0 a déplacé l'indication à l'acte final (§12.3), où elle n'est donnée qu'une fois. Rien n'aurait expliqué la survie des pontes 1 à 14, et treize survies consécutives à un acte mortel dans la plupart des cas ne sont pas défendables.

La ponte est donc ce que le §2.4 décrivait déjà mécaniquement : le niveau touche le plafond, condensation, puis divergence. Tier 0 §8 le formule mot pour mot — saturer lentement est la seule voie qui laisse une chance de franchir sans perdre son corps, donc de devenir un demi-dieu vivant plutôt qu'un dieu ou un monstre. C'est exactement la fiche du héros (§2.1) : faible débit, très forte contenance, espèce réservoir. Le jeu accélère un destin déjà écrit ; il ne le force pas.

Trois conséquences :

L'oxymore disparaît. §7.3 dit que le héros ne force jamais son évolution parce que forcer tue. C'est désormais vrai sans exception.
L'indication reprend sa valeur. Elle ne rend pas survivable une routine : elle rend survivable le dernier franchissement, celui qui n'a plus rien de subi (§12.3). Elle est précieuse parce qu'unique.
Le seul geste volontaire du jeu reste la ponte choisie (§10.1) — mais ce que le joueur choisit est le moment, jamais le forçage.

La trajectoire se lit donc ainsi : chaque ponte est un franchissement survécu. Chaque survie le rapproche de la contenance d'un demi-dieu vivant — capable de franchir et de revenir précisément parce qu'il garde un corps localisé.

Il finira dieu par investiture, la quatrième origine du Tier 0 §9. Le slot « héritier saturé » du dépôt reste libre.

[P] La dernière ponte de la série est celle après laquelle il cesse d'être localisé. Matière d'un autre jeu.

11. Ce qui persiste, et la technique

Point de conception majeur, sans lequel la ponte serait punitive : si la population se perd et si la Foi n'achète que des miracles, il faut un axe de persistance qui ne soit ni l'un ni l'autre.

Il existe, il est canonique, et il est gratuit : la technique est de l'information. Elle ne se dépense pas comme un stock, elle fuit, elle s'apprend, elle se transmet. Ce que le héros a appris — et ce que son peuple a appris — ne s'effondre pas avec les galeries.

11.1 Les trois axes de persistance
Axe	Nature	Fondement
Son plafond	Ce qu'il est devenu	Fixation individuelle, Tier 0 §3
Ce qu'il sait	Techniques, méthodes, savoir-faire du peuple	La technique est de l'information, Tier 0 §9
La densité du bassin	Par type, jamais redescendue	Tier 0 §1 et §5

Ce qui régresse : la roche et la population. Rien d'autre.

11.2 La technique en jeu

C'est l'arbre de progression permanent du jeu, et il remplace le Corail de Prestige de l'ancien GDD — avec une différence de nature : il ne s'achète pas, il s'apprend par la pratique.

Ce qu'on apprend	Effet
Creusement	Méthodes de percement, étaiement, drainage de poche
Plomberie sous contrainte	Siphons, cloches à gaz de vase, différentiels de pression, valves
Franchissement d'halocline	Techniques de passage, paliers de décompression chimique
Construction	Temples, portails — et les techniques de portail sont ce qui rend le réseau réouvrable vite
Entretien	Le peuple entretient l'eau pendant qu'il dort — plafond d'heures hors ligne
Conviction	Manières d'approcher une espèce, réduisant le coût de reconviction

Deux propriétés qui la distinguent d'un arbre d'achats :

Elle s'acquiert par l'usage, pas par une monnaie. Creuser beaucoup apprend à creuser.
Elle fuit. [P] Ce que son peuple apprend se transmet aux bassins reliés, et en revient enrichi. Le réseau est aussi un réseau de savoir.
12. La fin, en quatre temps

Chaque temps est une règle du dépôt appliquée. Aucune ressource nouvelle, aucun prêt de puissance.

12.1 L'esprit vient

Ce n'est pas le héros qui monte. Le seuil d'une strate est une contenance, non une porte : il ne peut pas y entrer pour rencontrer quelqu'un. C'est donc l'esprit qui descend.

Il peut agir là : la pierre est dans son type natal, et un dieu ne peut presque rien hors de son type.
Il en a les moyens, et ils viennent du héros. Des générations de convoitise adressée à sa pierre en ont fait un créditeur de ce flux précis. Il répond avec ce que le héros lui a payé sans le savoir.
Le gate est physique. Une pierre élémentaire sature son voisinage depuis des ères. Seul un héros ayant survécu à plusieurs pontes peut s'en approcher sans se défaire. La rencontre est datée par la contenance, pas par un déclencheur scénaristique.

L'appel est le seul adressage délibéré de toute la partie. Il adresse depuis toujours sans le savoir — c'est même ce qui a rendu la rencontre possible (§6.6), et Tier 0 §1 fait de l'orientation, non de l'intention, le seuil de l'adressage. Ce qui change ici est qu'il vise. Il a passé douze à quinze vies à recevoir de l'adressé pendant qu'il était scellé ; ici, il en émet volontairement. Un seul mécanisme canonique, tourné dans les deux sens — c'est ce qui fait que la fin change de verbe sans changer de règle.

[P] Ce que l'esprit veut reste à écrire. Il a un but, et il ne cède rien par amitié. Piste : une place au refuge — un prix déjà consenti, payé par un geste que le joueur a déjà fait cent fois.

12.2 Le relais

L'esprit rend possible ce qui était impossible depuis le premier jour : une communication claire avec la Primordiale.

Elle ne pouvait pas. Se rendre intelligible est un acte, et son coût est considérable pour une entité d'instinct sans réserve locale — elle a coupé la collecte sur ce bassin, donc elle n'a aucun analgésique ici. Elle peut payer une phrase, pas un exposé.
Lui peut. Les strates sont reliées et leurs résidents circulent. Un esprit articulé est exactement le répondeur que la suzeraineté achète.
Le tiers ne traduit pas mieux : il rend l'échange abordable.

Conséquence : le premier vrai dialogue du jeu arrive à la fin, et il arrive par un tiers. Elle a passé la partie entière à pousser un être qu'elle ne pouvait pas atteindre.

12.3 L'indication

Vient alors la seule chose qui rende un franchissement survivable : une indication vers une évolution.

Elle ne coûte aucune substance, mais un effort considérable.
Elle ne garantit rien : aucun dieu n'est omniscient, l'issue reste probabiliste.
Elle lui coûte définitivement le retour sur son investissement, et fabrique le rival qu'elle a accepté depuis le début. Précision nécessaire depuis la v2.2 : elle a coupé la collecte sur ce bassin (§3.1), elle n'en tirait donc aucun revenu courant. Ce qu'elle perd est ce que le reversement devait rapporter plus tard — dieux-et-benedictions.md, reverser : un investissement surveillé, jamais une aumône.

Ce n'est pas un prêt de mana. Un prêt soumettrait le porteur au plafond de contenance — trop béni, on se déforme — et une fenêtre de dépassement confortable contredirait le Tier 0. Ce qu'il reçoit est de l'information, et c'est précisément ce qui la rend précieuse.

Conception du moment : puisque le chemin est unique, la réussite n'est pas en jeu. Ce qui doit être lisible, c'est le prix — le joueur doit voir qu'elle vient de payer quelque chose qu'elle ne récupérera pas.

12.4 Demi-dieu vivant

Il franchit sans perdre son corps.

Il reste localisé, et c'est cette localisation qui lui permet de passer et de revenir.
Il est donc le seul pont entre le monde et les strates, et l'objet politique le plus convoité qui existe.
Son but tient son corps ensemble à travers les franchissements. Un demi-dieu qui perd son objet ne redescend pas : il se défait.

Ce que le joueur emporte : il n'a pas gagné un trône. Il a obtenu le droit de faire l'aller-retour, un créancier de plus, une marraine qui lui parle enfin, et un royaume qui existe. Le titre de dieu des étangs est ce que son peuple dit ; ce qu'il est, c'est un poisson qui peut sortir de l'eau du monde.

12.5 L'antique

[P] Question ouverte, volontairement. Y a-t-il du mana antique au voisinage de la pierre, et le héros peut-il y toucher ? Le lieu correspond exactement à la définition — sous l'eau, dans le noir, là où rien n'est jamais venu puiser. Mais c'est l'enjeu géopolitique de toute la série, et le dépenser dans le jeu 1 le brûle.

Recommandation : le montrer, ne pas le donner. Il le voit, il comprend ce que c'est, il ne peut rien en faire — parce qu'y toucher, c'est le manipuler, donc le détruire en tant qu'antique. La contrainte est physique, pas scénaristique, et elle laisse la ressource intacte pour la suite.

13. Le récit et la voix
13.1 Les paliers de voix

Axe de progression le plus important du jeu : il porte le tutoriel, l'interface et le récit en même temps. Sa lisibilité n'augmente pas parce qu'elle change, mais parce que la contenance du héros augmente — et parce que son coût à elle décroît à mesure que le destinataire grandit. Ce n'est pas seulement de la contenance, c'est un amortissement.

Palier	Ce qu'elle peut	Ce que l'UI montre	Déclencheur
La pente	Aucun mot. Une direction, une attirance vers le fond.	Aucun chiffre. Une jauge sans graduation, une lueur.	Début
Les signes	Des indices qui cessent d'être ambigus. Elle indique.	Le lexique apparaît. Barres graduées, ordres de grandeur.	1ʳᵉ ponte
Les directives	Des phrases courtes et claires : des ordres, des noms, des mises en garde.	Chiffres complets, débits, taux d'acclimatation.	3ᵉ ponte
Le dialogue	Tout : vassalité, tribut, domaine, les autres bassins, puis l'échelle continentale.	—	Le relais (§12.2)

Le point à ne pas perdre : elle est franche dès qu'il l'entend. Elle ne cache rien, elle n'a simplement pas les moyens d'expliquer un système. Se rendre intelligible est un acte, son coût est considérable, et elle n'a pas d'analgésique local puisqu'elle a coupé la collecte. Elle peut payer une phrase, pas un exposé.

Le piège à éviter, et il est nommé au canon : le palier Directives donne au joueur l'illusion d'un interlocuteur qui pourrait tout expliquer s'il le voulait. C'est faux, et il ne faut pas le corriger. Elle est directe et pauvre, pas évasive. Aucune ligne de dialogue ne doit suggérer qu'elle retient une information : elle en a les mots, jamais le budget.

Le palier de voix courant fixe le registre des entrées de succès, définitivement (§14.5). Une entrée obtenue sous la pente reste rédigée dans la langue de la pente, quel que soit le moment où le joueur la relit. La bibliothèque devient ainsi la preuve du chemin parcouru, à coût de production nul.

Entre les directives et le dialogue, l'escalade est portée par la spécificité (elle nomme des espèces, puis des lieux, puis ses propres erreurs) et par les égarés, jamais par de l'explication.

13.2 La révélation en deux temps, et elle arrive tard

Les deux temps sont dans le même échange final, non répartis sur la partie.

Au relais — l'esprit rend l'échange abordable, et elle explique le système sans rien cacher : ce qu'elle est, ce qu'elle attend, le tribut, le domaine, et le fait que d'autres tiennent déjà les montagnes, les forêts, les plaines, la mer côtière.
Puis, dans la même conversation — la correction d'échelle. Ce n'est pas le monde, c'est ce continent. D'autres entités comme elle existent ailleurs, qu'elle n'a jamais rencontrées.

Le second temps est le vrai coup, et il ne révèle aucun mensonge : il révèle une petitesse. Le héros n'est pas promu au sommet du monde ; il est recruté par une puissance régionale, sur un continent parmi d'autres. Rien n'était faux. Tout était plus petit.

13.3 Les figures

Le héros. Faible producteur, contenance énorme. Négligeable comme carburant, redoutable comme individu. Il n'a jamais rien détruit. Il ne veut pas régner, il veut qu'un endroit existe.

La Primordiale. Condensat d'intention instinctive — elle n'a jamais été localisée, jamais été personne, et n'a donc aucun corps à perdre. Portée continentale. Capital sans revenu. Elle ne délègue pas par bienveillance : elle franchise, et elle le fait depuis des éons partout sur son continent. Il n'est pas le premier ; il est le premier ici.

Deux choses à ne jamais confondre :

	Ce qu'elle peut	Ce qu'elle ne peut pas
Percevoir	Elle voit par tout vivant d'instinct, y compris dans les cités : il y pousse de l'herbe.	Voir là où rien ne vit — roche scellée, mer relique, fond stérile.
Recevoir	—	Répondre. Or la réciprocité concentre : ce qui répond capte du dirigé, ce qui reste muet ne reçoit qu'un fond diffus.

Elle n'est donc pas injoignable — elle est inconcentrable. Un dieu est par définition un être qui a cessé d'être localisé ; si la délocalisation coupait la réception, aucun dieu ne recevrait rien et le capital des strates, qui est de l'adressé accumulé, n'existerait pas.

C'est ce qui donne leur raison d'être au temple, à la bouche, au relais, au vassal : des dispositifs pour transformer un filet en flux.

Le fond de la mare est un de ses angles morts. Elle ne l'envoie pas vers ce qu'elle connaît : elle l'envoie voir.

La rivalité, et sur quoi elle porte. Pas le territoire — il n'y a pas de pénurie de bassins. La concentration de la réponse. Elle voit tout et ne sait pas répondre ; lui répond, donc capte du dirigé, dans un réseau qu'elle a franchisé. Et son but final touche l'instinct lui-même, son domaine propre, pour y installer une adresse vers son refuge — l'acte le plus concurrentiel que le dépôt autorise. Le Tier 0 l'avait prédit : chaque indication accordée fabrique un rival possible. Elle l'a accepté en connaissance de cause. Il l'ignore.

Ne pas adoucir ce point. « Il ne prend pas sa place » est vrai à court terme et faux à terme, et cet écart est le moteur dramatique de la série. Elle ne redoute pas son ambition : elle voit sa concentration monter.

L'esprit. Sorti d'un œuf dont la pierre est le reste. Il réside dans une strate de son type avec ses pairs, et n'est jamais revenu — absent, non mort et non endormi. Il a pourtant reçu, pendant des générations, une convoitise dont il ignorait la source. Il ne cherchait rien ici. Il vient parce qu'on l'a appelé avec assez de mana pour que ce soit intéressant, et il reste parce qu'il est curieux. Il a un but. Il ne cède rien par amitié.

[P] Statut : candidat au slot canonique du condensat de lieu. Promotion à décider et à dater — jamais par usage.

Les égarés. Reliques vivantes remontées par la fissure. Premier peuple du refuge, et la preuve que l'en-dessous existe.

Le peuple. Des rejetés, des franchissements interrompus, des fidèles arrêtés qui prient encore et ne montent plus. Aucun héros parmi eux.

13.4 Ce que le jeu ne dit jamais avant l'heure
Que la pente vient d'une pierre, et que l'eau de sa mare en descend.
Que son bassin est défiscalisé, et que sa chance n'en est pas une.
Qu'il existe un extérieur, un continent, d'autres bassins franchisés, des prédécesseurs.
Qu'il est un rival. Elle le sait ; lui l'ignore, et il ne s'en formalisera pas.
Ce qu'il en coûte à la Primordiale de se faire comprendre. Elle ne s'en plaint pas.
13.5 Chemin unique

IdlePond est une chronique, pas un dilemme. Le joueur ne choisit pas s'il accepte : il découvre progressivement ce qu'il a déjà accepté. La ponte n'est pas une décision morale, c'est un chapitre. Aucune fin alternative.

Sa seule vraie décision, du début à la fin, est combien de temps rester.

14. Les succès et la bibliothèque
14.1 Principe : on ne récompense pas, on constate

Un succès n'est pas une récompense accordée pour un comportement. C'est le moment où le héros ou son peuple comprend quelque chose que le monde a déjà fait.

La distinction est canonique, pas cosmétique. Tier 0 §1 : la magie n'est pas le mana, c'est le nom donné aux techniques d'utilisation. Et §11 : la technique est de l'information, elle s'apprend par la pratique. Donc :

Le gain existe déjà physiquement. Le succès est ce qui le rend exploitable.

La divergence a eu lieu, la densité est montée, le peuple a répété un geste jusqu'à ce qu'il marche. Le succès consigne l'observation, et l'observation vaut technique.

Conséquence de conception, absolue : un succès ne se réclame jamais. Aucun bouton, aucune fenêtre bloquante, aucune monnaie versée. L'effet s'applique au déclenchement, et aller lire le détail est facultatif.

Ce qui règle du même coup le conflit avec le pilier de §1.3 — un palier lisible seulement en chiffres est un multiplicateur, on le retire. Le succès n'est pas un palier et ne fabrique aucun gain : il en documente un.

14.2 Les trois familles
Famille	Déclencheur	Fréquence	Rôle
Franchissement	Ponte, couche fixée, acclimatation complétée, fin d'assise	~1 par cycle	Porte les chapitres
Seuil	10/25/50/100 individus, palier saturé, divergence observée, premier égaré recueilli	Continue	Porte la cadence courte
Acte	Premier temple, premier portail, premier miracle, première reconviction	Une fois chacun	Porte le tutoriel

Les trois familles sont typées dans les données dès la première ligne de code. Elles n'ont ni la même fréquence, ni le même coût de production, ni le même régime de visibilité (§14.6) — un pot commun devient illisible dès l'assise II, et ce typage ne se rétrofite pas.

Règle de production qui en découle :

Famille	Volume	Mode d'écriture
Acte	Stock fini et petit, ~30 sur la partie	Écrits à la main, une fois
Franchissement	Indexé sur pontes × assises, donc borné et prévisible	Écrits à la main
Seuil	La masse	Générés par gabarit, sinon le poste devient inatteignable pour un développeur seul
14.3 L'effet : chiffre ou verbe

Deux natures d'effet. Le choix se fait au cas par cas, avec un défaut orientatif par famille — sans défaut, la tendance de production est de tout chiffrer, parce que c'est moins cher.

Nature	Ce que c'est	Défaut
Chiffre	Un terme dans une formule : rendement, coût, réduction_technique (§6.4)	Défaut des seuils
Verbe	Le jeu fait désormais quelque chose tout seul, ou quelque chose devient possible	Défaut des franchissements

Les verbes sont le canal d'automatisation du jeu. C'est par eux que la reconviction, l'élargissement et la redescente cessent d'être manuels au fil des cycles. Cadence cible : environ un verbe par ponte, des chiffres entre.

Le prix de l'effet silencieux

Un effet appliqué sans être réclamé signifie que le joueur voit ses chiffres bouger sans savoir pourquoi. Dans ce genre, une variation inexpliquée du revenu est une source d'anxiété réelle : les joueurs auditent leurs multiplicateurs. Deux contreparties, obligatoires :

Notification discrète et non bloquante au déclenchement — une ligne qui apparaît et s'efface. Jamais une fenêtre.
Détail de la captation consultable, où chaque terme actif est attribuable à sa source. C'est l'équivalent de d'où viennent mes ×2, et c'est ce qui rend le système auditable sans le rendre obligatoire.

Traitement distinct des verbes. Un chiffre modifie un taux ; un verbe modifie ce que le jeu fait sans le joueur, et un joueur qui ne remarque pas que la reconviction est devenue automatique continue de la faire à la main. Les verbes apparaissent donc dans la notification et dans l'écran de retour ; les chiffres seulement dans le détail de captation.

14.4 Persistance

Un succès est acquis définitivement. Il ne se perd jamais, y compris à la ponte.

Fondement : Tier 0 §3, un être surévolué conserve ses acquis à vie, en tout lieu — et §11, qui range la technique parmi les trois axes de persistance.

Effet recherché : la bibliothèque devient l'inventaire visible de l'axe « ce qu'il sait », aujourd'hui le plus important et le plus abstrait des trois. Le joueur ne lit plus une ligne de tableau, il consulte une collection.

Les succès ne sont pas re-déclenchables. Un succès marque la première fois. Les répétitions d'un même seuil après une ponte alimentent le journal mais ne redonnent aucun effet — sinon le gain devient une rente indexée sur le nombre de pontes, et la courbe casse.

14.5 Le narrateur, et ce qu'il n'a pas le droit de faire

Forme : du texte dans l'interface. Aucune voix, aucune incarnation, aucune ligne de dialogue, aucun portrait.

Statut : la partie est techniquement une histoire déjà arrivée. Le texte est donc une chronique rétrospective, et le narrateur sait ce qui s'est passé parce que c'est passé. Ce cadrage évite d'avoir à justifier une voix système hors registre, et il interdit d'employer celle de la Primordiale — qui ne peut pas payer un exposé (§13.1).

Le registre est figé au déclenchement

Une entrée est rédigée dans la langue que le héros avait au moment où il l'a obtenue, et n'est jamais réécrite.

Le registre est fixé par le palier de voix courant au déclenchement (§13.1), jamais par l'assise concernée. Un seuil de l'assise I atteint au cycle 10 est donc rédigé en langue tardive — ce qui est correct, puisque c'est bien à ce moment-là qu'il a été observé.

Palier de voix	Registre de l'entrée
La pente	Un geste qui marche, aucune théorie. Le peuple nomme sans comprendre.
Les signes	Des ordres de grandeur, des comparaisons, un vocabulaire propre au bassin.
Les directives	Des termes justes, des mesures. La cause reste hors de portée.
Le dialogue	Précis, et rétrospectivement lucide sur ce qui a été mal compris.

Trois bénéfices, à coût de production nul :

Aucun texte n'est écrit deux fois. Le volume ne bouge pas d'une ligne.
La bibliothèque devient la preuve du chemin parcouru : le joueur qui relit ses vieilles entrées voit à quel point il se trompait, et personne n'a eu besoin de le lui dire.
Les explications fausses de l'assise I sont canoniques, pas une facilité — Tier 0 §8 : les maîtres enseignent une méthode, les morts ne les contredisent pas. Une science populaire d'étang, avec ses superstitions utiles.
La limite, et elle est dure

Le narrateur rapporte ce qui est arrivé et ce que ça a changé. Il n'explique jamais pourquoi le monde fonctionne ainsi.

Il constate : la faune a changé, le peuple a appris à faire ceci, l'eau n'est pas redevenue ce qu'elle était. Il ne commente pas les causes.

Sans cette règle, une seule phrase de succès grille l'un des cinq secrets de §13.4 ou le retournement d'échelle de §13.2 — qui est le meilleur moment du jeu. C'est aussi ce qui protège la numérisation progressive de l'UI : un narrateur qui livre la vraie physique annule le principe nommé sans être compris (§1.3).

14.6 Visibilité : trois états
État	Ce que le joueur voit	Familles concernées
Ouvert	Nom + condition + barre de progression	Seuils, actes
Fermé	Nom seul, condition masquée	Franchissements
Secret	Emplacement vide. Rien d'autre	Voir critère ci-dessous

Pourquoi ce découpage. Les seuils et les actes sont mécaniques et sans contenu narratif — convaincre 25 individus, bâtir un premier temple. Les afficher entièrement ne divulgue rien et donne au joueur sa liste d'objectifs, l'un des moteurs de rétention les plus efficaces du genre. Les franchissements portent l'histoire : leur nom seul crée l'attente sans dire ce qui arrive.

Critère du secret, mécanique et applicable en production par n'importe qui :

Est secret tout succès dont le nom seul révélerait un élément de §13.4 — la pierre et l'origine de l'eau, la défiscalisation, l'existence d'un extérieur et d'un continent, sa rivalité avec la Primordiale, ce qu'il en coûte à celle-ci de se faire comprendre.

Verrouillage par assise. Un succès n'est listé, quel que soit son état, que lorsque son assise est atteinte. Une assise non atteinte n'affiche rien du tout, pas même un compte. Sans cette règle, la bibliothèque annonce à l'heure 1 qu'il existe une mer relique et une pierre.

Compteur de secrets : pas de compteur global. Des emplacements vides par assise, visibles seulement une fois l'assise atteinte. Le joueur voit qu'il lui manque trois choses ici, sans jamais apprendre combien il en existe au total, ni qu'il existe une assise VI.

Barre de progression sur les seuils ouverts. Motivateur le moins cher du genre, et déjà natif au système (10/25/50/100, §7.2).

14.7 Cadence

Les succès découlent du contenu : ils ne sont pas définis par un quota posé d'avance. Une exception, et une seule.

Plancher garanti sur l'assise I

La première session est le seul moment où l'absence de récompense se paie par un abandon définitif. Elle ne peut pas être laissée à l'émergence.

Garantie	Valeur
Premier succès	Dans les deux premières minutes — première espèce convaincue ou premier palier ouvert
Première demi-heure	Un déclenchement toutes les 3 à 5 minutes
Première ponte	Un franchissement, obligatoirement — c'est le premier vrai chapitre

Ce plancher est presque gratuit : 3 espèces (§19.1) × 4 seuils = 12 succès générés par gabarit, plus 6 ouvertures de palier et 4 à 5 actes de tutoriel. Une vingtaine de déclenchements sur le premier cycle, sans effort d'écriture original.

Et surtout : mesurer

Ajouter à la télémétrie (§18.4) : intervalle entre deux succès, par cycle. C'est la métrique qui détectera l'assèchement de la mi-partie — le risque « plateau de la voix » de §16.4, enfin quantifiable.

14.8 La bibliothèque

Écran séparé du journal. Le journal raconte, la bibliothèque inventorie : deux usages distincts, on lit l'un et on consulte l'autre.

Liste des succès obtenus, fermés et secrets (§14.6), gatée par assise.
Sélection d'une entrée → détail : le texte figé au registre d'obtention, la condition, et l'effet exact, chiffré.
C'est le lieu de l'audit réclamé au §14.3 : le détail de la captation renvoie ici pour chaque terme.

Filtre, pas onglets. Au premier cycle la collection est petite, et trois onglets à moitié vides donnent une impression de vide. Un filtre par famille, réglé sur « tout » par défaut, qui devient utile quand le volume arrive.

14.9 Schéma de données

Le typage par famille, l'état de visibilité et le registre figé sont exactement ce qui ne se rétrofite pas. À poser avant la première ligne de contenu.

typescript
interface Succes {
  id: SuccesId;
  famille: 'franchissement' | 'seuil' | 'acte';
  assise: AssiseId;                        // gate d'affichage (§14.6)
  visibilite: 'ouvert' | 'ferme' | 'secret';
  condition: Condition;                    // affichée si 'ouvert'
  effet:
    | { nature: 'chiffre'; cible: TermeDeFormule; valeur: number }
    | { nature: 'verbe'; capacite: CapaciteId };
  texte: string;                           // figé, jamais réécrit
}
15. Direction artistique
15.1 Le corps par accumulation

Un corps de base sur lequel s'ajoutent des marques, une par assise fixée : branchies, membranes, luminescence, minéralisation, épaississement. La divergence se lit comme une somme d'histoire, pas comme un remplacement de sprite. Système indexé par type de mana, donc réutilisable pour tout le bestiaire de l'univers — candidat sérieux à une promotion Tier 1 après le jeu 1.

15.2 Registre
Dominante vitale. Les espèces des paliers saturés paraissent increvables, pas magiques.
Pas de combustion sous l'eau. Le registre steampunk passe par la plomberie sous contrainte : siphons, cloches à gaz de vase, différentiels de pression, courants domestiqués, valves, manomètres, joints qui lâchent.
La lumière est la variable de progression la plus lisible. Elle décroît continûment jusqu'à ce que la seule lumière restante soit celle que le mana produit.
Interface : effets avant chiffres, numérisation progressive au rythme de la voix.
L'inversion d'échelle est le cœur de la DA. Vue de la berge : une flaque entre les racines.
15.3 À produire, et c'est bloquant
Anatomie du corps de base et points d'ancrage des couches.
Silhouette d'un temple par élément, et lisibilité du palier chargé qui en découle.
Apparence d'un portail — tunnel entre deux eaux — et signalétique du réseau.
Palette par assise, et courbe de lumière.
15.4 Ce que l'ancienne DA doit perdre

L'imagerie d'Étang des Merveilles — or, arcs-en-ciel permanents, larme de lune, nexus cosmique, poisson céleste, dimension quantique — est incompatible avec le registre vital et avec un univers où aucun dieu n'est bienveillant. Les palettes des zones 0 à 6 restent partiellement utilisables ; les zones 7 à 29 sont hors registre.

16. Progression et équilibrage
16.1 Ce qui est fixé
Paramètre	Valeur	Statut
Assises	6	Fixé (§6.2) — six au papier, engagement par assise en production
Paliers	6/8/10/12/14/16 = 66	Proposé
Pontes pour une partie	12 à 15	Cible, susceptible d'être réduite par [P20]
Couches	6, deux variantes chacune	Fixé
Fraction d'un cycle en redescente	20 à 25 %	Cible dure
Coût de reconviction	Fonction de la densité locale du type supporté	Fixé
Acclimatation au retour	100 %	Fixé (canon)
Captation	Deux canaux additifs	Fixé (canon)
Borne du canal acclimaté	Part du type mûr, non densité absolue	Fixé (§3.0)
Nature de la ponte	Évolution subie	Fixé (§10.5)
Vitesse de maturation d'un palier	—	[P29]
Hors ligne	Plafonné en heures, aucune perte	Fixé
Foi	Miracles et effort uniquement	Fixé (canon)
Succès	Constatés, jamais réclamés ; acquis définitivement	Fixé (§14)
Coût de redescente	coût_base × f × réduction_technique	Forme fixée, valeurs [P5]
f — fraction du coût d'origine	—	[P5]
k — taux de repeuplement après ponte	—	[P6], pilote réel des 20–25 %
16.2 Courbe cible

Provisoire, à valider par simulation avant production des assises III+.

Cycle	Profondeur	Durée	Dont redescente
1	I → II	2–4 h	—
2–3	II → III	4–8 h	~20 %
4–6	III → IV	8–20 h	~20 %
7–10	IV → V	1–3 jours	~25 %
11–13	V → VI	3–7 jours	~25 %
14–15	VI, et la fin	1–2 semaines	~25 %
16.3 Ordre de calibrage
Assise I et ses six paliers seulement.
Mesurer la durée du cycle 1 avec la vraie boucle.
En déduire le nombre de paliers des assises II à VI sous la contrainte des 20–25 %.
Portails et temples après — ils changent la forme de la courbe, pas sa pente initiale. 4 bis. Aucune assise n'est produite avant que la précédente ait été mesurée (§6.2, règle d'engagement). L'arbitrage de durée [P20] se fait sur les cycles III à V, une fois la durée réelle du cycle 1 connue.
Foi et miracles en dernier : ils n'ont aucun effet sur la vitesse, donc ne peuvent pas casser la courbe.
16.4 Risques
Risque	Description	Mitigation
La redescente devient le jeu	~900 franchissements de palier sur une partie. À 40 %, c'est une corvée.	Cible dure de 25 %, mesurée en télémétrie à chaque cycle. Métrique n° 1.
La Foi paraît inutile	Minuscule, et elle n'achète pas de puissance.	Elle est le seul accès à cinq actions impossibles autrement, et le seul analgésique. Le lien doit être montré, pas seulement vrai.
Les temples deviennent une liste de déblocages	C'est le mode d'échec naturel du système.	Le prix est payé par l'allié, chaque allié a un but, et le dosage n'est pas dans les mains du joueur.
La mare devient un panthéon	Les portails fournissent des adresses ; sans frein, le royaume se remplit de créanciers et perd sa marge.	Frein émergent, pas de plafond : chaque temple redirige définitivement une part du revenu de Foi (§9.3). Et la plupart des adresses ne trouvent aucun dieu disposé (§9.4).
Les portails deviennent un multiplicateur	« Un bassin de plus = +x % ».	Coût bilatéral, enrichissement préalable obligatoire, et ce qu'ils apportent est de la population et des adresses, pas du rendement.
Le plateau de la voix	Plusieurs pontes entre les directives et le dialogue.	Les égarés portent l'escalade, et surtout les succès portent la cadence continue (§14). Métrique dédiée : intervalle entre deux succès, par cycle (§18.4). C'est la première mitigation mesurable de ce risque ; la v2.1 n'avait que « à vérifier en playtest », ce qui n'en est pas une.
La ponte non choisie frustre	Un joueur inattentif se fait pondre.	Alerte à 85 %, captation coupée à 100 %, délai. Pénalité douce.
L'assise VI coûte trop cher pour sa durée	Poste d'assets le plus lourd, consommé en fin de partie.	Fuite de faune dès IV–V, et deux étages qui portent toute la fin.
Le hors ligne rend le jeu passif	Si l'absence est le meilleur moteur, pourquoi jouer ?	Elle produit du mana ; l'acquis de séjour dépend de la densité, donc de la profondeur, donc du jeu.
16.5 Question ouverte : y a-t-il une machine ?

Tier 0 §5 fait de la machine le seul puits existant. Introduire un puits dans un jeu dont toute la boucle est une escalade est un choix de fin de partie, pas un bâtiment.

[P] Recommandation : non dans le jeu 1. La mare n'a jamais vu de machine, personne à l'extérieur ne sait la situer, et l'absence de puits est ce qui rend la densité crédible. Une machine serait la matière d'un jeu ultérieur — et la meilleure menace possible pour une suite.

17. Monétisation
17.1 Le problème, énoncé franchement

L'ancien modèle reposait sur les Gemmes : arbre premium à 4 200 Gemmes, boosts délivrant X heures de production, Poisson Céleste à 500 Gemmes. Structurellement incompatible avec le canon, pour une raison de règle et non de goût :

Vendre du rendement ou des heures de production vend de la puissance, or forcer son plafond tue. Un raccourci acheté contredit l'invariant central.
Le mana ne s'échange pas, et la Foi n'achète que des miracles. Une monnaie premium recréerait une troisième monnaie qu'on a écartée.
Une boutique qui vend de l'accélération, dans un jeu dont le thème est rien ne se détruit et tout revient plus vite de soi-même, travaille contre son propre récit.

Gemmes et Perles sont supprimées, pas converties.

17.2 Ce qui reste vendable
Catégorie	Compatible ?
Cosmétique de corps — finitions alternatives des couches, sans effet	Oui
Cosmétique de lieu — ouvrages, monuments, plomberie décorative	Oui
Confort d'interface — navigation multi-palier, statistiques, historique des cycles	Oui
Soutien — achat fondateur, crédit au générique, nommer une espèce	Oui
Accélération — boosts, réductions de coût, rachat automatique	Non
17.3 Statut

Non résolu, et honnêtement insuffisant en l'état. Trois modèles à arbitrer : premium à l'achat, cosmétique pur en F2P, ou épisodique sur une série dont l'univers est déjà écrit — ce qui est factuellement l'actif principal du projet.

À décider après le prototype, quand la durée réelle d'une partie sera connue : c'est elle qui détermine si le jeu se vend une fois ou se soutient dans le temps. Ce qui est décidé en revanche : aucune monétisation ne vendra de puissance, et c'est une contrainte de canon, pas une position à renégocier au moment du budget.

18. Architecture technique et migration
18.1 Stack

Inchangée — principal actif récupérable du projet précédent.

React 19 + TypeScript · Vite · Phaser 3 · Zustand 5 + persist · break_infinity.js · Tailwind 4.

18.2 Ce qui se garde du code existant
Élément	Réutilisation
GameLoopManager (tick 100 ms)	Direct. Recalculer les deux canaux, croissance de population, acquis de séjour, densité.
OfflineManager	Direct. Le cap 24 h devient un cap débloqué par technique.
Sérialisation Decimal	Direct.
PondScene : monde vertical, scroll, palettes par zone	Réécrire — la v2.1 disait « adapter ». Le code existant gère 8 zones visuelles ; la cible est 66 paliers et 6 palettes d'assise. Ce n'est pas une adaptation.
Système de jalons	Convertir en seuils de peuplement.
Événements narratifs ambiants	Garder le mécanisme, réécrire le pool : le registre a changé.
Journal / Bestiaire	Garder, en scindant le bestiaire en deux volets.
Succès	Correction v2.2 — périmé. « Garder le mécanisme, changer la récompense » ne tient plus : les succès deviennent un système de plein droit, porteur d'effets et de la distribution narrative continue. Voir §14. Le mécanisme de déclenchement reste réutilisable ; la structure de données est à refaire.
18.3 Ce qui se supprime

gemmes, perles, research.ts, pearlUpgrades.ts, prestigeUpgrades.ts, PearlMarket.tsx, Research.tsx, PrestigeUpgrades.tsx, BoostOverlay.tsx, challenges.ts, Challenges.tsx, buyFish et toute la logique d'achat par exemplaire, Shop.tsx.

Sur les défis quotidiens : ils récompensaient la présence quotidienne, ce qui contredit le pilier « ne jamais punir l'absence ». Supprimés sans remplacement.

18.4 Ce qui est à construire
Module	Priorité
Modèle de densité — par palier et par type, monotone croissante, persistante	P0
Jauge — niveau, plafond, acquis de séjour, saturation, divergence non choisie	P0
Captation à deux canaux — natif fonction de la population vivante, acclimaté fonction du séjour	P0
Acclimatation — rendement par assise, double régime, retour à 100 %	P0
Population — conviction, croissance, seuils, débit	P0
Ponte — reset sélectif, franchissement, encaissement de Foi, fixation de couche	P0
Technique — acquisition par l'usage, effets permanents	P1
Foi et effort — réserve, miracles probabilistes, usure quand la réserve est vide	P1
Corps en couches — 6 emplacements × 2 variantes	P1
Voix — machine à états à 4 paliers, pilotant la numérisation de l'UI	P1
Portails — coût bilatéral, migration, densité distante persistante	P2
Temples — adresses, alliés, charge de zone par type, table de divergence	P2
Succès et bibliothèque — trois familles, effets chiffre/verbe appliqués en silence, registre figé, trois états de visibilité	P1 — porte la cadence de la première session, donc la rétention
Détail de la captation — chaque terme actif attribuable à sa source	P1 — contrepartie obligatoire de l'effet silencieux (§14.3)
Flux adressé visible — perçu en permanence, encaissé seulement à la ponte	P2 — rend lisible le frein des temples (§9.3)
Télémétrie — fraction de cycle en redescente, durée par cycle, palier de blocage, intervalle entre deux succès par cycle	P1
18.5 État global cible
typescript
interface GameState {
  // Héros
  niveau: Decimal;
  plafond: Decimal;
  acquisDeSejour: Decimal;
  fatigue: number;                            // usure quand la Foi est vide
  couches: Array<{ assise: number; variante: 'complete' | 'interrompue' }>;

  // Monde
  densite: Record<TypeMana, Decimal>;         // ne décroît jamais
  paliersOuverts: number;
  capacite: Record<PalierId, number>;
  populations: Array<{ espece: EspeceId; palier: PalierId; effectif: number }>;
  acclimatations: Record<AssiseId, { rendement: number; complete: boolean }>;

  // Économie
  mana: Decimal;
  foi: number;                                // minuscule par construction

  // Persistance
  techniques: string[];
  densiteDistante: Record<BassinId, Decimal>; // survit à la ponte
  alliances: Array<{ entite: EntiteId; dette: string }>;

  // Réseau et politique
  liaisons: BassinId[];                       // s'effondrent à la ponte
  temples: Array<{ palier: PalierId; entite: EntiteId; actif: boolean }>;
  chargeDePalier: Record<PalierId, TypeMana>; // l'effet survit

  // Récit
  pontes: number;
  palierDeVoix: 'pente' | 'signes' | 'directives' | 'dialogue';
  succes: Record<SuccesId, {                  // survit à la ponte
    obtenuAuCycle: number;
    registre: PalierDeVoix;                   // fige le registre du texte
  }>;

  // Système
  lastSaveTime: number;
  capHorsLigneHeures: number;                 // débloqué par technique
}
19. Roadmap
19.1 Prototype — la boucle nue

Objet unique : mesurer la fraction de redescente et la durée d'un cycle.

Assise I, six paliers, trois espèces.
Jauge complète, saturation incluse.
Captation à deux canaux.
Ponte fonctionnelle : reset, franchissement, Foi encaissée mais non dépensable.
Densité persistante.
Succès de l'assise I, avec le plancher de cadence de §14.7. Ce n'est pas de la finition : le plancher est un objet de mesure, au même titre que la fraction de redescente.
Aménagement à la redescente, pour mesurer f et k ensemble.
Aucun art définitif, aucun texte définitif.
19.2 v0.2 — le cycle vertueux
Assise II, acclimatation et son double régime.
Reconviction par la densité.
Technique : premières acquisitions par l'usage.
Paliers de voix 1 et 2, avec numérisation de l'UI.
Télémétrie.
19.3 v0.3 — la politique
Assises III et IV, égarés, fidèles arrêtés.
Portails : deux bassins, coût bilatéral, migration.
Temples : un allié, une charge de palier, une faune qui diverge.
Foi et miracles.
Corps en couches, deux premières paires.
19.4 v1.0 — la chronique complète
Assises V et VI, mer relique, pierre.
Fin en quatre temps.
Six paires de couches, bestiaire à deux volets, journal complet.
19.5 Priorités immédiates
Trancher [P2] et [P3] — la borne du canal acclimaté et la nature de la ponte. Ce sont les deux seuls blocages structurels du document ; tout calibrage fait avant eux est à refaire.
Mettre idlepond-tier-2.md à jour : quatre paliers de voix, dépendance Tier 0 v0.14, guide reclassé en condensat d'intention instinctive. Le fichier est en v0.1 et contredit le présent document sur trois points.
Porter la demande de précision Tier 0 §8 sur l'acquis de séjour (§2.3). Un [P] de Tier 2 ne peut pas réinterpréter un invariant : le séjour est nécessaire mais non suffisant doit être inscrit au Tier 0, ou refusé.
Passe de conformité lexicale sur le reste du dépôt (« strate » pour de la roche, milieu, zone). Faite dans le présent document en v2.2, pas ailleurs.
Anatomie du corps de base et points d'ancrage — bloque le premier sprite.
Conventions phonétiques — bloque tout nom d'assise, d'espèce, de bâtiment.
Simulation de la courbe sur trois cycles, f et k conjointement, avant production de l'assise III.
Porter l'extension [P] sur la condition d'épuisement de la scellure (§4.2).
Annexe A — Glossaire
Terme	Définition
Assise	Subdivision majeure. Six. Assets complets et type de mana propre.
Palier	Subdivision d'une assise. Modulateurs seulement.
Strate	Réservé au plan des dieux. Interdit pour de la roche.
Couche	Marque permanente sur le corps, une par assise fixée, deux variantes.
Niveau / Plafond	Ce qu'il contient / ce qu'il peut contenir sans danger.
Acquis de séjour [P]	Capacité à franchir, construite par le séjour, convertie à la ponte.
Contenance	Propriété du héros, croissante, sans maximum connu.
Canal natif / acclimaté	Les deux sources de captation, additives.
Signature vive	Degré 1 de la chaîne. Se fond dans l'ambiant en quelques jours. Type natal du héros.
Ponte	Prestige. Il se scelle dans un œuf et expire tout son mana. Une évolution subie, survécue parce que lente (Tier 0 §8).
Divergence non choisie	Ponte déclenchée seule par saturation. Fixe moins d'acquis. Jamais « forcée » : le mot est pris au Tier 0 §2 par le cas inverse.
Part mûre	Fraction du type mûr dans la charge d'un palier. Seule entrée du canal acclimaté. Peupler la dilue.
Acclimatation	Rendement dans un type non natal. Technique sans ponte, évolution après.
Reconviction	Reconstitution de la population après une ponte.
Densité	Charge de mana d'un lieu, par type. Ne redescend jamais.
Défiscalisation	La collecte est coupée sur ce bassin : la part adressée reste sur place.
Mana	Monnaie courante. Captée, jamais achetée.
La Foi [P]	Réserve d'adressé encaissée dans l'œuf. Miracles et effort. Minuscule.
Miracle	Action impossible au mana. Coûte de la Foi, et ne garantit rien.
Effort	Coût de tout acte au-delà de l'ordinaire. Porté par la Foi, ou par lui.
Adresse	Terme canonique (Tier 0 §1). Orientation qui désigne un destinataire.
Réciprocité	Ce qui répond capte du dirigé ; ce qui reste muet reçoit un fond diffus.
Portail	Tunnel entre deux plans d'eau. Transporte du vivant. Coût bilatéral.
Temple	Non un hommage : une adresse locale. Charge la zone par la dépense de l'allié, et redirige définitivement une part du revenu de Foi.
Égarés	Reliques vivantes remontées de la mer relique par la suintance.
Fidèles arrêtés	Ceux qui ont tenté, qui prient encore, et qui ne montent plus.
Mer relique	Bassin marin scellé, mûri jusqu'à l'élémentaire. Pas l'océan.
Pierre élémentaire	Reste d'œuf d'esprit. Ne produit rien, condense indéfiniment. Une adresse.
La Primordiale	Condensat d'intention instinctive, jamais localisée, portée continentale. Inconcentrable, non injoignable.
Demi-dieu vivant	Contenance franchie, corps conservé, donc encore localisé. Seul pont vers les strates.
Aménager	Rendre un palier habitable pour du vivant ordinaire. Puits principal de la redescente.
Succès	Non une récompense : le moment où le héros ou son peuple comprend ce que le monde a déjà fait. Vaut technique. Constaté, jamais réclamé.
Bibliothèque	Écran d'inventaire des succès — ouverts, fermés, secrets. Distinct du journal, qui raconte.
Registre figé	Une entrée de succès est rédigée dans la langue du palier de voix courant, et n'est jamais réécrite.
Annexe B — Ce qui change depuis le GDD v1.1, et pourquoi

Le v1.1 a été rédigé sans accès au Tier 0, au Tier 1, ni à idlepond-tier-2.md v0.7. Table de correction complète.

Point v1.1	Statut	Motif
L'eau pure comme ressource terminale (§9 entière)	Supprimé	Elle n'a rien à faire. L'eau de la pierre est déjà pure au registre antique. Toute fabrication de pureté est un raffinage, donc un puits. Le terminus est une contenance, pas une barre à remplir.
« Dédier du mana pour produire de la pureté »	Supprimé	Contredit mana-typologie.md : la pureté est une absence de manipulation, et l'antique n'est pas raffinable.
La Foi n'achète que du permanent au refuge	Faux, corrigé	Le Tier 2 dit miracles, et rien d'autre, et le Tier 1 en fait d'abord un analgésique portant l'effort. Ma règle supprimait l'arbitrage agir/s'user, et la Foi est minuscule : un arbre d'achats est exclu.
Le refuge comme bâti permanent à achats en Foi	Supprimé	Je le justifiais par « peuplé et non creusé » ; or la population aussi se perd. La persistance passe par le savoir, la densité et le plafond (§11).
Cap hors ligne acheté en Foi	Corrigé	Ce n'est pas un miracle. Déplacé vers la technique.
Prêt de puissance / dépassement de plafond lisible	Supprimé	Un prêt de mana soumet le porteur au plafond : trop béni, on se déforme. Le don final est une indication, transmise par le relais.
Captation comme produit de facteurs	Corrigé	Deux canaux additifs, natif et acclimaté — et le second borné par la part mûre depuis la v2.3 (§3.0). Et la signature vive se fond en quelques jours, ce qui rend le peuplement physiquement nécessaire.
Portails	Ajouté	Système canonique entier, omis.
Temples élémentaires	Ajouté	Système canonique entier, omis — et c'est le meilleur du dépôt.
Défiscalisation du bassin	Ajouté	Justification interne du rendement anormal.
Fidèles arrêtés, anguille migrante	Ajouté	Contenu canonique gratuit.
Technique comme axe de persistance	Ajouté	Sans elle, la ponte est punitive.
Fin en trois éléments (eau pure + bénédiction + indication)	Refondu	Quatre temps canoniques : l'esprit vient, le relais, l'indication, demi-dieu vivant.
Paliers de voix : « elle n'explique rien avant l'acte VIII »	Nuancé	Elle est franche dès qu'il l'entend. Elle n'a pas les moyens d'un exposé, faute d'analgésique local. Table à quatre temps.
La Primordiale « condensat de lieu »	Corrigé dès v1.1	Condensat d'intention instinctive, jamais localisée.
Lexique, six couches à deux variantes, redescente à 20–25 %, conviction, seuils de peuplement, deux bestiaires, monétisation	Conservés	Design local, dérivé de documents effectivement lus. Aucun conflit avec le canon.
Annexe C — Traçabilité depuis Étang des Merveilles
Élément v2.0 (avril 2026)	Statut	Raison
30 zones / 11 jouables	Converti	6 → assises, 16 → paliers, 2 → éléments narratifs, 4 → contenu Tier 1, 1 fusionné, 3 coupés.
210 espèces planifiées — 38 documentées dans POISSONS.md, 16 réellement implémentées dans fishTypes.ts (chiffre corrigé en v2.2 : la v2.1 annonçait 38 implémentées)	Refondu	Plus de collection payante. Espèces convaincues, réparties en deux bestiaires.
Achat de poissons (1,15^n), niveaux achetés	Supprimé	On ne possède pas le vivant. Le meurtre et l'extraction sont inefficaces au canon.
Jalons 10/25/50/100	Conservé, converti	Seuils de peuplement. Meilleure mécanique de satisfaction courte de l'ancien jeu.
Gemmes, Perles	Supprimé	Vendaient de la puissance. Remplacées par mana + Foi.
Corail de Prestige, Marché des Perles, Améliorations de Prestige	Supprimé	Arbres de multiplicateurs abstraits. Remplacés par la technique, qui s'apprend au lieu de s'acheter.
Boosts de temps, Poisson Céleste	Supprimé	Vendent de la production. Créature premium hors progression, incompatible avec un peuple de rejetés.
Défis quotidiens	Supprimé	Récompensent la présence quotidienne, contre le pilier n° 2.
49 succès	Refondu, v2.2	Ni cosmétique ni pot commun : trois familles typées, effets chiffre ou verbe, distribution narrative continue. Voir §14.
Événements narratifs ambiants	Conservé, pool réécrit	Excellent mécanisme, registre à refaire.
Journal + Bestiaire	Conservé, étendu	Journal par palier de voix, bestiaire scindé.
Améliorations de décor (4 emplacements)	Conservé, refondu	Aménagements en mana qui régressent ; la permanence passe par la technique et la densité.
Hors ligne 24 h, ×2 premium	Conservé, refondu	Cap en heures débloqué par technique. Aucune perte, aucun multiplicateur premium.
Piliers de design	Conservé, reformulé	Quatre des six passent tels quels.
Personas	Conservé	Un ajouté : le lecteur.
Stack et architecture	Conservé intégralement	Principal actif récupérable.
Catalogue IAP, publicités opt-in	Suspendu	Modèle économique non résolu (§17.3).
DA « or, arc-en-ciel, nexus, quantique »	Supprimé	Hors registre vital. Aucun dieu n'est bienveillant dans cet univers.
Annexe D — Registre des questions ouvertes

Un seul endroit pour tout ce qui n'est pas tranché. Les [P] dispersés dans le document y renvoient. Une question sortie d'ici doit être inscrite au journal de tête, jamais résolue en silence.

D.1 Blocages structurels — levés en v2.3
#	Question	Décision	§
P2	Comment borner le canal acclimaté	Plafond de maturation — part du type mûr, non densité absolue	§3.0
P3	Nature de la ponte	Évolution subie — l'indication ne couvre que le dernier franchissement	§10.5
P1	Lexique « divergence forcée »	Divergence non choisie, partout	§2.4

Il ne reste aucun blocage structurel. La question ouverte la plus lourde est désormais le scope ([P4] / [P20]), qui n'empêche pas de coder le prototype mais commande le budget d'assets et la forme de la courbe.

D.2 Calibrage — bloque le prototype
#	Question	§	Recommandation
P4	Cinq ou six assises ? Tranché v2.4 : six, au papier. Engagement par assise en production, coupe au milieu jamais à la fin	§6.2	—
P29	Vitesse de maturation d'un palier laissé maigre, et granularité de part_mûre (par palier ou par assise)	§3.0	Décide si l'arbitrage peupler/vieillir se joue à l'échelle d'une session ou d'un cycle
P5	Valeur de f, et plancher de réduction_technique — gratuité assumée en fin de partie, ou plancher dur ?	§6.4	Gratuité assumée : le frein réel devient le repeuplement, ce qui est cohérent
P6	Valeur de k, taux de repeuplement après ponte, et sa sensibilité à la technique conviction	§6.4 / §7.2	À mesurer conjointement avec f. C'est k qui produit réellement les 20–25 %
P7	Un temple inerte taxe-t-il la Foi ? Et comment rendre le frein lisible malgré sa latence d'un cycle ?	§9.3	Non pour le premier point — la taxe suppose une entité qui capte l'adressage. Et afficher le flux adressé en permanence pour le second
D.3 Conception ouverte — non bloquant
#	Question	§
P8	Volume de textes originaux hors gabarits, par assise — détermine si le poste d'écriture est tenable	§14.2
P9	La numérisation de l'UI doit-elle être avancée ? Chiffres complets à la 3ᵉ ponte peut représenter plusieurs jours réels	§13.1
P10	Acquis de séjour — extension Tier 2 à valider, et demande de précision Tier 0 §8 à porter	§2.3
P11	Condition d'épuisement de la scellure — extension Tier 2 mineure à porter	§4.2
P12	Le mot « Foi » ne nomme que le quart favorable de ce qui est émis. Candidats : veille, égard, ferveur, mémoire	§4.2
P13	Structure du réseau de portails — graphe libre à coût croissant en distance topologique	§8.3
P14	Dosage de la réponse d'un allié : où passe la ligne entre une divergence et une horreur	§9.2
P15	Ce que réclame un allié — contrainte permanente sur la conduite du royaume	§9.5
P16	Ce que l'esprit veut. Piste : une place au refuge	§12.1
P17	Y a-t-il du mana antique au voisinage de la pierre, et peut-il y toucher	§12.5
P18	Y a-t-il une machine dans le jeu 1	§16.5
P19	Promotion de l'esprit au slot canonique du condensat de lieu — à décider et à dater	§13.3
D.4 Absents du document — à ouvrir

Points relevés à l'audit de la v2.2 et qui n'ont aujourd'hui aucune section.

#	Manque	Pourquoi ça compte
P20	Durée de la courbe. Le §16.2 additionné donne ~1 100 h pour une partie complète, dont 78 % dans les assises V et VI. Si le récit est la récompense, une fin atteinte par 3 % des joueurs est 97 % de l'écriture jamais lue — et le retournement de §13.2 est le meilleur moment du jeu. Depuis la v2.4, c'est une question de calibrage et non de contenu : la coupe porte sur la longueur des cycles III à V, jamais sur le nombre d'assises. Cible suggérée : 300–400 h, 8–10 pontes	À mesurer au prototype. Décide le budget d'assets et la monétisation
P21	Écran de retour hors ligne. L'écran le plus important du genre n'est spécifié nulle part, alors que l'absence est ici canoniquement productive	C'est l'emplacement narratif le mieux placé du jeu
P22	L'acquis de séjour progresse-t-il hors ligne ? Oui/non non tranché	Décide si le jeu se joue ou s'attend
P23	Manipulation d'horloge. Aucune protection prévue, alors que l'absence n'est jamais pénalisée	Plus critique ici que dans un idle ordinaire
P24	Direction sonore. Zéro mention dans tout le document. La lumière est la variable de progression revendiquée ; elle appelle un pendant audio, et c'est bon marché	Un des meilleurs rapports effet/coût du projet
P25	Mortalité de population et legs consenti. §7.2 n'a aucun terme de décès, et la faculté de captation — la mutation qui définit le héros — ne sert qu'au prologue. Tier 0 §6.2 (le mana cédé transfère la quasi-totalité) colle exactement au pilier « on ne récompense jamais la mise à mort » et à un refuge de mourants	Le système que ce jeu devrait avoir et n'a pas
P26	Miracles à variance de qualité plutôt qu'à tout ou rien. Ressource minuscule + résultat nul possible est la combinaison la plus mal tolérée du genre. Tier 0 dit qu'on déplace des probabilités, pas qu'on perd sa mise	Décide si la Foi est utilisée ou thésaurisée
P27	Architecture d'information et navigation sur 66 paliers. §17.2 la traite comme un confort vendable ; c'est de l'UX de base	Bloque toute maquette
P28	Schéma de sauvegarde versionné et migration. §18.3 supprime toutes les clés du store existant	Bloque la première release