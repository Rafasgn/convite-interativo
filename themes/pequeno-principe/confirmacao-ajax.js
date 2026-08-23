(function () {
  'use strict';

  var container = document.getElementById('pp-confirmacao');
  if (!container) return;

  // Delegação de evento — pega submit em qualquer form dentro do container,
  // mesmo depois que o partial for substituído
  container.addEventListener('submit', function (e) {
    var form = e.target.closest('form[data-pp-form]');
    if (!form) return;

    e.preventDefault();

    var submitButton = form.querySelector('button[type="submit"]:focus') ||
                       document.activeElement.closest('button[type="submit"]') ||
                       form.querySelector('button[type="submit"]');

    // Se veio de um button específico com name/value (padrão modo Individual),
    // precisa incluir esse par no FormData
    var formData = new FormData(form);
    if (submitButton && submitButton.name && submitButton.value) {
      formData.append(submitButton.name, submitButton.value);
    }

    // Handler vem do formaction (asp-page-handler gera formaction no button)
    var action = submitButton && submitButton.formAction ? submitButton.formAction : form.action;

    // Loading state
    var todosBotoes = container.querySelectorAll('button[type="submit"]');
    todosBotoes.forEach(function (b) { b.disabled = true; });
    if (submitButton) submitButton.classList.add('pp-btn-loading');

    // Antiforgery — Razor Pages já injeta __RequestVerificationToken como input hidden no form
    var token = formData.get('__RequestVerificationToken');

    fetch(action, {
      method: 'POST',
      body: formData,
      headers: {
        'X-Requested-With': 'XMLHttpRequest',
        'RequestVerificationToken': token || ''
      },
      credentials: 'same-origin'
    })
      .then(function (resp) {
        if (!resp.ok) throw new Error('Resposta não OK: ' + resp.status);
        return resp.text();
      })
      .then(function (html) {
        container.innerHTML = html;
      })
      .catch(function (err) {
        console.error('Falha no submit AJAX, fazendo fallback pra submit normal', err);
        // Fallback: submit normal (recarrega página inteira)
        todosBotoes.forEach(function (b) { b.disabled = false; });
        if (submitButton) submitButton.classList.remove('pp-btn-loading');
        form.submit();
      });
  });
})();
