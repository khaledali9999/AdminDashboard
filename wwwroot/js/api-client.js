// Module واحد لكل الـ AJAX: Errors + Loading + Retry موحّدة
const ApiClient = (() => {
  const base = '/api';
  const sleep = ms => new Promise(r => setTimeout(r, ms));

  async function request(endpoint, { silent = false, retries = 0, ...options } = {}) {
    ui.loading(true);
    try {
      for (let attempt = 0; ; attempt++) {
        try {
          const res = await fetch(base + endpoint, { headers: { 'X-Requested-With': 'XMLHttpRequest', ...(options.headers || {}) }, ...options });
          if (res.status === 401) { location.href = '/Account/Login'; throw new Error('انتهت الجلسة'); }
          const json = await res.json().catch(() => ({ success: false, message: 'استجابة غير صالحة' }));
          if (!res.ok || !json.success) { const e = new Error(json.message || 'حدث خطأ'); e.noRetry = res.status < 500; throw e; }
          if (json.message && options.method && options.method !== 'GET') ui.toast(json.message);
          return json.data;
        } catch (e) {
          if (attempt >= retries || e.noRetry) throw e;
          await sleep(2 ** attempt * 500); // exponential backoff
        }
      }
    } catch (e) { if (!silent) ui.toast(e.message, 'error'); throw e; }
    finally { ui.loading(false); }
  }

  const json = (method) => (url, body, o = {}) => request(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body), ...o });
  return {
    get: (url, o) => request(url, { retries: 2, ...o }),
    post: json('POST'), put: json('PUT'),
    postForm: (url, data) => request(url, { method: 'POST', body: new URLSearchParams(data) }),
    delete: url => request(url, { method: 'DELETE' })
  };
})();
