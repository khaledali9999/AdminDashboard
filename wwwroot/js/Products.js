const search = document.getElementById('search'), form = document.getElementById('addForm');
(async () => {
  const cats = await ApiClient.get('/products/categories');
  document.getElementById('cat').innerHTML = cats.map(c => `<option value="${c.id}">${ui.esc(c.name)}</option>`).join('');
})();
const table = ui.dataTable({ container: '#table', url: '/products', searchInput: search, columns: [
  { title: '#', key: 'id' }, { title: 'الاسم', key: 'name' }, { title: 'السعر', key: 'price' }, { title: 'المخزون', key: 'stock' }, { title: 'التصنيف', key: 'category' },
  { title: '', render: r => `<button class="btn btn-sm btn-outline-danger" onclick="del(${r.id})">حذف</button>` }] });
form.onsubmit = async e => {
  e.preventDefault(); const f = new FormData(form);
  try { await ApiClient.post('/products', { name: f.get('name'), price: +f.get('price'), stock: +f.get('stock'), categoryId: +f.get('categoryId') }, { silent: false }); form.reset(); table.reload(); } catch {}
};
async function del(id) { if (confirm('متأكد؟')) { try { await ApiClient.delete('/products/' + id); table.reload(); } catch {} } }
