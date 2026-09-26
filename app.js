// 方案页交互：平滑滚动 + 导航高亮
(function () {
  const links = Array.from(document.querySelectorAll('[data-nav]'));
  const sections = links
    .map((a) => document.querySelector(a.getAttribute('href')))
    .filter(Boolean);

  // 点击平滑滚动（考虑 sticky 头部）
  links.forEach((a) => {
    a.addEventListener('click', (e) => {
      const target = document.querySelector(a.getAttribute('href'));
      if (!target) return;
      e.preventDefault();
      const top = target.getBoundingClientRect().top + window.scrollY - 64;
      window.scrollTo({ top, behavior: 'smooth' });
      history.replaceState(null, '', a.getAttribute('href'));
    });
  });

  // 滚动时高亮当前章节
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        const id = '#' + entry.target.id;
        links.forEach((a) =>
          a.classList.toggle('active', a.getAttribute('href') === id)
        );
      });
    },
    { rootMargin: '-30% 0px -60% 0px', threshold: 0 }
  );
  sections.forEach((s) => observer.observe(s));
})();
