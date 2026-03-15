import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    vue(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Cutline Fantasy',
        short_name: 'Cutline',
        description: 'Will you make the cut?',
        theme_color: '#000000',
      },
    }),
  ],
  server: {
    proxy: {
      '/api': 'http://localhost:5134',
      '/hubs': { target: 'ws://localhost:5134', ws: true },
    },
  },
})
