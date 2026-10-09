const form = document.getElementById('addForm');
const table = ui.dataTable({ container: '#table', url: '/users', searchInput: document.getElementById('search'), columns: [
  { title: '#', key: 'id' }, { title: 'الإيميل', key: 'email' }, { title: 'الاسم', key: 'fullName' }, { title: 'الدور', key: 'role' }] });
form.onsubmit = async e => {
  e.preventDefault(); const f = Object.fromEntries(new FormData(form));
  try { await ApiClient.post('/users', f); form.reset(); table.reload(); } catch {}
};
