<script setup lang="ts">
import { onMounted } from 'vue'
import AppNav from './components/AppNav.vue'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
onMounted(() => auth.init())
</script>

<template>
  <div class="min-h-screen" style="background: var(--bg); color: var(--text)">
    <AppNav />
    <!-- pt-16 offsets the fixed nav -->
    <div class="pt-16">
      <RouterView />
    </div>
  </div>
</template>
