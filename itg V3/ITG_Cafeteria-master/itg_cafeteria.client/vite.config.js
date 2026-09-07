import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 58322,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'https://localhost:7005',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
