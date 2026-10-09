const ui = (() => {
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const debounce = (fn, ms = 400) => { let t; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); }; };

  function toast(msg, type = 'success') {
    const el = document.createElement('div');
    el.className = `alert alert-${type === 'error' ? 'danger' : type} shadow`; el.textContent = msg;
    document.getElementById('toasts').appendChild(el); setTimeout(() => el.remove(), 4000);
  }

  let pending = 0;
  function loading(on) {
    pending = Math.max(0, pending + (on ? 1 : -1));
    let bar = document.getElementById('topbar');
    if (!bar) { bar = document.createElement('div'); bar.id = 'topbar'; bar.className = 'progress position-fixed top-0 w-100'; bar.style.cssText = 'height:3px;z-index:3000;display:none';
      bar.innerHTML = '<div class="progress-bar progress-bar-striped progress-bar-animated w-100"></div>'; document.body.appendChild(bar); }
    bar.style.display = pending ? 'block' : 'none';
  }

  // Table: Pagination + Search + Loading/Empty/Error states
  function dataTable({ container, url, columns, searchInput, size = 10, extraParams = () => ({}) }) {
    const root = document.querySelector(container); let page = 1;
    async function load() {
      root.innerHTML = '<div class="text-center p-5"><div class="spinner-border"></div></div>';
      try {
        const q = new URLSearchParams({ page, size, search: searchInput?.value || '', ...extraParams() });
        const d = await ApiClient.get(`${url}?${q}`, { silent: true });
        if (!d.items.length) { root.innerHTML = '<div class="text-center text-muted p-5">لا توجد نتائج</div>'; return; }
        root.innerHTML = `<table class="table table-striped"><thead><tr>${columns.map(c => `<th>${c.title}</th>`).join('')}</tr></thead>
          <tbody>${d.items.map(r => `<tr>${columns.map(c => `<td>${c.render ? c.render(r) : ui.esc(r[c.key])}</td>`).join('')}</tr>`).join('')}</tbody></table>
          <div class="d-flex justify-content-between align-items-center"><span>${d.total.toLocaleString()} نتيجة</span>
          <div class="btn-group"><button class="btn btn-outline-secondary" id="prev" ${page <= 1 ? 'disabled' : ''}>السابق</button>
          <span class="btn disabled">${d.page} / ${d.totalPages}</span>
          <button class="btn btn-outline-secondary" id="next" ${page >= d.totalPages ? 'disabled' : ''}>التالي</button></div></div>`;
        root.querySelector('#prev').onclick = () => { page--; load(); };
        root.querySelector('#next').onclick = () => { page++; load(); };
      } catch (e) {
        root.innerHTML = `<div class="alert alert-danger">${esc(e.message)} <button class="btn btn-sm btn-light" id="retry">إعادة المحاولة</button></div>`;
        root.querySelector('#retry').onclick = load;
      }
    }
    searchInput?.addEventListener('input', debounce(() => { page = 1; load(); })); // Debounced Search
    load(); return { reload: load };
  }
  return { esc, debounce, toast, loading, dataTable };
})();
