import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';
import { createBaseConfig } from '../vite.config.base';

const baseConfig = createBaseConfig(__dirname);

export default defineConfig({
  ...baseConfig,
  plugins: [vue()],
  base: '/document-viewer/',
  build: {
    outDir: resolve(__dirname, '../../wwwroot/document-viewer'),
    emptyOutDir: true,
    sourcemap: process.env.NODE_ENV !== 'production',
    rollupOptions: {
      input: resolve(__dirname, 'src/main.ts'),
      output: {
        entryFileNames: 'document-viewer.js',
        chunkFileNames: 'document-viewer.[hash].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name?.endsWith('.css')) return 'document-viewer.css';
          return 'assets/[name].[hash].[ext]';
        }
      }
    }
  },
  server: {
    ...baseConfig.server,
    port: 5175,
    strictPort: true
  }
});
