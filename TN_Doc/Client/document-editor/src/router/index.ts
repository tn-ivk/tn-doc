import { createRouter, createWebHistory } from 'vue-router';
import DocumentEditor from '@/views/DocumentEditor.vue';
import ErrorPage from '@/views/ErrorPage.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/edit/:deviceId/:docType/:id',
      name: 'editor',
      component: DocumentEditor,
      props: true
    },
    {
      path: '/error',
      name: 'error',
      component: ErrorPage
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/error?message=Страница не найдена'
    }
  ]
});

export default router;
