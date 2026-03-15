<script setup lang="ts">
import AppNav from './components/AppNav.vue'
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
