---
outline: deep
---

<script setup>
import { data } from './releases.data'
</script>

# Changelog

All notable releases of Termina.

<div class="releases">
  <div v-for="release in data.releases" :key="release.version" class="release">
    <h2 :id="release.version">
      <a :href="'#' + release.version" class="header-anchor">#</a>
      {{ release.name }}
      <span v-if="release.isPrerelease" class="prerelease-badge">pre-release</span>
    </h2>
    <p class="release-meta">
      <span class="release-date">{{ release.date }}</span>
      <a :href="release.url" target="_blank" class="github-link">View full release notes on GitHub →</a>
    </p>
    <ul class="release-highlights">
      <li v-for="(item, index) in getHighlights(release.summary)" :key="index">{{ item }}</li>
    </ul>
  </div>
</div>

<div v-if="data.releases.length === 0" class="no-releases">
  <p>No releases found. Check the <a href="https://github.com/Aaronontheweb/Termina/releases">GitHub releases page</a>.</p>
</div>

<script>
export default {
  methods: {
    getHighlights(text) {
      // Extract bullet points as plain text highlights
      return text
        .split('\n')
        .filter(line => line.startsWith('- ') || line.startsWith('* '))
        .map(line => line.slice(2).replace(/`/g, '').replace(/\*\*/g, ''))
        .slice(0, 5)
    }
  }
}
</script>

<style scoped>
.releases {
  margin-top: 2rem;
}

.release {
  margin-bottom: 3rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid var(--vp-c-divider);
}

.release:last-child {
  border-bottom: none;
}

.release h2 {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.prerelease-badge {
  font-size: 0.75rem;
  padding: 0.2rem 0.5rem;
  background: var(--vp-c-warning-soft);
  color: var(--vp-c-warning-1);
  border-radius: 4px;
  font-weight: 500;
}

.release-meta {
  display: flex;
  gap: 1rem;
  align-items: center;
  color: var(--vp-c-text-2);
  font-size: 0.9rem;
  margin-bottom: 1rem;
}

.github-link {
  color: var(--vp-c-brand-1);
  text-decoration: none;
}

.github-link:hover {
  text-decoration: underline;
}

.release-highlights {
  color: var(--vp-c-text-1);
  margin: 0.5rem 0;
  padding-left: 1.5rem;
}

.release-highlights li {
  margin: 0.4rem 0;
}

.no-releases {
  text-align: center;
  color: var(--vp-c-text-2);
  padding: 2rem;
}
</style>
