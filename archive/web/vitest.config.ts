import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    // Le noyau est pur : aucun DOM n'est requis pour le prouver.
    environment: 'node',
    include: ['tests/**/*.test.ts'],
    // Quinze cycles de simulation prennent quelques secondes : le défaut de
    // 5 s de vitest transformerait une lenteur en faux échec.
    testTimeout: 120_000,
  },
})
