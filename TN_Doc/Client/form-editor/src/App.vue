<template>
  <div id="form-editor-app">
    <AdditionalInfo v-if="isInitialized" />
    <div v-else class="loading">
      Загрузка формы...
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AdditionalInfo from './components/AdditionalInfo.vue'
import { useFormStore } from './stores/formStore'
import { useDictionaryStore } from './stores/dictionaryStore'
import type { FormEditorConfig } from './types/field.types'

const props = defineProps<{
  config: FormEditorConfig
}>()

const isInitialized = ref(false)

onMounted(() => {
  try {
    const formStore = useFormStore()
    const dictStore = useDictionaryStore()

    // Инициализация полей формы
    formStore.initFields(props.config.fields, props.config.data)

    // Инициализация справочников
    dictStore.init(props.config.dictionaries)

    // Установка недопустимых символов
    if (props.config.invalidChars) {
      formStore.setInvalidChars(props.config.invalidChars)
    }

    isInitialized.value = true

    console.log('Form editor инициализирован', {
      fieldsCount: props.config.fields.length,
      usersCount: props.config.dictionaries.Users?.length || 0,
      invalidCharsCount: props.config.invalidChars?.length || 0
    })
  } catch (error) {
    console.error('Ошибка инициализации form editor:', error)
  }
})
</script>

<style scoped>
#form-editor-app {
  width: 100%;
  padding: 10px;
}

.loading {
  padding: 20px;
  text-align: center;
  color: #666;
  font-size: 14px;
}
</style>
