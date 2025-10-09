// Material 3 theme initialization with localStorage + system preference
(function(){
  var THEME_KEY = 'tn_doc_theme'; // 'light' | 'dark' | 'gray' | 'system'

  function applyTheme(theme){
    var root = document.documentElement;
    if(theme === 'light'){
      root.setAttribute('data-theme', 'light');
    } else if(theme === 'dark'){
      root.setAttribute('data-theme', 'dark');
    } else if(theme === 'gray'){
      root.setAttribute('data-theme', 'gray');
    } else {
      // system
      root.removeAttribute('data-theme');
    }
  }

  function readStored(){
    try { return localStorage.getItem(THEME_KEY) || 'system'; } catch(e){ return 'system'; }
  }

  // initial
  applyTheme(readStored());

  // optional: expose simple API
  window.TNDocTheme = {
    set: function(theme){
      if(['light','dark','gray','system'].indexOf(theme) === -1) return;
      try { localStorage.setItem(THEME_KEY, theme); } catch(e) {}
      applyTheme(theme);
    },
    get: function(){ return readStored(); }
  };
})();


