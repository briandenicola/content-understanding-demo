<script setup lang="ts">
import DocumentUpload from './components/DocumentUpload.vue'
import ResultsPanel from './components/ResultsPanel.vue'
import { ref }from 'vue'

interface ExtractedField {
  name: string
  value: string
  confidence: number
  confidenceLabel: string
  category: string | null
}

interface AnalysisResult {
  documentId: string
  documentType: string
  fileName: string
  analyzedAt: string
  overallConfidence: number
  confidenceExplanation: string
  fields: ExtractedField[]
  agentSummary: string | null
  markdown: string | null
}

const results = ref<AnalysisResult[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const pdfUrl = ref<string | null>(null)

async function handleUpload(file: File, useAgent: boolean) {
  isLoading.value = true
  error.value = null

  // Create object URL for PDF preview
  pdfUrl.value = URL.createObjectURL(file)

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
      <h1>Account Opening Portal</h1>
      <p class="subtitle">Upload documents for identity verification</p>
    </header>

    <main class="main">
      <DocumentUpload :is-loading="isLoading" @upload="handleUpload" />

      <div v-if="error" class="error-banner">
        {{ error }}
      </div>

      <ResultsPanel v-if="results.length > 0" :results="results" :pdf-url="pdfUrl" />
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
  font-family: 'Wells Fargo Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: #faf8f5;
  color: #2d2926;
}

.app {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0;
}

.header {
  background: #b71c1c;
  color: white;
  padding: 1.5rem 2rem;
  margin-bottom: 2rem;
}

.header h1 {
  font-size: 1.75rem;
  color: white;
  font-weight: 600;
}

.subtitle {
  color: rgba(255, 255, 255, 0.85);
  margin-top: 0.25rem;
  font-size: 0.95rem;
}

.main {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 0 2rem 2rem;
}

.error-banner {
  background: #fef3f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
  padding: 1rem;
  color: #b71c1c;
}
</style>
