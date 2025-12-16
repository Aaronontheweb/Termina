<script setup lang="ts">
import { computed } from 'vue'
import DefaultTheme from 'vitepress/theme'
import { useData, useRoute } from 'vitepress'
import VersionBadge from './VersionBadge.vue'

const { Layout } = DefaultTheme
const { theme, frontmatter } = useData()
const route = useRoute()

// Show footer on doc pages, but not on changelog (it has its own) or home
const showFooter = computed(() => {
  const layout = frontmatter.value.layout
  const path = route.path
  return theme.value.footer &&
         layout !== 'home' &&
         !path.includes('/changelog')
})
</script>

<template>
  <Layout>
    <template #nav-bar-content-after>
      <VersionBadge />
    </template>
    <template #doc-after>
      <div class="custom-footer" v-if="showFooter">
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
