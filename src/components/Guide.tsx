const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <div className="lg-guide-sec">
    <h4>{title}</h4>
    <div>{children}</div>
  </div>
);

const Tag = ({ children, color = 'blue' }: { children: React.ReactNode; color?: string }) => (
  <span className={`lg-tag ${color}`}>{children}</span>
);

export const Guide = () => (
  <div className="lg-guide">
    <Section title="🎮 Bienvenue">
      <p>
        Dans <strong>l'Étang des Merveilles</strong>, votre but est de produire de plus en plus de <Tag color="blue">Mana</Tag>,
        en achetant des poissons, en améliorant votre étang et en accumulant des ressources rares.
      </p>
    </Section>

    <Section title="💰 Les Ressources">
      <p><Tag color="blue">Mana</Tag> — La ressource principale. Produite passivement par vos poissons. Sert à acheter et améliorer.</p>
      <p><Tag color="green">Gemmes 💎</Tag> — Gagnées en débloquant des <strong>Succès</strong> et via la branche Mystique du Corail. Servent au Corail de Prestige et au Marché des Perles.</p>
      <p><Tag color="purple">Perles 🪸</Tag> — La monnaie de Prestige. Gagnées uniquement en faisant un Prestige. Servent aux Améliorations de Prestige.</p>
    </Section>

    <Section title="🐟 Les Poissons">
      <p>Chaque poisson produit du <Tag color="blue">Mana/s</Tag> en continu. Le coût augmente à chaque achat (×1,15).</p>
      <p>Boutons d'achat : <Tag>x1</Tag> <Tag>x10</Tag> <Tag>max</Tag> pour accélérer l'accumulation.</p>
      <p>Chaque poisson possède un <strong>niveau</strong> (1 → 100). Le bouton Améliorer augmente son revenu selon la formule <code>1,5^(niveau-1)</code>.</p>
    </Section>

    <Section title="⭐ Les Jalons de Niveau">
      <p>À chaque palier <Tag color="yellow">10</Tag> <Tag color="yellow">25</Tag> <Tag color="yellow">50</Tag> <Tag color="yellow">100</Tag>, un poisson décroche un bonus permanent :</p>
      <p>— <strong>Multiplicateur propre</strong> ×2 (cumulatif : ×2, ×4, ×8, ×16 au total)</p>
      <p>— <strong>Bonus global</strong> % sur toute la production (variable selon l'espèce).</p>
      <p>La barre de jalons sous chaque poisson indique les paliers atteints (🟡) et le prochain (clignotant).</p>
    </Section>

    <Section title="⛏️ Améliorer l'Étang">
      <p>Creuser plus profond coûte de la <Tag color="blue">Mana</Tag> et débloque de nouveaux biomes et espèces :</p>
      <p>Niv. 0 → <Tag>🐟 Or</Tag> <Tag>🎏 Carpe</Tag>  ·  Niv. 1 → <Tag>🐠 Rubis</Tag> <Tag>🪲 Libellule</Tag></p>
      <p>Niv. 2 → <Tag>🐡 Diamant</Tag> <Tag>🦀 Crabe</Tag>  ·  Niv. 3 → <Tag>🦑 Abyssal</Tag> <Tag>🐙 Pieuvre</Tag></p>
      <p>Niv. 4 → <Tag color="yellow">🔥 Salamandre</Tag> <Tag color="yellow">⚡ Anguille</Tag>  ·  Niv. 5 → <Tag>🫧 Méduse</Tag> <Tag>🦈 Requin</Tag></p>
      <p>Niv. 6 → <Tag>🐉 Dragon</Tag> <Tag>💎 Léviathan</Tag>  ·  Niv. 7 → <Tag color="yellow">✨ Égrégore</Tag> <Tag color="yellow">🌟 Céleste</Tag> (Prestige requis)</p>
      <p>Les zones sont visibles dans la vue de l'étang — utilisez la molette pour naviguer en profondeur.</p>
    </Section>

    <Section title="✨ Le Prestige">
      <p>Disponible à partir de la profondeur 2. Le Prestige <strong>remet le jeu à zéro</strong> (Mana, poissons, profondeur) mais vous rapporte des <Tag color="purple">Perles 🪸</Tag>.</p>
      <p>La récompense augmente avec la profondeur atteinte et la Mana accumulée.</p>
      <p>Avec les <strong>Améliorations de Prestige</strong>, chaque run suivant commence plus fort.</p>
    </Section>

    <Section title="🧬 Corail de Prestige">
      <p>Achetez des améliorations permanentes avec des <Tag color="green">Gemmes 💎</Tag>. 5 branches :</p>
      <p><strong style={{ color: '#2FB873' }}>Biologie</strong> — Revenu global (+15% → +75% + jalons anticipés)</p>
      <p><strong style={{ color: '#E59412' }}>Géologie</strong> — Coûts de creusage (−15% → −50% cumulable)</p>
      <p><strong style={{ color: '#6B47F0' }}>Alchimie</strong> — Boost de Mana (durée, coût, ×3 au lieu de ×2)</p>
      <p><strong style={{ color: '#0E9CAB' }}>Mystique</strong> — Gemmes passives + bonus sur récompenses succès</p>
      <p><strong style={{ color: '#16C8C4' }}>Océanologie</strong> — Revenu des profondeurs, coût poissons, +3 💎/min</p>
    </Section>

    <Section title="🏪 Marché des Perles">
      <p>Améliorations supplémentaires achetées avec des <Tag color="green">Gemmes 💎</Tag> : revenu global, réduction coût poissons, production hors-ligne ×2.</p>
    </Section>

    <Section title="🪸 Améliorations de Prestige">
      <p>Achetées avec des <Tag color="purple">Perles 🪸</Tag> et <strong>permanentes</strong>. Elles modifient le point de départ de chaque run :</p>
      <p>Mana de départ · Profondeur initiale · Niveau de départ des poissons · Poissons conservés · Bonus de Perles · Réduction légendaire…</p>
    </Section>

    <Section title="🏆 Succès & ⚔️ Défis">
      <p><strong>Succès</strong> — Débloquez des badges automatiquement en jouant. Chaque succès rapporte des <Tag color="green">Gemmes 💎</Tag>.</p>
      <p><strong>Défis</strong> — 3 objectifs quotidiens renouvelés à minuit. Les réclamer rapporte des <Tag color="purple">Perles 🪸</Tag>.</p>
    </Section>

    <Section title="🌙 Production Hors-Ligne">
      <p>L'étang continue de produire jusqu'à <strong>24 heures</strong> après votre déconnexion. Le calcul tient compte de vos boosts actifs et des multiplicateurs du Marché des Perles.</p>
    </Section>
  </div>
);
