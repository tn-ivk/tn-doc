<template>
  <svg :width="size" :height="size" viewBox="0 0 24 24">
    <path
      fill="currentColor"
      d="M12 21l-4-4h8l-4 4zm-8-8l-4-4h24l-4 4H4zm4-8L4 1h16l-4 4H8z"
    />
  </svg>
</template>

<script setup lang="ts">
interface Props {
  size?: number;
}

withDefaults(defineProps<Props>(), {
  size: 16
});
</script>


