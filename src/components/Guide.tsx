const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <div className="mb-5">
    <h4 className="text-sm font-bold text-white uppercase tracking-wider border-b border-white/10 pb-1 mb-2">{title}</h4>
    <div className="text-xs text-gray-300 leading-relaxed space-y-1.5">{children}</div>
  </div>
);

const Tag = ({ children, color = 'blue' }: { children: React.ReactNode; color?: string }) => {
  const cls: Record<string, string> = {
    blue:   'bg-blue-900/50 text-blue-300 border-blue-700/50',
    green:  'bg-emerald-900/50 text-emerald-300 border-emerald-700/50',
    purple: 'bg-purple-900/50 text-purple-300 border-purple-700/50',
    yellow: 'bg-yellow-900/50 text-yellow-300 border-yellow-700/50',
    teal:   'bg-teal-900/50 text-teal-300 border-teal-700/50',
  };
  return (
    <span className={`inline-block text-[10px] font-bold px-1.5 py-0.5 rounded border mx-0.5 ${cls[color] ?? cls.blue}`}>
      {children}
    </span>
  );
};

export const Guide = () => (
  <div className="flex flex-col gap-1">
    <div className="flex items-center justify-between mb-3">
      <h3 className="text-sm font-semibold text-white uppercase tracking-wider">Guide du Joueur</h3>
      <span className="text-lg">📖</span>
    </div>

    <Section title="🎮 Bienvenue">
      <p>
        Dans <strong className="text-white">l'Étang des Merveilles</strong>, votre but est de produire de plus en plus de <Tag color="blue">Mana</Tag>,
        en achetant des poissons, en améliorant votre étang et en accumulant des ressources rares.
      </p>
    </Section>

    <Section title="💰 Les Ressources">
      <p><Tag color="blue">Mana</Tag> — La ressource principale. Produite passivement par vos poissons. Sert à acheter et améliorer.</p>
      <p><Tag color="green">Gemmes 💎</Tag> — Gagnées en débloquant des <strong className="text-white">Succès</strong> et via la branche Mystique du Corail. Servent au Corail de Prestige et au Marché des Perles.</p>
      <p><Tag color="purple">Perles 🪸</Tag> — La monnaie de Prestige. Gagnées uniquement en faisant un Prestige. Servent aux Améliorations de Prestige.</p>
    </Section>

    <Section title="🐟 Les Poissons">
      <p>Chaque poisson produit du <Tag color="blue">Mana/s</Tag> en continu. Le coût augmente à chaque achat (×1,15).</p>
      <p>Boutons d'achat : <Tag>x1</Tag> <Tag>x10</Tag> <Tag>max</Tag> pour accélérer l'accumulation.</p>
      <p>Chaque poisson possède un <strong className="text-white">niveau</strong> (1 → 100). Le bouton Améliorer augmente son revenu selon la formule <code className="text-blue-300">1,5^(niveau-1)</code>.</p>
    </Section>

    <Section title="⭐ Les Jalons de Niveau">
      <p>À chaque palier <Tag color="yellow">10</Tag> <Tag color="yellow">25</Tag> <Tag color="yellow">50</Tag> <Tag color="yellow">100</Tag>, un poisson décroche un bonus permanent :</p>
      <p>— <strong className="text-white">Multiplicateur propre</strong> ×2 (cumulatif : ×2, ×4, ×8, ×16 au total)</p>
      <p>— <strong className="text-white">Bonus global</strong> % sur toute la production (variable selon l'espèce).</p>
      <p>La barre de jalons sous chaque poisson indique les paliers atteints (🟡) et le prochain (clignotant).</p>
    </Section>

    <Section title="⛏️ Améliorer l'Étang">
      <p>Creuser plus profond coûte de la <Tag color="blue">Mana</Tag> et débloque de nouvelles espèces de poissons :</p>
      <p>Niv. 0 → <Tag>🐟 Or</Tag>  ·  Niv. 1 → <Tag>🐠 Rubis</Tag>  ·  Niv. 2 → <Tag>🐡 Diamant</Tag>  ·  Niv. 3 → <Tag>🦑 Abyssal</Tag></p>
      <p>Niv. 4 → <Tag color="yellow">🌟 Céleste</Tag> (requiert aussi un Prestige)</p>
      <p>Les zones sont visibles dans la vue de l'étang — utilisez la molette pour naviguer en profondeur.</p>
    </Section>

    <Section title="✨ Le Prestige">
      <p>Disponible à partir de la profondeur 2. Le Prestige <strong className="text-white">remet le jeu à zéro</strong> (Mana, poissons, profondeur) mais vous rapporte des <Tag color="purple">Perles 🪸</Tag>.</p>
      <p>La récompense augmente avec la profondeur atteinte et la Mana accumulée.</p>
      <p>Avec les <strong className="text-white">Améliorations de Prestige</strong>, chaque run suivant commence plus fort.</p>
    </Section>

    <Section title="🧬 Corail de Prestige">
      <p>Achetez des améliorations permanentes avec des <Tag color="green">Gemmes 💎</Tag>. 4 branches :</p>
      <p><strong className="text-emerald-300">Biologie</strong> — Revenu global (+15% → +75% + jalons anticipés)</p>
      <p><strong className="text-amber-300">Géologie</strong> — Coûts de creusage (−15% → −50% cumulable)</p>
      <p><strong className="text-violet-300">Alchimie</strong> — Boost de Mana (durée, coût, ×3 au lieu de ×2)</p>
      <p><strong className="text-cyan-300">Mystique</strong> — Gemmes passives + bonus sur récompenses succès</p>
    </Section>

    <Section title="🏪 Marché des Perles">
      <p>Améliorations supplémentaires achetées avec des <Tag color="green">Gemmes 💎</Tag> : revenu global, réduction coût poissons, production hors-ligne ×2.</p>
    </Section>

    <Section title="🪸 Améliorations de Prestige">
      <p>Achetées avec des <Tag color="purple">Perles 🪸</Tag> et <strong className="text-white">permanentes</strong>. Elles modifient le point de départ de chaque run :</p>
      <p>Mana de départ · Profondeur initiale · Niveau de départ des poissons · Poissons conservés · Bonus de Perles · Réduction légendaire…</p>
    </Section>

    <Section title="🏆 Succès & ⚔️ Défis">
      <p><strong className="text-white">Succès</strong> — Débloquez des badges automatiquement en jouant. Chaque succès rapporte des <Tag color="green">Gemmes 💎</Tag>.</p>
      <p><strong className="text-white">Défis</strong> — 3 objectifs quotidiens renouvelés à minuit. Les réclamer rapporte des <Tag color="purple">Perles 🪸</Tag>.</p>
    </Section>

    <Section title="🌙 Production Hors-Ligne">
      <p>L'étang continue de produire jusqu'à <strong className="text-white">24 heures</strong> après votre déconnexion. Le calcul tient compte de vos boosts actifs et des multiplicateurs du Marché des Perles.</p>
    </Section>
  </div>
);
