import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    preserveSymlinks: true
  },
  build: {
    outDir: 'dist'
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5038',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
