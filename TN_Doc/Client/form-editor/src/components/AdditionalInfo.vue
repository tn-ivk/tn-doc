<template>
  <table id="AdditionalInfo" class="additional-info-table">
    <tbody>
      <tr v-for="field in fields" :key="field.name">
        <td class="label-cell">{{ field.label }}</td>
        <td class="value-cell">
          <SelectField
            v-if="field.type === 'select'"
            v-model="formData[field.name]"
            :field="field"
          />
          <FormField
            v-else
            v-model="formData[field.name]"
            :field="field"
          />
        </td>
      </tr>
    </tbody>
  </table>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia'
import FormField from './FormField.vue'
import SelectField from './SelectField.vue'
import { useFormStore } from '../stores/formStore'

const formStore = useFormStore()
const { fields, formData } = storeToRefs(formStore)
</script>

<style scoped>
.additional-info-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 10px;
}

.additional-info-table td {
  padding: 8px;
  border: 1px solid #ddd;
  vertical-align: middle;
}

.label-cell {
  width: 30%;
  font-weight: 500;
  background-color: #f5f5f5;
  text-align: right;
  padding-right: 12px;
}

.value-cell {
  width: 70%;
  padding: 4px 8px;
}
</style>
