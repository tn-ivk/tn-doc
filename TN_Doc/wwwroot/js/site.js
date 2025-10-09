// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(function () {

    const titles = {
        light: 'Светлая',
        dark: 'Тёмная',
        gray: 'Серая',
        system: 'Системная'
    };

    function markActive($menu, theme) {
        $menu.find('.custom-dropdown-item').removeClass('active');
        $menu.find(`[data-theme-select="${theme}"]`).addClass('active');
    }

    const $menu = $('.dropdown-menu.custom-dropdown-menu');
    if ($menu.length) {
        const html = [
            { key: 'light', icon: 'fa-sun-o' },
            { key: 'dark', icon: 'fa-moon-o' },
            { key: 'gray', icon: 'fa-adjust' },
            { key: 'system', icon: 'fa-desktop' },
        ].map(x => `
            <a class="dropdown-item custom-dropdown-item" href="#" data-theme-select="${x.key}">
              <i class="fa ${x.icon}" aria-hidden="true"></i>
              <span>Тема: <b>${titles[x.key]}</b></span>
            </a>
        `).join('');

        $menu.append('<div class="dropdown-divider"></div>' + html);

        $menu.on('click', '[data-theme-select]', function (e) {
            e.preventDefault();
            const theme = $(this).attr('data-theme-select');
            if (window.TNDocTheme && window.TNDocTheme.set) {
                window.TNDocTheme.set(theme);
                markActive($menu, theme);
            }
        });

        // initial active
        const current = (window.TNDocTheme && window.TNDocTheme.get && window.TNDocTheme.get()) || 'system';
        markActive($menu, current);
    }
});
