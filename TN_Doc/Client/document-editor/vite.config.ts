import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  build: {
    outDir: '../../wwwroot/document-editor',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        entryFileNames: 'document-editor.js',
        chunkFileNames: 'chunks/[name]-[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name?.endsWith('.css')) {
            return 'document-editor.css'
          }
          return 'assets/[name]-[hash][extname]'
        }
      }
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:38509',
        changeOrigin: true
      }
    }
  }
})
