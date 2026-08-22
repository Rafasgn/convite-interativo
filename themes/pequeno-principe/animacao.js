/*
  Tema Pequeno Príncipe — HU-05b.
  Consome window.dadosConvite (injetado pela partial Razor) e conduz a timeline
  visual via classes CSS cumulativas em #cena. Não sabe nada sobre EF/DTOs — só
  lê o JSON já pronto (ADR 0009).

  Duas redes de segurança independentes garantem que a tela nunca fica travada:
  (a) try/catch no bootstrap — qualquer exceção revela tudo na hora;
  (b) timeout de 15s fora do try/catch — cobre falhas silenciosas (sprite que
      não carrega sem disparar erro, promise pendurada, etc).
*/
(function () {
  'use strict';

  var cena = document.getElementById('cena');
  var skipBtn = document.getElementById('pp-skip');
  var nomesEl = document.getElementById('pp-nomes');
  var dataHoraEl = document.getElementById('pp-data-hora');
  var enderecoEl = document.getElementById('pp-endereco');
  var linkMapaEl = document.getElementById('pp-link-mapa');

  var FASES = [
    { t: 0, classe: 'fase-fundo' },
    { t: 500, classe: 'fase-nuvens' },
    { t: 1500, classe: 'fase-lua' },
    { t: 2500, classe: 'fase-principe' },
    { t: 4000, classe: 'fase-estrela-cadente' },
    { t: 6500, classe: 'fase-pergaminho' },
    { t: 7500, classe: 'fase-digitando' },
    { t: 9000, classe: 'fase-dados' },
    { t: 10000, classe: 'fase-confirmacao' },
  ];

  var timers = [];
  var digitacaoIntervalId = null;
  var finalizado = false;

  function prefereReducedMotion() {
    return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  function nomesParaTexto(nomes) {
    if (!nomes || nomes.length === 0) {
      return '';
    }
    if (nomes.length === 1) {
      return nomes[0];
    }
    return nomes.slice(0, -1).join(', ') + ' e ' + nomes[nomes.length - 1];
  }

  function formatarDataHora(iso) {
    if (!iso) {
      return '';
    }
    var data = new Date(iso);
    if (isNaN(data.getTime())) {
      return '';
    }
    var dia = String(data.getDate()).padStart(2, '0');
    var mes = String(data.getMonth() + 1).padStart(2, '0');
    var horas = String(data.getHours()).padStart(2, '0');
    var minutos = String(data.getMinutes()).padStart(2, '0');
    return dia + '/' + mes + '/' + data.getFullYear() + ' ' + horas + ':' + minutos;
  }

  function preencherDadosEvento(dados) {
    var evento = (dados && dados.evento) || {};
    if (dataHoraEl) {
      dataHoraEl.textContent = formatarDataHora(evento.dataHora);
    }
    if (enderecoEl) {
      enderecoEl.textContent = evento.endereco || '';
    }
    if (linkMapaEl && evento.linkMapa) {
      linkMapaEl.setAttribute('href', evento.linkMapa);
    }
  }

  function limparTimers() {
    timers.forEach(function (id) {
      clearTimeout(id);
    });
    timers = [];

    if (digitacaoIntervalId !== null) {
      clearInterval(digitacaoIntervalId);
      digitacaoIntervalId = null;
    }
  }

  // Rede de segurança: idempotente, chamável de qualquer lugar (skip, erro,
  // timeout de 15s, reduced-motion) sem risco de duplicar trabalho.
  function mostrarTudoAgora(dados) {
    if (finalizado) {
      return;
    }
    finalizado = true;

    limparTimers();

    if (cena) {
      FASES.forEach(function (fase) {
        cena.classList.add(fase.classe);
      });
      cena.classList.remove('fase-digitando');
    }

    if (nomesEl && dados) {
      nomesEl.textContent = nomesParaTexto(dados.nomes);
    }

    if (dados) {
      preencherDadosEvento(dados);
    }
  }

  function iniciarDigitacao(texto) {
    if (!nomesEl) {
      return;
    }

    var i = 0;
    nomesEl.textContent = '';

    digitacaoIntervalId = setInterval(function () {
      i += 1;
      nomesEl.textContent = texto.slice(0, i);

      if (i >= texto.length) {
        clearInterval(digitacaoIntervalId);
        digitacaoIntervalId = null;
        if (cena) {
          cena.classList.remove('fase-digitando');
        }
      }
    }, 100);
  }

  function aplicarFase(fase, dados) {
    if (finalizado || !cena) {
      return;
    }

    cena.classList.add(fase.classe);

    if (fase.classe === 'fase-digitando') {
      iniciarDigitacao(nomesParaTexto(dados.nomes));
    }

    if (fase.classe === 'fase-dados') {
      preencherDadosEvento(dados);
    }
  }

  // Se a aba estiver em background quando o marco disparar, adia a fase até
  // a aba voltar a ficar visível, em vez de aplicar "escondida" ou perder o passo.
  function agendarFase(fase, dados) {
    var id = setTimeout(function () {
      if (document.hidden) {
        var aoFicarVisivel = function () {
          if (!document.hidden) {
            document.removeEventListener('visibilitychange', aoFicarVisivel);
            aplicarFase(fase, dados);
          }
        };
        document.addEventListener('visibilitychange', aoFicarVisivel);
        return;
      }

      aplicarFase(fase, dados);
    }, fase.t);

    timers.push(id);
  }

  function iniciarTimeline(dados) {
    FASES.forEach(function (fase) {
      agendarFase(fase, dados);
    });
  }

  function bootstrap() {
    var dados = window.dadosConvite || {};

    if (skipBtn) {
      skipBtn.addEventListener('click', function () {
        mostrarTudoAgora(dados);
      });
    }

    if (prefereReducedMotion()) {
      mostrarTudoAgora(dados);
      return;
    }

    iniciarTimeline(dados);
  }

  // Rede de segurança (a): qualquer erro no bootstrap revela tudo na hora.
  try {
    bootstrap();
  } catch (erro) {
    mostrarTudoAgora(window.dadosConvite || {});
  }

  // Rede de segurança (b): independente do try/catch acima — cobre falhas
  // silenciosas (sprite que não carrega sem lançar erro, timer perdido etc).
  setTimeout(function () {
    mostrarTudoAgora(window.dadosConvite || {});
  }, 15000);
})();
