import React, { useEffect, useRef, useMemo, useState } from 'react';
import { BIOMES, FISH } from '../gameData';

const BAND_HEIGHT = 720;

interface PondCanvasProps {
  depth: number;
  fishCounts: Record<string, number>;
  fishIconStyle?: string;
}

function BiomeBand({ biome, playerDepth, fishCounts, fishIconStyle }: {
  biome: typeof BIOMES[0];
  playerDepth: number;
  fishCounts: Record<string, number>;
  fishIconStyle: string;
}) {
  const { depth, name, blurb, palette } = biome;
  const { top, mid, deep } = palette;
  const locked = depth > playerDepth;
  const isFirst = depth === 0;
  const isLast  = depth === BIOMES.length - 1;
  const gradId = `grad-${depth}`;
  const rippleId = `ripples-${depth}`;

  const sprites: { fish: typeof FISH[0]; key: string; i: number }[] = [];
  Object.entries(fishCounts || {}).forEach(([fishId, count]) => {
    const fish = FISH.find(f => f.id === fishId);
    if (!fish || fish.depth !== depth) return;
    const visible = Math.min(count, 6);
    for (let i = 0; i < visible; i++) sprites.push({ fish, key: `${fishId}-${i}`, i });
  });

  const rays = depth <= 1 ? Array.from({ length: 6 }, (_, i) => {
    const left = 10 + i * 14;
    const rot = -8 + i * 2.5;
    const dur = 9 + (i % 3) * 2;
    return (
      <div
        key={i}
        className="ray"
        style={{
          left: `${left}%`,
          transform: `rotate(${rot}deg)`,
          animation: `ray-sway ${dur}s ease-in-out ${i * 0.4}s infinite alternate`,
        }}
      />
    );
  }) : null;

  return (
    <div className={`biome-band ${locked ? 'locked' : ''}`} data-depth={depth}>
      <svg viewBox={`0 0 1600 ${BAND_HEIGHT}`} preserveAspectRatio="xMidYMid slice">
        <defs>
          <linearGradient id={gradId} x1="0" x2="0" y1="0" y2="1">
            <stop offset="0%"   stopColor={top} />
            <stop offset="55%"  stopColor={mid} />
            <stop offset="100%" stopColor={deep} />
          </linearGradient>
          <pattern id={rippleId} width="160" height="160" patternUnits="userSpaceOnUse">
            <path d="M0 70 Q 40 60, 80 70 T 160 70" stroke="oklch(1 0 0 / 0.07)" fill="none" strokeWidth="1.2" />
            <path d="M0 110 Q 40 100, 80 110 T 160 110" stroke="oklch(1 0 0 / 0.05)" fill="none" strokeWidth="1.2" />
          </pattern>
        </defs>

        <rect width="1600" height={BAND_HEIGHT} fill={`url(#${gradId})`} />
        {isFirst && <ellipse cx="800" cy="-50" rx="900" ry="320" fill="oklch(0.96 0.1 80 / 0.45)" />}
        <ellipse cx="800" cy="120" rx="1100" ry="120" fill="oklch(0.95 0.08 80 / 0.10)" />
        <ellipse cx="800" cy={BAND_HEIGHT - 120} rx="1300" ry="160" fill="oklch(0.18 0.06 250 / 0.18)" />
        <rect width="1600" height={BAND_HEIGHT} fill={`url(#${rippleId})`} opacity="0.7" />

        {depth === 0 && (
          <g>
            <ellipse cx="240" cy="80" rx="80" ry="18" fill="oklch(0.52 0.1 145 / 0.55)" />
            <ellipse cx="230" cy="76" rx="50" ry="14" fill="oklch(0.6 0.1 145 / 0.7)" />
            <ellipse cx="1250" cy="60" rx="90" ry="20" fill="oklch(0.52 0.1 148 / 0.5)" />
            <ellipse cx="1240" cy="56" rx="58" ry="15" fill="oklch(0.6 0.1 148 / 0.65)" />
            <ellipse cx="680" cy="100" rx="64" ry="15" fill="oklch(0.55 0.11 145 / 0.45)" />
            <g opacity="0.7">
              <path d="M 100 720 Q 96 600 110 540" stroke="oklch(0.4 0.08 145)" strokeWidth="3" fill="none" />
              <path d="M 120 720 Q 124 580 115 510" stroke="oklch(0.4 0.08 145)" strokeWidth="3" fill="none" />
              <path d="M 1480 720 Q 1488 580 1474 500" stroke="oklch(0.4 0.08 145)" strokeWidth="3" fill="none" />
              <path d="M 1500 720 Q 1496 600 1510 540" stroke="oklch(0.4 0.08 145)" strokeWidth="3" fill="none" />
            </g>
          </g>
        )}
        {depth === 1 && (
          <g opacity="0.6">
            <path d="M 0 200 Q 200 240 400 220 T 800 230 T 1200 220 T 1600 230" stroke="oklch(0.4 0.06 25 / 0.4)" strokeWidth="2" fill="none" />
            <path d="M 0 450 Q 200 480 400 470 T 800 480 T 1200 470 T 1600 480" stroke="oklch(0.45 0.07 30 / 0.5)" strokeWidth="2" fill="none" />
            <ellipse cx="220" cy="540" rx="60" ry="8" fill="oklch(0.55 0.15 25 / 0.5)" />
            <ellipse cx="1240" cy="380" rx="80" ry="10" fill="oklch(0.55 0.15 25 / 0.5)" />
          </g>
        )}
        {depth === 2 && (
          <g opacity="0.6">
            <path d="M 200 650 Q 210 580 230 575 Q 252 588 256 650 Z" fill="oklch(0.55 0.13 30 / 0.7)" />
            <path d="M 240 660 Q 256 590 280 585 Q 308 596 302 660 Z" fill="oklch(0.5 0.12 350 / 0.6)" />
            <path d="M 1320 640 Q 1334 570 1360 563 Q 1390 578 1382 640 Z" fill="oklch(0.6 0.1 280 / 0.6)" />
            <path d="M 1360 660 Q 1374 600 1398 593 Q 1424 604 1418 660 Z" fill="oklch(0.55 0.13 30 / 0.7)" />
            <circle cx="500" cy="300" r="4" fill="oklch(0.9 0.15 195 / 0.8)" />
            <circle cx="900" cy="200" r="3" fill="oklch(0.9 0.15 320 / 0.8)" />
            <circle cx="1100" cy="380" r="5" fill="oklch(0.9 0.15 145 / 0.7)" />
            <circle cx="300" cy="420" r="3" fill="oklch(0.9 0.15 30 / 0.7)" />
          </g>
        )}
        {depth === 3 && (
          <g>
            <circle cx="300" cy="200" r="3" fill="oklch(0.9 0.15 195 / 0.9)" />
            <circle cx="700" cy="350" r="4" fill="oklch(0.85 0.12 280 / 0.8)" />
            <circle cx="1100" cy="280" r="3" fill="oklch(0.9 0.13 145 / 0.9)" />
            <circle cx="1300" cy="500" r="4" fill="oklch(0.9 0.15 30 / 0.75)" />
            <circle cx="500" cy="540" r="3" fill="oklch(0.9 0.15 195 / 0.8)" />
            <circle cx="900" cy="120" r="2" fill="oklch(0.85 0.13 280 / 0.7)" />
          </g>
        )}
        {depth === 4 && (
          <g>
            <path d="M 320 720 L 336 580 L 364 580 L 380 720 Z" fill="oklch(0.18 0.05 25)" />
            <ellipse cx="350" cy="582" rx="20" ry="5" fill="oklch(0.32 0.09 25)" />
            <path d="M 340 580 Q 360 460 332 380 Q 372 460 360 580" fill="oklch(0.5 0.1 30 / 0.4)" />
            <path d="M 1160 720 L 1180 600 L 1220 600 L 1240 720 Z" fill="oklch(0.18 0.05 25)" />
            <ellipse cx="1200" cy="602" rx="22" ry="5" fill="oklch(0.32 0.09 25)" />
            <path d="M 1184 600 Q 1212 460 1192 360 Q 1234 460 1220 600" fill="oklch(0.5 0.1 30 / 0.4)" />
            <circle cx="350" cy="450" r="3" fill="oklch(0.85 0.18 30 / 0.8)" />
            <circle cx="370" cy="380" r="2" fill="oklch(0.85 0.18 30 / 0.7)" />
            <circle cx="1200" cy="430" r="3" fill="oklch(0.85 0.18 30 / 0.8)" />
          </g>
        )}
        {depth === 5 && (
          <g opacity="0.7">
            <ellipse cx="800" cy="700" rx="1500" ry="40" fill="oklch(0.18 0.05 270 / 0.5)" />
            <ellipse cx="300" cy="200" rx="40" ry="22" fill="oklch(0.7 0.08 280 / 0.25)" />
            <path d="M 260 200 Q 280 280 280 360" stroke="oklch(0.7 0.08 280 / 0.2)" strokeWidth="1.2" fill="none" />
            <path d="M 340 200 Q 320 290 320 380" stroke="oklch(0.7 0.08 280 / 0.2)" strokeWidth="1.2" fill="none" />
            <ellipse cx="1200" cy="350" rx="50" ry="28" fill="oklch(0.7 0.08 280 / 0.25)" />
            <path d="M 1160 350 Q 1170 440 1170 520" stroke="oklch(0.7 0.08 280 / 0.2)" strokeWidth="1.2" fill="none" />
            <path d="M 1240 350 Q 1230 450 1235 540" stroke="oklch(0.7 0.08 280 / 0.2)" strokeWidth="1.2" fill="none" />
          </g>
        )}
        {depth === 6 && (
          <g opacity="0.55">
            <g stroke="oklch(0.85 0.07 295 / 0.55)" fill="none" strokeWidth="1.4">
              <path d="M 150 200 L 180 200 M 165 185 L 165 215 M 180 200 L 200 215" />
              <path d="M 220 220 Q 235 200 250 220 Q 235 240 220 220 Z" />
              <path d="M 1380 280 L 1410 280 M 1395 265 L 1395 295 M 1410 280 L 1430 295" />
              <path d="M 1320 400 L 1352 380 L 1352 420 Z" />
              <path d="M 700 500 L 720 500 M 710 490 L 710 510" />
            </g>
          </g>
        )}
        {depth === 7 && (
          <g>
            <g opacity="0.6">
              <circle cx="800" cy="360" r="220" fill="none" stroke="oklch(0.9 0.1 295)" strokeWidth="0.8" strokeDasharray="3 4" />
              <circle cx="800" cy="360" r="140" fill="none" stroke="oklch(0.9 0.1 295)" strokeWidth="0.8" strokeDasharray="2 6" />
              <circle cx="800" cy="360" r="70" fill="oklch(0.95 0.1 295 / 0.15)" />
              <circle cx="800" cy="360" r="30" fill="oklch(0.95 0.1 295 / 0.35)" />
            </g>
            <circle cx="400" cy="220" r="3" fill="oklch(0.95 0.13 295 / 0.85)" />
            <circle cx="1200" cy="180" r="4" fill="oklch(0.95 0.13 295 / 0.8)" />
            <circle cx="1300" cy="540" r="3" fill="oklch(0.95 0.13 295 / 0.85)" />
            <circle cx="280" cy="540" r="4" fill="oklch(0.95 0.13 295 / 0.8)" />
          </g>
        )}

        {isLast && (
          <g opacity="0.7">
            <path
              d={`M0 ${BAND_HEIGHT - 40} Q 200 ${BAND_HEIGHT - 80} 400 ${BAND_HEIGHT - 60} T 700 ${BAND_HEIGHT - 70} T 1000 ${BAND_HEIGHT - 60} T 1300 ${BAND_HEIGHT - 80} T 1600 ${BAND_HEIGHT - 70} L 1600 ${BAND_HEIGHT} L 0 ${BAND_HEIGHT} Z`}
              fill={deep}
            />
          </g>
        )}
      </svg>

      {rays && <div className="biome-rays" aria-hidden="true">{rays}</div>}

      <div className="biome-label">
        <span className="biome-num">{String(depth).padStart(2, '0')}</span>
        <div>
          <div className="biome-name">{name}</div>
          <div className="biome-blurb">{blurb}</div>
        </div>
      </div>

      {locked && (
        <div className="biome-locked">
          <div className="lk-card">
            <div className="lk-eyebrow">Sealed</div>
            <div className="lk-name">{name}</div>
            <div className="lk-sub">Dig from above to enter.</div>
          </div>
        </div>
      )}

      <div className="fish-layer" aria-hidden="true">
        {sprites.map(({ fish, key, i }, idx) => {
          const seed = (fish.id.charCodeAt(0) * 31 + i * 17 + idx) % 1000;
          const topPct = 18 + (seed % 70);
          const dur = 18 + (seed % 14);
          const delay = -((seed * 0.13) % dur);
          const flip = i % 2 === 0;
          return (
            <div
              key={key}
              className={`fish-sprite ${fishIconStyle}`}
              style={{
                top: `${topPct}%`,
                left: 0,
                animation: `swim-${flip ? 'r' : 'l'} ${dur}s ease-in-out ${delay}s infinite`,
              }}
            >
              <div className="frame">
                {fishIconStyle === 'placeholder' ? <span>FISH<br />ART</span> : null}
                {fishIconStyle === 'emoji' ? '🐟' : null}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function DepthRuler({ depth, scrollRef }: { depth: number; scrollRef: React.RefObject<HTMLDivElement | null> }) {
  const [scrolled, setScrolled] = useState(0);

  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    const onScroll = () => setScrolled(el.scrollTop / BAND_HEIGHT);
    el.addEventListener('scroll', onScroll);
    onScroll();
    return () => el.removeEventListener('scroll', onScroll);
  }, [scrollRef]);

  const jumpTo = (d: number) => {
    scrollRef.current?.scrollTo({ top: d * BAND_HEIGHT, behavior: 'smooth' });
  };

  return (
    <div className="depth-ruler">
      {BIOMES.map(b => {
        const locked = b.depth > depth;
        const active = Math.round(scrolled) === b.depth;
        return (
          <button
            key={b.depth}
            className={`dr-tick ${locked ? 'locked' : ''} ${active ? 'active' : ''}`}
            onClick={() => !locked && jumpTo(b.depth)}
            disabled={locked}
            title={locked ? 'Sealed' : b.name}
          >
            <span className="dr-num">{String(b.depth).padStart(2, '0')}</span>
            <span className="dr-name">{b.name}</span>
          </button>
        );
      })}
    </div>
  );
}

export function PondCanvas({ depth, fishCounts, fishIconStyle = 'placeholder' }: PondCanvasProps) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const hasInitialised = useRef(false);

  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    el.scrollTo({
      top: depth * BAND_HEIGHT,
      behavior: hasInitialised.current ? 'smooth' : 'auto',
    });
    hasInitialised.current = true;
  }, [depth]);

  const bubbles = useMemo(() => (
    Array.from({ length: 30 }, (_, i) => {
      const size = 4 + Math.random() * 12;
      const left = Math.random() * 100;
      const dur = 14 + Math.random() * 16;
      const delay = -Math.random() * dur;
      return { size, left, dur, delay, key: i };
    })
  ), []);

  return (
    <div className="pond" ref={scrollRef}>
      <div className="pond-column" style={{ height: BIOMES.length * BAND_HEIGHT }}>
        {BIOMES.map((biome) => (
          <BiomeBand
            key={biome.depth}
            biome={biome}
            playerDepth={depth}
            fishCounts={fishCounts}
            fishIconStyle={fishIconStyle}
          />
        ))}
      </div>

      <div className="pond-bubbles" aria-hidden="true">
        {bubbles.map(b => (
          <div
            key={b.key}
            className="bubble"
            style={{
              width: b.size,
              height: b.size,
              left: `${b.left}%`,
              bottom: -20,
              animation: `rise ${b.dur}s linear ${b.delay}s infinite`,
            }}
          />
        ))}
      </div>

      <div className="pond-grain" aria-hidden="true" />

      <DepthRuler depth={depth} scrollRef={scrollRef} />
    </div>
  );
}
