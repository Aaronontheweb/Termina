// VitePress data loader - fetches GitHub releases at build time
export interface Release {
  version: string
  name: string
  date: string
  url: string
  summary: string
  isPrerelease: boolean
}

export interface ReleasesData {
  latest: string
  releases: Release[]
}

declare const data: ReleasesData
export { data }

export default {
  async load(): Promise<ReleasesData> {
    const response = await fetch(
      'https://api.github.com/repos/Aaronontheweb/Termina/releases?per_page=10'
    )

    if (!response.ok) {
      console.warn('Failed to fetch releases:', response.status)
      return { latest: '0.0.0', releases: [] }
    }

    const releases = await response.json()

    const parsed: Release[] = releases.map((r: any) => ({
      version: r.tag_name,
      name: r.name || r.tag_name,
      date: new Date(r.published_at).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      }),
      url: r.html_url,
      summary: extractSummary(r.body || ''),
      isPrerelease: r.prerelease
    }))

    return {
      latest: parsed[0]?.version || '0.0.0',
      releases: parsed
    }
  }
}

function extractSummary(body: string): string {
  // Get first meaningful paragraph (skip headers, blank lines)
  const lines = body.split('\n')
  const summaryLines: string[] = []

  for (const line of lines) {
    const trimmed = line.trim()
    // Skip empty lines, headers, and bullet points at start
    if (!trimmed || trimmed.startsWith('#')) continue
    // Stop at a header or after getting enough content
    if (summaryLines.length > 0 && trimmed.startsWith('#')) break
    // Collect bullet points and text
    summaryLines.push(trimmed)
    if (summaryLines.length >= 5) break
  }

  return summaryLines.join('\n')
}
