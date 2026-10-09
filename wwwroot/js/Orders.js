const form = document.getElementById('addForm');
const table = ui.dataTable({ container: '#table', url: '/orders', columns: [
  { title: '#', key: 'id' }, { title: 'المستخدم', key: 'user' }, { title: 'التاريخ', render: r => new Date(r.createdAt).toLocaleString('ar-EG') },
  { title: 'عدد المنتجات', key: 'itemsCount' }, { title: 'الإجمالي', key: 'total' }] });
form.onsubmit = async e => {
  e.preventDefault();
  try { await ApiClient.postForm('/orders', { items: new FormData(form).get('items') }); form.reset(); table.reload(); } catch {}
};
