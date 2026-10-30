// Live price preview on the booking form. The SERVER recalculates the real price on submit.
// Rule (mirrors PriceCalculator.cs): nights starting on Friday or Saturday cost +20%.
document.addEventListener('DOMContentLoaded', () => {
  const start = document.querySelector('input[name="CheckIn"]');
  const end = document.querySelector('input[name="CheckOut"]');
  const nights = document.getElementById('night-count');
  const total = document.getElementById('total-price');
  const note = document.getElementById('weekend-note');
  const rateEl = document.querySelector('.summary-line strong');
  if (!start || !end || !nights || !total || !rateEl) return;

  const rate = Number(rateEl.textContent.replace(/[^0-9.]/g, ''));
  const SURCHARGE = 0.20;
  const pad = n => String(n).padStart(2, '0');
  const fmt = d => d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()); // local date (no UTC shift)

  const update = () => {
    if (!start.value || !end.value) return;
    const s = new Date(start.value + 'T00:00:00'), e = new Date(end.value + 'T00:00:00');
    let n = 0, w = 0, sum = 0;
    for (const d = new Date(s); d < e; d.setDate(d.getDate() + 1)) {
      n++;
      const day = d.getDay(); // 5 = Friday, 6 = Saturday
      if (day === 5 || day === 6) { w++; sum += Math.round(rate * (1 + SURCHARGE) * 100) / 100; } else sum += rate;
    }
    if (n > 0) {
      nights.textContent = n + ' ' + (n === 1 ? 'night' : 'nights');
      total.textContent = '₹' + Math.round(sum).toLocaleString('en-IN');
      if (note) note.textContent = w > 0 ? 'Includes ' + w + ' weekend night(s) at +20%.' : 'Friday & Saturday nights carry a 20% weekend surcharge.';
    }
  };

  start.addEventListener('change', () => {
    if (end.value <= start.value) { const d = new Date(start.value + 'T00:00:00'); d.setDate(d.getDate() + 1); end.value = fmt(d); }
    update();
  });
  end.addEventListener('change', update);
});
