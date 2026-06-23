import { Component, type CSSProperties, type ErrorInfo, type ReactNode } from 'react';
import { STORAGE_KEY } from '../store/useGameStore';

interface Props {
  children: ReactNode;
}

interface State {
  error: Error | null;
}

/**
 * Capture toute erreur de rendu pour éviter l'écran blanc irrécupérable.
 * Propose de recharger (erreur transitoire) ou de réinitialiser la sauvegarde
 * (si la save est en cause).
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Trace conservée pour le diagnostic ; non bloquante pour l'utilisateur.
    console.error('Erreur de rendu interceptée par ErrorBoundary :', error, info);
  }

  private handleReload = () => {
    window.location.reload();
  };

  private handleResetSave = () => {
    try {
      window.localStorage.removeItem(STORAGE_KEY);
    } catch {
      // localStorage indisponible : on recharge tout de même.
    }
    window.location.reload();
  };

  render() {
    if (!this.state.error) return this.props.children;

    return (
      <div style={styles.overlay}>
        <div style={styles.card}>
          <div style={styles.emoji}>🐟</div>
          <h1 style={styles.title}>L'étang s'est troublé</h1>
          <p style={styles.text}>
            Une erreur inattendue a interrompu le jeu. Tu peux recharger la page ;
            si le problème persiste, ta sauvegarde est peut-être corrompue et tu
            peux la réinitialiser.
          </p>
          <div style={styles.actions}>
            <button style={styles.primary} onClick={this.handleReload}>
              Recharger
            </button>
            <button style={styles.danger} onClick={this.handleResetSave}>
              Réinitialiser la sauvegarde
            </button>
          </div>
          <p style={styles.warn}>
            La réinitialisation efface définitivement la progression locale.
          </p>
        </div>
      </div>
    );
  }
}

// Styles inline : indépendants de toute CSS applicative susceptible d'être
// affectée par l'erreur.
const styles: Record<string, CSSProperties> = {
  overlay: {
    position: 'fixed',
    inset: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: '#0b1f2a',
    padding: 24,
    zIndex: 9999,
    fontFamily: 'system-ui, sans-serif',
  },
  card: {
    maxWidth: 420,
    width: '100%',
    background: 'rgba(15, 41, 54, 0.95)',
    border: '1px solid rgba(45, 212, 191, 0.3)',
    borderRadius: 16,
    padding: '32px 28px',
    textAlign: 'center',
    boxShadow: '0 20px 60px rgba(0,0,0,0.5)',
  },
  emoji: { fontSize: 44, marginBottom: 8 },
  title: { color: '#fff', fontSize: 22, fontWeight: 800, margin: '0 0 12px' },
  text: { color: 'rgba(203, 213, 225, 0.9)', fontSize: 14, lineHeight: 1.6, margin: '0 0 24px' },
  actions: { display: 'flex', gap: 12, justifyContent: 'center', flexWrap: 'wrap' },
  primary: {
    background: '#14b8a6',
    color: '#03201b',
    border: 'none',
    borderRadius: 10,
    padding: '10px 20px',
    fontWeight: 700,
    fontSize: 14,
    cursor: 'pointer',
  },
  danger: {
    background: 'transparent',
    color: '#fca5a5',
    border: '1px solid rgba(248, 113, 113, 0.4)',
    borderRadius: 10,
    padding: '10px 20px',
    fontWeight: 600,
    fontSize: 14,
    cursor: 'pointer',
  },
  warn: { color: 'rgba(148, 163, 184, 0.7)', fontSize: 12, fontStyle: 'italic', margin: '16px 0 0' },
};
