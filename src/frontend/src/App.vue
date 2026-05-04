<script setup lang="ts">
import DocumentUpload from './components/DocumentUpload.vue'
import ResultsPanel from './components/ResultsPanel.vue'
import { ref } from 'vue'

interface ExtractedField {
  name: string
  value: string
  confidence: number
  category: string | null
}

interface AnalysisResult {
  documentId: string
  documentType: string
  fileName: string
  analyzedAt: string
  overallConfidence: number
  fields: ExtractedField[]
  agentSummary: string | null
}

const results = ref<AnalysisResult[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

async function handleUpload(file: File, useAgent: boolean) {
  isLoading.value = true
  error.value = null

  const formData = new FormData()
  formData.append('file', file)

  const endpoint = useAgent ? '/api/documents/analyze-agent' : '/api/documents/upload'

  try {
    const response = await fetch(endpoint, {
      method: 'POST',
      body: formData
    })

    if (!response.ok) {
      throw new Error(`Upload failed: ${response.statusText}`)
    }

    const result: AnalysisResult = await response.json()
    results.value.unshift(result)
  } catch (e: any) {
    error.value = e.message
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="app">
    <header class="header">
      <h1>🏦 Account Opening Portal</h1>
      <p class="subtitle">Upload documents for identity verification</p>
    </header>

    <main class="main">
      <DocumentUpload :is-loading="isLoading" @upload="handleUpload" />

      <div v-if="error" class="error-banner">
        ⚠️ {{ error }}
      </div>

      <ResultsPanel :results="results" />
    </main>
  </div>
</template>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: #f5f7fa;
  color: #1a1a2e;
}

.app {
  max-width: 960px;
  margin: 0 auto;
  padding: 2rem;
}

.header {
  text-align: center;
  margin-bottom: 2rem;
}

.header h1 {
  font-size: 2rem;
  color: #0f3460;
}

.subtitle {
  color: #666;
  margin-top: 0.5rem;
}

.main {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.error-banner {
  background: #fee;
  border: 1px solid #fcc;
  border-radius: 8px;
  padding: 1rem;
  color: #c33;
}
</style>
