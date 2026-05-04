<script setup lang="ts">
import { ref } from 'vue'

defineProps<{ isLoading: boolean }>()
const emit = defineEmits<{ upload: [file: File, useAgent: boolean] }>()

const dragOver = ref(false)
const selectedFile = ref<File | null>(null)
const useAgent = ref(true)

function handleDrop(event: DragEvent) {
  dragOver.value = false
  const files = event.dataTransfer?.files
  if (files && files.length > 0) {
    selectedFile.value = files[0]
  }
}

function handleFileInput(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    selectedFile.value = input.files[0]
  }
}

function submit() {
  if (selectedFile.value) {
    emit('upload', selectedFile.value, useAgent.value)
    selectedFile.value = null
  }
}
</script>

<template>
  <div class="upload-card">
    <h2>📄 Upload Document</h2>
    <p class="help-text">Supported: Passport, Driver's License, Utility Bill, Bank Statement (PDF)</p>

    <div
      class="drop-zone"
      :class="{ 'drag-over': dragOver }"
      @dragover.prevent="dragOver = true"
      @dragleave="dragOver = false"
      @drop.prevent="handleDrop"
    >
      <div v-if="!selectedFile">
        <p class="drop-icon">📁</p>
        <p>Drag & drop a PDF here, or <label class="file-label">browse<input type="file" accept=".pdf" @change="handleFileInput" hidden /></label></p>
      </div>
      <div v-else class="selected-file">
        <p>✅ {{ selectedFile.name }} ({{ (selectedFile.size / 1024).toFixed(1) }} KB)</p>
      </div>
    </div>

    <div class="options">
      <label class="toggle-label">
        <input type="checkbox" v-model="useAgent" />
        Use AI Agent for review
      </label>
    </div>

    <button class="submit-btn" :disabled="!selectedFile || isLoading" @click="submit">
      <span v-if="isLoading">⏳ Analyzing...</span>
      <span v-else>🚀 Analyze Document</span>
    </button>
  </div>
</template>

<style scoped>
.upload-card {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.upload-card h2 {
  margin-bottom: 0.5rem;
}

.help-text {
  color: #888;
  font-size: 0.9rem;
  margin-bottom: 1rem;
}

.drop-zone {
  border: 2px dashed #ccc;
  border-radius: 8px;
  padding: 2rem;
  text-align: center;
  cursor: pointer;
  transition: border-color 0.2s, background 0.2s;
}

.drop-zone.drag-over {
  border-color: #0f3460;
  background: #f0f5ff;
}

.drop-icon {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.file-label {
  color: #0f3460;
  cursor: pointer;
  text-decoration: underline;
}

.selected-file {
  color: #2a7;
  font-weight: 500;
}

.options {
  margin-top: 1rem;
}

.toggle-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.submit-btn {
  margin-top: 1.5rem;
  width: 100%;
  padding: 0.75rem;
  font-size: 1rem;
  border: none;
  border-radius: 8px;
  background: #0f3460;
  color: white;
  cursor: pointer;
  transition: background 0.2s;
}

.submit-btn:hover:not(:disabled) {
  background: #1a5276;
}

.submit-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
