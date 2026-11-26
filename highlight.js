(() => {
  const original = document.querySelector('.feature');
  const cards = [...document.querySelectorAll('#projects .grid article')].filter(card => card.querySelector('.visual img, .night'));
  if (!original || !cards.length) return;
  const gallery = document.createElement('div');
  gallery.className = 'highlights';
  gallery.setAttribute('aria-label', '프로젝트 하이라이트');
  original.before(gallery);
  gallery.append(original);
  const slides = [original];
  for (const card of cards) {
    const slide = document.createElement('section');
    slide.className = 'feature';
    slide.hidden = true;
    const copy = document.createElement('div');
    const kicker = document.createElement('p');
    kicker.className = 'kicker';
    kicker.textContent = card.querySelector('small').textContent;
    const title = document.createElement('h1');
    title.textContent = card.querySelector('h3').textContent;
    copy.append(kicker, title, card.querySelector('.body p').cloneNode(true), card.querySelector('.note-actions').cloneNode(true));
    const figure = document.createElement('figure');
    const media = card.querySelector('.visual img, .night').cloneNode(true);
    if (media.tagName === 'IMG') media.loading = 'lazy';
    figure.append(media);
    slide.append(copy, figure);
    gallery.append(slide);
    slides.push(slide);
  }
  const controls = document.createElement('div');
  controls.className = 'highlight-controls';
  const pause = document.createElement('button');
  pause.type = 'button';
  pause.textContent = '일시정지';
  const dots = slides.map((slide, i) => {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'highlight-dot';
    button.setAttribute('aria-label', (i + 1) + '번째 프로젝트 보기');
    button.addEventListener('click', () => { show(i); restart(); });
    controls.append(button);
    return button;
  });
  controls.append(pause);
  gallery.append(controls);
  let current = 0, timer, stopped = false;
  const reduced = matchMedia('(prefers-reduced-motion: reduce)');
  function show(i) {
    slides[current].hidden = true;
    current = i;
    slides[current].hidden = false;
    dots.forEach((dot, index) => dot.setAttribute('aria-current', index === current ? 'true' : 'false'));
  }
  function restart() {
    pause.hidden = reduced.matches;
    clearInterval(timer);
    if (!stopped && !reduced.matches) timer = setInterval(() => show((current + 1) % slides.length), 7000);
  }
  pause.addEventListener('click', () => { stopped = !stopped; pause.textContent = stopped ? '자동 재생' : '일시정지'; restart(); });
  gallery.addEventListener('mouseenter', () => clearInterval(timer));
  gallery.addEventListener('mouseleave', restart);
  gallery.addEventListener('focusin', () => clearInterval(timer));
  gallery.addEventListener('focusout', event => { if (!gallery.contains(event.relatedTarget)) restart(); });
  document.addEventListener('visibilitychange', () => document.hidden ? clearInterval(timer) : restart());
  reduced.addEventListener('change', restart);
  show(0);
  restart();
})();
