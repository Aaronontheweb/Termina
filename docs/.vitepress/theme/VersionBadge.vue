<script setup lang="ts">
import { data } from '../../releases.data'
</script>

<template>
  <a
    v-if="data.latest !== '0.0.0'"
    href="/Termina/changelog"
    class="version-badge"
    title="View changelog"
  >
    v{{ data.latest }}
  </a>
</template>

<style scoped>
.version-badge {
  display: inline-flex;
  align-items: center;
  padding: 0 10px;
  height: 28px;
  font-size: 13px;
  font-weight: 500;
  color: var(--vp-c-brand-1);
  background: var(--vp-c-brand-soft);
  border-radius: 14px;
  text-decoration: none;
  transition: background 0.2s, color 0.2s;
  margin-left: 8px;
}

.version-badge:hover {
  background: var(--vp-c-brand-1);
  color: var(--vp-c-white);
}
</style>
