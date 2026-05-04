<script setup lang="ts">
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

defineProps<{ results: AnalysisResult[] }>()

function confidenceColor(confidence: number): string {
  if (confidence >= 0.9) return '#2a7'
  if (confidence >= 0.7) return '#e8a838'
  return '#c33'
}

function groupByCategory(fields: ExtractedField[]): Record<string, ExtractedField[]> {
  return fields.reduce((acc, field) => {
    const cat = field.category || 'Other'
    if (!acc[cat]) acc[cat] = []
    acc[cat].push(field)
    return acc
  }, {} as Record<string, ExtractedField[]>)
}
</script>

<template>
  <div v-if="results.length > 0" class="results">
    <h2>📋 Analysis Results</h2>

    <div v-for="result in results" :key="result.documentId" class="result-card">
      <div class="result-header">
        <div>
          <strong>{{ result.documentType }}</strong>
          <span class="filename">{{ result.fileName }}</span>
        </div>
        <div class="confidence-badge" :style="{ background: confidenceColor(result.overallConfidence) }">
          {{ (result.overallConfidence * 100).toFixed(0) }}% confidence
        </div>
      </div>

      <div v-for="(fields, category) in groupByCategory(result.fields)" :key="category" class="field-group">
        <h4>{{ category }}</h4>
        <div class="fields-grid">
          <div v-for="field in fields" :key="field.name" class="field-item">
            <span class="field-name">{{ field.name }}</span>
            <span class="field-value">{{ field.value }}</span>
            <span class="field-confidence" :style="{ color: confidenceColor(field.confidence) }">
              {{ (field.confidence * 100).toFixed(0) }}%
            </span>
          </div>
        </div>
      </div>

      <div v-if="result.agentSummary" class="agent-summary">
        <h4>🤖 Agent Review</h4>
        <pre>{{ result.agentSummary }}</pre>
      </div>
    </div>
  </div>
</template>

<style scoped>
.results h2 {
  margin-bottom: 1rem;
}

.result-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  margin-bottom: 1rem;
}

.result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #eee;
}

.filename {
  display: block;
  color: #888;
  font-size: 0.85rem;
}

.confidence-badge {
  color: white;
  padding: 0.25rem 0.75rem;
  border-radius: 20px;
  font-size: 0.85rem;
  font-weight: 500;
}

.field-group {
  margin-bottom: 1rem;
}

.field-group h4 {
  color: #0f3460;
  margin-bottom: 0.5rem;
  font-size: 0.9rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.fields-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 0.5rem;
}

.field-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem;
  background: #f8f9fa;
  border-radius: 6px;
}

.field-name {
  font-size: 0.8rem;
  color: #666;
  min-width: 100px;
}

.field-value {
  font-weight: 500;
  flex: 1;
}

.field-confidence {
  font-size: 0.75rem;
  font-weight: 600;
}

.agent-summary {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #eee;
}

.agent-summary h4 {
  margin-bottom: 0.5rem;
}

.agent-summary pre {
  background: #f8f9fa;
  padding: 1rem;
  border-radius: 8px;
  font-size: 0.85rem;
  white-space: pre-wrap;
  overflow-x: auto;
}
</style>
