<script setup lang="ts">
import DefaultTheme from 'vitepress/theme'
import { useData } from 'vitepress'

const { Layout } = DefaultTheme
const { theme } = useData()
</script>

<template>
  <Layout>
    <template #doc-after>
      <div class="custom-footer" v-if="theme.footer">
        <p class="message" v-if="theme.footer.message" v-html="theme.footer.message"></p>
        <p class="copyright" v-if="theme.footer.copyright" v-html="theme.footer.copyright"></p>
      </div>
    </template>
  </Layout>
</template>

<style scoped>
.custom-footer {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--vp-c-divider);
  text-align: center;
  font-size: 0.875rem;
  color: var(--vp-c-text-2);
}

.custom-footer .message {
  margin-bottom: 0.5rem;
}

.custom-footer .copyright {
  margin: 0;
}

.custom-footer :deep(a) {
  color: var(--vp-c-brand-1);
  text-decoration: none;
}

.custom-footer :deep(a:hover) {
  text-decoration: underline;
}
</style>
