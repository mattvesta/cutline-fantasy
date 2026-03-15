<script setup lang="ts">
import AppNav from './components/AppNav.vue'
</script>

<template>
  <div class="min-h-screen" style="background: var(--bg); color: var(--text)">
    <AppNav />
    <!-- pt-14 offsets the fixed nav -->
    <div class="pt-14">
      <RouterView />
    </div>
  </div>
</template>
