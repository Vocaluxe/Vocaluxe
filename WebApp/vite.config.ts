import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// During development (npm run dev) API calls are forwarded to a running Vocaluxe.
// Override the address with VOCALUXE_URL, e.g. VOCALUXE_URL=http://192.168.0.10:3000 npm run dev
const vocaluxe = process.env.VOCALUXE_URL ?? 'http://localhost:3000'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
  },
  server: {
    host: true,
    proxy: {
      '/api': vocaluxe,
      '/swagger': vocaluxe
    }
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    assetsDir: 'assets'
  }
})
